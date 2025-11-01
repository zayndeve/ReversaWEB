using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Services; // your service folder
using WebApplication1.Models;
using WebApplication1.Enums;   // for DTOs and enums if needed
using WebApplication1.Exceptions;

namespace WebApplication1.Controllers
{
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
                var result = await _memberService.GetUsersAsync(); // implement this in service
                ViewData["CurrentPath"] = "/admin/user/all";
                return View("Users", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error GetUsers");
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "❌ Unable to load users.";
                return RedirectToAction("GetLogin", "Admin");
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
                TempData["AlertMessage"] = "❌ Unable to open support page.";
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
                    var saved = await ReversaWEB.Core.Utils.FileUploader.SaveFileAsync(memberImage, "members");
                    newMember.MemberImage = saved;
                }

                var result = await _memberService.ProcessSignupAsync(newMember);

                // store admin info in session
                HttpContext.Session.SetString("MemberId", result.Id.ToString());
                HttpContext.Session.SetString("MemberNick", result.MemberNick ?? "Admin");
                // store image name in session if present
                HttpContext.Session.SetString("MemberImage", result.MemberImage ?? string.Empty);

                // simulate alert + redirect
                TempData["AlertMessage"] = "✅ Signup successful! Please log in.";
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
                HttpContext.Session.SetString("MemberId", result.Id.ToString());
                HttpContext.Session.SetString("MemberNick", result.MemberNick ?? "Admin");
                // store image name so navbar can show avatar
                HttpContext.Session.SetString("MemberImage", result.MemberImage ?? string.Empty);

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

        [HttpGet("admin/logout")]
        public async Task<IActionResult> ProcessLogout()
        {
            try
            {
                _logger.LogInformation("processLogout");

                HttpContext.Session.Clear();
                await HttpContext.Session.CommitAsync();

                return RedirectToAction("GoHome", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ProcessLogout");
                TempData["AlertMessage"] = "Logout failed";
                return RedirectToAction("GoHome", "Admin");
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
                    var saved = await ReversaWEB.Core.Utils.FileUploader.SaveFileAsync(memberImage, "members");
                    input.MemberImage = saved; // saved filename stored
                }

                // ✅ get current logged-in member from session as STRING
                string? memberId = HttpContext.Session.GetString("MemberId");

                if (string.IsNullOrEmpty(memberId))
                {
                    return StatusCode(400, new { success = false, message = "Missing member ID in session" });
                }

                // ✅ pass it directly as string (no Guid.Parse)
                var result = await _memberService.UpdateAdminDataAsync(memberId, input);

                // ✅ update session data
                HttpContext.Session.SetString("MemberNick", result.MemberNick ?? "Admin");
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
            var memberType = HttpContext.Session.GetString("MemberType");
            return memberType == MemberType.Admin.ToString();
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
                // var kpis = await _analyticsService.GetKPIAsync();
                // var monthlySales = await _analyticsService.GetMonthlySalesAsync();
                // var topCategories = await _analyticsService.GetTopCategoriesAsync();
                var topBuyers = await _analyticsService.GetTopBuyersAsync();

                // _logger.LogInformation("monthlySales: {@MonthlySales}", monthlySales);
                // _logger.LogInformation("topCategories: {@TopCategories}", topCategories);
                // _logger.LogInformation("topBuyers: {@TopBuyers}", topBuyers);

                return View("TopBuyers", new
                {
                    // kpis,
                    // monthlySales,
                    // topCategories,
                    topBuyers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Analytics Dashboard Error");
                return RedirectToAction("GetDashboard", "Admin");
            }
        }

    }
}
