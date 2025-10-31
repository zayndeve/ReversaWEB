using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebApplication1.Services;
using WebApplication1.Models;
using WebApplication1.Exceptions;
using WebApplication1.Enums;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/member")]
    public class MemberController : ControllerBase
    {
        private readonly MemberService _memberService;
        private readonly AuthService _authService;
        private readonly ILogger<MemberController> _logger;

        public MemberController(MemberService memberService, AuthService authService, ILogger<MemberController> logger)
        {
            _memberService = memberService;
            _authService = authService;
            _logger = logger;
        }

        // ===== SPA pages (for testing) =====
        [HttpGet("")]
        public IActionResult GoHome()
        {
            try
            {
                return Ok("Home page");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error goHome");
                return StatusCode(500, "Error loading home page");
            }
        }

        [HttpGet("login")]
        public IActionResult GetLogin()
        {
            return Ok("Login page");
        }

        [HttpGet("signup")]
        public IActionResult GetSignup()
        {
            return Ok("Signup page");
        }

        // ===== Signup =====
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromForm] MemberInput input, IFormFile? memberImage)
        {
            try
            {
                _logger.LogInformation("signup");

                if (memberImage != null)
                    input.MemberImage = memberImage.FileName;

                var result = await _memberService.SignupAsync(input);

                // create session (you can later replace with JWT)
                HttpContext.Session.SetString("MemberId", result.Id);
                HttpContext.Session.SetString("MemberNick", result.MemberNick);
                HttpContext.Session.SetString("MemberType", result.MemberType.ToString());

                return Created("", new { member = result });
            }
            catch (AppException ex)
            {
                return StatusCode((int)ex.Code, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in signup");
                return StatusCode(500, new { success = false, message = "Something went wrong" });
            }
        }

        // ===== Login =====
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginInput input)
        {
            try
            {
                _logger.LogInformation("login");

                var result = await _memberService.LoginAsync(input);

                // Store auth session
                HttpContext.Session.SetString("MemberId", result.Id);
                HttpContext.Session.SetString("MemberNick", result.MemberNick);
                HttpContext.Session.SetString("MemberType", result.MemberType.ToString());

                return Ok(new { member = result });
            }
            catch (AppException ex)
            {
                return StatusCode((int)ex.Code, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error login");
                return StatusCode(500, new { success = false, message = "Something went wrong" });
            }
        }

        // ===== Logout =====
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                _logger.LogInformation("logout");
                HttpContext.Session.Clear();
                return Ok(new { logout = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logout");
                return StatusCode(500, new { success = false, message = "Logout failed" });
            }
        }

        // ===== Verify session auth =====
        [HttpGet("verify")]
        public IActionResult VerifyAuth()
        {
            try
            {
                var memberId = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberId))
                {
                    throw new AppException(HttpCode.Unauthorized, ErrorMessage.NotAuthenticated);
                }

                return Ok(new { authenticated = true, memberId });
            }
            catch (AppException ex)
            {
                return StatusCode((int)ex.Code, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifyAuth");
                return StatusCode(500, new { success = false, message = "Something went wrong" });
            }
        }

        // ===== Retrieve session member (optional) =====
#pragma warning disable ASP0023 // Route conflict detected between controller actions
        [HttpGet("me")]
#pragma warning restore ASP0023 // Route conflict detected between controller actions
        public IActionResult RetrieveAuth()
        {
            try
            {
                var memberNick = HttpContext.Session.GetString("MemberNick");
                if (string.IsNullOrEmpty(memberNick))
                {
                    return Unauthorized(new { success = false, message = "No session found" });
                }

                return Ok(new { memberNick });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieveAuth");
                return StatusCode(500, new { success = false, message = "Something went wrong" });
            }
        }


        // ===== Password Reset ===== //

        [HttpPost("request-password")]
        public async Task<IActionResult> RequestPassword([FromBody] PasswordResetRequestInput input)
        {
            try
            {
                _logger.LogInformation("member requestPassword");

                var result = await _memberService.RequestPasswordAsync(input);

                return Ok(new { result, error = false });
            }
            catch (AppException ex)
            {
                _logger.LogWarning("Error, member requestPassword: {Message}", ex.Message);
                return StatusCode((int)ex.Code, new { message = ex.Message, error = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error, member requestPassword");
                return StatusCode(400, new { message = "Something went wrong", error = true });
            }
        }

        [HttpPost("reset-password/{token}")]
        public async Task<IActionResult> ResetPassword(string token, [FromBody] dynamic body)
        {
            try
            {
                _logger.LogInformation("member resetPassword");

                string? newPassword = body?.newPassword;
                if (string.IsNullOrEmpty(newPassword))
                    return BadRequest(new { message = "Missing new password", error = true });

                await _memberService.ResetPasswordAsync(token, newPassword!);

                return Ok(new { message = "Password reset successful!" });
            }
            catch (AppException ex)
            {
                _logger.LogWarning("Error, member resetPassword: {Message}", ex.Message);
                return StatusCode((int)ex.Code, new { message = ex.Message, error = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error, member resetPassword");
                return StatusCode(400, new { message = "Something went wrong", error = true });
            }
        }


        // ===== Update Self ===== //

        [HttpPost("update-self")]
        public async Task<IActionResult> UpdateSelf([FromForm] MemberUpdateInput input, IFormFile? memberImage)
        {
            try
            {
                // ✅ Verify session authentication
                var memberId = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberId))
                {
                    return StatusCode(401, new
                    {
                        code = 401,
                        message = "Not authenticated"
                    });
                }

                // ✅ Attach image if uploaded
                if (memberImage != null)
                {
                    input.MemberImage = memberImage.FileName;
                }

                // ✅ Update current user
                var updated = await _memberService.UpdateAdminDataAsync(memberId, input);

                // ✅ Update session info
                HttpContext.Session.SetString("MemberNick", updated.MemberNick ?? "User");
                await HttpContext.Session.CommitAsync();

                // ✅ Return updated profile
                return Ok(new { success = true, data = updated });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ updateSelf error");
                return StatusCode(500, new
                {
                    code = 500,
                    message = "Update failed"
                });
            }
        }
#pragma warning disable ASP0023 // Route conflict detected between controller actions
        [HttpGet("me")]
#pragma warning restore ASP0023 // Route conflict detected between controller actions
        public async Task<IActionResult> GetSelf()
        {
            try
            {
                // ✅ Check authentication from session
                var memberIdString = HttpContext.Session.GetString("MemberId");
                if (string.IsNullOrEmpty(memberIdString))
                {
                    return StatusCode(401, new
                    {
                        code = 401,
                        message = "Not authenticated"
                    });
                }

                // ✅ Re-fetch fresh member info from MongoDB
                var fullMember = await _memberService.GetByIdAsync(memberIdString);

                return Ok(fullMember); // ✅ Return up-to-date member
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSelf");
                return StatusCode(500, new { message = "Server error" });
            }
        }

    }
}
