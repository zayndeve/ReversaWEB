using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Services; // your service folder
using WebApplication1.Models;
using WebApplication1.Types;
using WebApplication1.Enums;   // for DTOs and enums if needed
using WebApplication1.Exceptions;
using WebApplication1.Core.Utils;

namespace WebApplication1.Controllers
{
    // Admin Controller for Razor views
    // Session isolation: Uses "Admin_" prefixed keys (e.g., Admin_MemberId) to avoid conflicts with User sessions.
    // User sessions use "User_" prefixed keys. Both share the same session cookie but have isolated data.
    public class AdminController : Controller
    {
        private readonly MemberService _memberService;
        private readonly AnalyticsService _analyticsService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            MemberService memberService,
            AnalyticsService analyticsService,
            ILogger<AdminController> logger)
        {
            _memberService = memberService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        // ==== Test ==== //
        [HttpGet("admin/update")]
        public IActionResult GetUpdateAdmin()
        {
            try
            {
                _logger.LogInformation("getUpdateAdminHome");
                return View("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetUpdateAdmin");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Failed to load profile page.";
                return RedirectToAction("GetDashboard", "Admin");
            }
        }

        // ====== SPA ====== //
        [HttpGet("admin/home")]
        public IActionResult GoHome()
        {
            try
            {
                _logger.LogInformation("goHome");
                return View("Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GoHome");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to open home page.";
                return RedirectToAction("GetLogin", "Admin");
            }
        }

        [HttpGet("admin/signup")]
        public IActionResult GetSignup()
        {
            try
            {
                _logger.LogInformation("getSignup");
                return View("Signup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetSignup");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to open signup page.";
                return RedirectToAction("GetLogin", "Admin");
            }
        }

        [HttpGet("admin/login")]
        public IActionResult GetLogin()
        {
            try
            {
                _logger.LogInformation("getLogin");
                return View("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetLogin");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to open login page.";
                return RedirectToAction("GoHome", "Admin");
            }
        }

        // ==== Dashboard ==== //
        [HttpGet("admin/dashboard")]
        public IActionResult GetDashboard()
        {
            try
            {
                _logger.LogInformation("getDashboard");
                return View("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to load dashboard.";
                return RedirectToAction("GetLogin", "Admin");
            }
        }

        // ==== Profile ==== //
        [HttpGet("admin/profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var adminId = HttpContext.Session.GetString("Admin_MemberId");
                if (string.IsNullOrEmpty(adminId))
                {
                    TempData["AlertType"] = "danger";
                    TempData["AlertMessage"] = "Not authenticated.";
                    return RedirectToAction("GetLogin", "Admin");
                }

                var admin = await _memberService.GetByIdAsync(adminId);

                // Sync session data with DB values
                // Using Admin_ prefixed session keys for isolated admin session data
                HttpContext.Session.SetString("Admin_MemberNick", admin.MemberNick ?? "Admin");
                HttpContext.Session.SetString("Admin_MemberEmail", admin.MemberEmail ?? string.Empty);
                HttpContext.Session.SetString("Admin_MemberPhone", admin.MemberPhone ?? string.Empty);
                HttpContext.Session.SetString("Admin_MemberAddress", admin.MemberAddress ?? string.Empty);
                HttpContext.Session.SetString("Admin_MemberDesc", admin.MemberDesc ?? string.Empty);
                HttpContext.Session.SetString("Admin_MemberImage", admin.MemberImage ?? string.Empty);
                await HttpContext.Session.CommitAsync();

                return View("Profile", admin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin profile");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "Failed to load profile.";
                return RedirectToAction("GetDashboard", "Admin");
            }
        }

        // ==== Request Password ==== //
        [HttpGet("admin/request-password")]
        public IActionResult GetRequestPassword()
        {
            try
            {
                _logger.LogInformation("getRequestPassword");
                return View("RequestPassword");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetRequestPassword");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to open password request page.";
                return RedirectToAction("GoHome", "Admin");
            }
        }

        // ==== Reset Password ==== //
        [HttpGet("admin/reset-password/{token?}")]
        public IActionResult GetResetPassword(string? token)
        {
            try
            {
                _logger.LogInformation("getResetPassword");
                ViewData["Token"] = token;
                return View("ResetPassword");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetResetPassword");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to open reset password page.";
                return RedirectToAction("GetRequestPassword", "Admin");
            }
        }

        // ==== Users ==== //
        [HttpGet("admin/users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                _logger.LogInformation("getUsers");

                var users = await _memberService.GetUsersAsync();
                ViewData["CurrentPath"] = "/admin/user/all";

                if (users == null || !users.Any())
                {
                    _logger.LogWarning("No users found in database.");
                    TempData["AlertType"] = "warning";
                    TempData["AlertMessage"] = "⚠️ No users found in the system.";
                    return View("Users", new List<WebApplication1.Models.Member>()); //  empty list, no redirect
                }

                return View("Users", users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetUsers");

                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to load users due to a server error.";

                // ❌ Instead of redirecting to login, stay on the Users page gracefully
                return View("Users", new List<WebApplication1.Models.Member>());
            }
        }

        // ==== Admin Support ==== //
        [HttpGet("admin/support")]
        public IActionResult AdminSupportPage()
        {
            try
            {
                _logger.LogInformation("adminSupportPage");
                return View("AdminSupport");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error AdminSupportPage");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = " Unable to open support page.";
                return RedirectToAction("GetDashboard", "Admin");
            }
        }
        // ====== SSR / Authentication ====== //

        [HttpPost("admin/signup")]
        public async Task<IActionResult> ProcessSignup(MemberInput newMember, IFormFile? memberImage)
        {
            try
            {
                _logger.LogInformation("processSignup");

                // assign admin role and image filename
                newMember.MemberType = MemberType.Admin;
                if (memberImage != null)
                {
                    // save admin image into wwwroot/uploads/members
                    var saved = await WebApplication1.Core.Utils.FileUploader.SaveFileAsync(memberImage, "members");
                    newMember.MemberImage = saved;
                }

                var result = await _memberService.ProcessSignupAsync(newMember);

                // store admin info in session
                // Admin session keys are prefixed with "Admin_" to isolate from User sessions
                HttpContext.Session.SetString("Admin_MemberId", result.Id?.ToString() ?? "");
                HttpContext.Session.SetString("Admin_MemberNick", result.MemberNick ?? "Admin");
                HttpContext.Session.SetString("Admin_MemberType", "Admin"); // Set admin type
                // store image name in session if present
                HttpContext.Session.SetString("Admin_MemberImage", result.MemberImage ?? string.Empty);
                HttpContext.Session.SetString("Admin_MemberEmail", result.MemberEmail ?? "");
                HttpContext.Session.SetString("Admin_MemberPhone", result.MemberPhone ?? "");
                HttpContext.Session.SetString("Admin_MemberAddress", result.MemberAddress ?? "");
                HttpContext.Session.SetString("Admin_MemberDesc", result.MemberDesc ?? "");

                // simulate alert + redirect
                TempData["AlertMessage"] = " Signup successful! Please log in.";
                return RedirectToAction("GetLogin", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ProcessSignup");
                var message = ex is AppException ? ex.Message : "Something went wrong.";
                TempData["AlertMessage"] = message;
                return RedirectToAction("GetLogin", "Admin");
            }
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> ProcessLogin(LoginInput input)
        {
            try
            {
                _logger.LogInformation("processLogin");

                var result = await _memberService.ProcessLoginAsync(input);

                // regenerate session manually (ASP.NET Core handles it differently)
                await HttpContext.Session.LoadAsync();
                HttpContext.Session.Clear();

                // store admin info
                // Admin session keys are prefixed with "Admin_" to isolate from User sessions
                HttpContext.Session.SetString("Admin_MemberId", result.Id?.ToString() ?? "");
                HttpContext.Session.SetString("Admin_MemberNick", result.MemberNick ?? "Admin");
                HttpContext.Session.SetString("Admin_MemberType", "Admin"); // Set admin type
                // store image name so navbar can show avatar
                HttpContext.Session.SetString("Admin_MemberImage", result.MemberImage ?? string.Empty);
                HttpContext.Session.SetString("Admin_MemberEmail", result.MemberEmail ?? "");
                HttpContext.Session.SetString("Admin_MemberPhone", result.MemberPhone ?? "");
                HttpContext.Session.SetString("Admin_MemberAddress", result.MemberAddress ?? "");
                HttpContext.Session.SetString("Admin_MemberDesc", result.MemberDesc ?? "");

                await HttpContext.Session.CommitAsync();

                return RedirectToAction("GetDashboard", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ProcessLogin");
                var message = ex is AppException ? ex.Message : "Something went wrong.";
                // Error example
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "No member found with that nickname.";
                return RedirectToAction("GetLogin", "Admin");
            }
        }
        // ====== Password Reset ====== //

        [HttpPost("admin/request-password")]
        public async Task<IActionResult> RequestPassword(PasswordResetRequestInput input)
        {
            try
            {
                _logger.LogInformation("requestPassword");

                var result = await _memberService.RequestPasswordAsync(input);


                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "A password reset link has been sent to your registered email address.";
                return RedirectToAction("GetRequestPassword");
            }
            catch (AppException ex)
            {
                _logger.LogWarning(ex, "Handled app error in RequestPassword");

                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = ex.Message; // e.g. "No member found"
                return RedirectToAction("GetRequestPassword");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in RequestPassword");

                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "Something went wrong while sending the reset link. Please try again.";
                return RedirectToAction("GetRequestPassword");
            }
        }

        [HttpPost("admin/reset-password/{token}")]
        public async Task<IActionResult> ResetPassword(string token, [FromForm] string newPassword)
        {
            try
            {
                _logger.LogInformation("resetPassword: Token={Token}", token);

                await _memberService.ResetPasswordAsync(token, newPassword);

                TempData["AlertType"] = "success";
                TempData["AlertMessage"] = "Your password has been successfully reset. You can now log in.";
                return RedirectToAction("GetLogin");
            }
            catch (AppException ex)
            {
                _logger.LogWarning(ex, "Handled app error in ResetPassword");

                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = ex.Message; // e.g. "Token expired"
                return RedirectToAction("GetResetPassword", new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ResetPassword");

                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("GetResetPassword", new { token });
            }
        }


        // ====== Logout ====== //


        [HttpPost("admin/logout")]
        public IActionResult AdminLogout()
        {
            try
            {
                _logger.LogInformation("admin logout");

                // ✅ Only clear Admin session keys (matching the exact case used in login)
                HttpContext.Session.Remove("Admin_MemberId");
                HttpContext.Session.Remove("Admin_MemberNick");
                HttpContext.Session.Remove("Admin_MemberType");
                HttpContext.Session.Remove("Admin_MemberImage");
                HttpContext.Session.Remove("Admin_MemberEmail");
                HttpContext.Session.Remove("Admin_MemberPhone");
                HttpContext.Session.Remove("Admin_MemberAddress");
                HttpContext.Session.Remove("Admin_MemberDesc");

                return Ok(new { logout = true, message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin logout");
                return StatusCode(500, new { logout = false, message = "Logout failed" });
            }
        }




        // ====== Admin Panel (Update Data) ====== //

        [HttpPost("admin/update")]
        public async Task<IActionResult> UpdateAdminData(MemberUpdateInput input, IFormFile? memberImage)
        {
            try
            {
                _logger.LogInformation("updateAdminData");

                if (memberImage != null)
                {
                    var saved = await WebApplication1.Core.Utils.FileUploader.SaveFileAsync(memberImage, "members");
                    input.MemberImage = saved; // saved filename stored
                }

                //  get current logged-in member from session as STRING
                string? memberId = HttpContext.Session.GetString("Admin_MemberId");

                if (string.IsNullOrEmpty(memberId))
                {
                    return StatusCode(400, new { success = false, message = "Missing member ID in session" });
                }

                //  pass it directly as string (no Guid.Parse)
                var result = await _memberService.UpdateAdminDataAsync(memberId, input);

                //  update session data
                HttpContext.Session.SetString("Admin_MemberNick", result.MemberNick ?? "Admin");
                await HttpContext.Session.CommitAsync();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UpdateAdminData");
                var message = ex is AppException ? ex.Message : "Something went wrong.";
                return StatusCode(500, new { success = false, message });
            }
        }

        // ====== Update Chosen Member ====== //

        [HttpPost("admin/update-member")]
        public async Task<IActionResult> UpdateChosenMember([FromBody] MemberUpdateInput input)
        {
            try
            {
                _logger.LogInformation("updateChosenMember");

                if (input == null)
                {
                    _logger.LogWarning("UpdateChosenMember called with null input");
                    return BadRequest(new { success = false, message = "Missing request body" });
                }

                if (string.IsNullOrEmpty(input.Id))
                {
                    _logger.LogWarning("UpdateChosenMember called without Id");
                    return BadRequest(new { success = false, message = "Missing member Id" });
                }

                var result = await _memberService.UpdateChosenMemberAsync(input);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error UpdateChosenMember");
                var message = ex is AppException ? ex.Message : "Something went wrong.";
                return StatusCode(500, new { success = false, message });
            }
        }
        // ====== Check Auth Session ====== //

        [HttpGet("admin/check-auth")]
        public IActionResult CheckAuthSession()
        {
            try
            {
                var memberNick = HttpContext.Session.GetString("MemberNick");

                if (!string.IsNullOrEmpty(memberNick))
                {
                    return Content($"<script>alert('{memberNick}');</script>", "text/html");
                }
                else
                {
                    return Content($"<script>alert('Not authenticated');</script>", "text/html");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error CheckAuthSession");
                return Content($"<script>alert('Error: {ex.Message}');</script>", "text/html");
            }
        }


        // ====== Verify Admin (Middleware-style) ====== //

        private bool VerifyAdmin()
        {
            var memberType = HttpContext.Session.GetString("Admin_MemberType");
            return memberType == "Admin";
        }

        [NonAction]
        public IActionResult RequireAdminAccess()
        {
            if (!VerifyAdmin())
            {
                var message = "Not authenticated";
                return Content($"<script>alert('{message}');</script>", "text/html");
            }

            return null!;
        }


        // Example usage of VerifyAdmin in any action
        [HttpGet("admin/secure-page")]
        public IActionResult SecurePage()
        {
            var result = RequireAdminAccess();
            if (result != null) return result;

            // proceed with admin logic
            return View("Dashboard");
        }


        // ====== Analytics Dashboard ====== //
        [HttpGet("admin/analytics-dashboard")]
        public async Task<IActionResult> GetAnalyticsDashboardData()
        {
            try
            {
                // verify admin access
                if (!VerifyAdmin()) return RequireAdminAccess();

                var kpis = await _analyticsService.GetKPIAsync();
                var monthlySales = await _analyticsService.GetMonthlySalesAsync();
                var topCategories = await _analyticsService.GetTopCategoriesAsync();
                var topBuyers = await _analyticsService.GetTopBuyersAsync();

                _logger.LogInformation("✅ Analytics dashboard loaded successfully.");

                return View("TopBuyers", new
                {
                    kpis,
                    monthlySales,
                    topCategories,
                    topBuyers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Analytics Dashboard Error");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "Failed to load analytics data.";
                return RedirectToAction("GetDashboard", "Admin");
            }
        }
        // ===== Get Admin Session Member =====
        [HttpGet("admin-self")]
        public async Task<IActionResult> GetAdminSelf()
        {
            try
            {
                //  Check authentication from ADMIN session
                var adminIdString = HttpContext.Session.GetString("Admin_MemberId");
                if (string.IsNullOrEmpty(adminIdString))
                {
                    return StatusCode(401, new
                    {
                        code = 401,
                        message = "Not authenticated as admin"
                    });
                }

                //  Re-fetch fresh admin info from MongoDB
                var fullAdmin = await _memberService.GetByIdAsync(adminIdString);

                return Ok(fullAdmin); //  Return up-to-date admin
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAdminSelf");
                return StatusCode(500, new { message = "Server error" });
            }
        }

    }
}
