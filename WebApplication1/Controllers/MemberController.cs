using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ReversaWEB.Services;
using ReversaWEB.Core.Types;
using ReversaWEB.Core.Config;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ReversaWEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberController : ControllerBase
    {
        private readonly MemberService _memberService;
        private readonly AuthService _authService;

        public MemberController(MemberService memberService, AuthService authService)
        {
            _memberService = memberService;
            _authService = authService;
        }

        // ===== STATIC ROUTES =====
        [HttpGet("home")]
        public IActionResult GoHome() => Ok("Home page");

        [HttpGet("login-page")]
        public IActionResult GetLogin() => Ok("Login page");

        [HttpGet("signup-page")]
        public IActionResult GetSignup() => Ok("Sign-up page");

        // ===== SIGNUP =====
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromForm] MemberInput input, IFormFile? memberImage)
        {
            if (memberImage != null)
            {
                // Save uploaded file (example path)
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, memberImage.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await memberImage.CopyToAsync(stream);
                }

                input.MemberImage = memberImage.FileName;
            }

            var member = await _memberService.Signup(input);
            var token = _authService.CreateToken(MemberFilter.FilterTokenPayload(member));

            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = false,
                Expires = DateTimeOffset.Now.AddHours(AppConfig.AuthTimer)
            });

            return Created("", new { member, accessToken = token });
        }


        // ===== LOGIN =====
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginInput input)
        {
            var member = await _memberService.Login(input);
            if (member == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = _authService.CreateToken(MemberFilter.FilterTokenPayload(member));

            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = false,
                Expires = DateTimeOffset.Now.AddHours(AppConfig.AuthTimer)
            });

            return Ok(new { member, accessToken = token });
        }

        // ===== LOGOUT =====
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("accessToken", "", new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(-1)
            });
            return Ok(new { logout = true });
        }

        // ===== PASSWORD RESET =====
        [HttpPost("request-password")]
        public async Task<IActionResult> RequestPassword([FromBody] PasswordResetRequestInput input)
        {
            var result = await _memberService.RequestPassword(input);
            return Ok(result);
        }

        [HttpPost("reset-password/{token}")]
        public async Task<IActionResult> ResetPassword(string token, [FromBody] PasswordResetInput input)
        {
            await _memberService.ResetPassword(token, input.NewPassword);
            return Ok(new { message = "Password reset successful!" });
        }

        // ===== SELF MANAGEMENT =====
        [Authorize]
        [HttpPut("update-self")]
        public async Task<IActionResult> UpdateSelf([FromBody] MemberUpdateInput input)
        {
            var token = Request.Cookies["accessToken"];
            var payload = _authService.CheckAuth(token);
            if (payload == null) return Unauthorized();

            var updated = await _memberService.UpdateSelf(payload.Id, input);
            var newToken = _authService.CreateToken(MemberFilter.FilterTokenPayload(updated));

            Response.Cookies.Append("accessToken", newToken, new CookieOptions
            {
                HttpOnly = false,
                Expires = DateTimeOffset.Now.AddHours(AppConfig.AuthTimer)
            });

            return Ok(updated);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetSelf()
        {
            var token = Request.Cookies["accessToken"];
            var payload = _authService.CheckAuth(token);
            if (payload == null) return Unauthorized();

            var member = await _memberService.GetById(payload.Id);
            return Ok(member);
        }
    }
}
