using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MongoDB.Driver;
using WebApplication1.Services;
using WebApplication1.Models;
using WebApplication1.Enums;
using Microsoft.Extensions.Configuration;

namespace WebApplication1.Core.Utils
{
    /// <summary>
    /// Small helper to send emails used by the password-reset flow.
    /// Reads SMTP configuration from environment variables (SMTP_HOST/SMTP_PORT/SMTP_USER/SMTP_PASS/SMTP_FROM).
    /// If SMTP is not configured it logs the reset link to the console instead of sending.
    /// </summary>
    public class EmailHelper
    {
        private readonly MongoDBService _mongo;
        private readonly IConfiguration _config;

        public EmailHelper(MongoDBService mongo, IConfiguration config)
        {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Sends a password reset email for the supplied address and token.
        /// If a member with the email is found and is an Admin, the reset link targets the admin route.
        /// Otherwise the public reset route is used.
        /// SMTP behavior is controlled via environment variables; if not present the link is written to console.
        /// </summary>
        public async Task SendResetPasswordEmailAsync(string email, string nick, string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    Console.Error.WriteLine("[EmailHelper] No email provided, aborting send.");
                    return;
                }

                // Lookup the member to determine type (admin vs user)
                if (_mongo == null)
                {
                    Console.Error.WriteLine("[EmailHelper] MongoDBService is not available.");
                    return;
                }

                var members = _mongo.Database.GetCollection<Member>("members");
                var member = await members.Find(m => m.MemberEmail == email).FirstOrDefaultAsync();

                bool isAdmin = member != null && member.MemberType == MemberType.Admin;

                // Default links match the legacy project (change if you want configurable hosts)
                var resetLink = isAdmin
                    ? $"http://localhost:5001/admin/reset-password/{token}"
                    : $"http://localhost:3000/reset-password/{token}";

                Console.WriteLine($"[EmailHelper] Prepared reset link: {resetLink}");

                // Read SMTP configuration from appsettings (Smtp section) with env var fallback
                var host = _config["Smtp:Host"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
                var portStr = _config["Smtp:Port"] ?? Environment.GetEnvironmentVariable("SMTP_PORT");
                var user = _config["Smtp:User"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
                var pass = _config["Smtp:Pass"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");
                var from = _config["Smtp:From"] ?? Environment.GetEnvironmentVariable("SMTP_FROM") ?? user ?? "no-reply@example.com";

                string subject = "Password Reset Request";
                string htmlBody = $@"<p>Hi {WebUtility.HtmlEncode(nick ?? string.Empty)},</p>
                <p>You requested a password reset. Click the link below to reset your password:</p>
                <p><a href=""{resetLink}"">Reset Password</a></p>
                <p>This link will expire in 30 minutes.</p>";

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portStr) || !int.TryParse(portStr, out var port))
                {
                    // SMTP not configured — fallback to console
                    Console.WriteLine("[EmailHelper] SMTP not configured, skipping send. Reset link:");
                    Console.WriteLine(resetLink);
                    return;
                }

                try
                {
                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = true
                    };

                    if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                    {
                        client.Credentials = new NetworkCredential(user, pass);
                    }

                    using var msg = new MailMessage(from, email, subject, htmlBody)
                    {
                        IsBodyHtml = true
                    };

                    // Send asynchronously
                    await client.SendMailAsync(msg);

                    Console.WriteLine($"[EmailHelper] Sent reset email to {email}");
                }
                catch (Exception ex)
                {
                    // Log error and continue — the caller may handle user-facing messaging
                    Console.Error.WriteLine($"[EmailHelper] Failed to send reset email to {email}: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EmailHelper] Unexpected error while sending reset email: {ex?.Message}");
                Console.Error.WriteLine(ex?.StackTrace);
            }
        }
    }
}
