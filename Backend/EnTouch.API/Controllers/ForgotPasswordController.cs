using EnTouch.API.Services;
using EnTouch.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EnTouch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IMemoryCache _cache;

        public ForgotPasswordController(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ISmsService smsService,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _emailService = emailService;
            _smsService = smsService;
            _cache = cache;
        }

        // POST: api/forgotpassword/send-otp
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return NotFound(new { message = "Email not registered" });

            var otp = new Random().Next(100000, 999999).ToString();

            var cacheKey = $"otp_{request.Email}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));

            await _emailService.SendPasswordResetOtpAsync(
                user.Email!, user.FullName, otp);

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                try
                {
                    await _smsService.SendPasswordResetOtpAsync(
                        user.PhoneNumber, otp);
                }
                catch
                {
                    // SMS failed but email was sent, continue
                }
            }

            return Ok(new
            {
                message = "OTP sent to your email" +
                          (string.IsNullOrEmpty(user.PhoneNumber) ? "" : " and phone number")
            });
        }

        // POST: api/forgotpassword/verify-otp
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var cacheKey = $"otp_{request.Email}";

            if (!_cache.TryGetValue(cacheKey, out string? storedOtp))
                return BadRequest(new { message = "OTP expired, please request a new one" });

            if (storedOtp != request.Otp)
                return BadRequest(new { message = "Invalid OTP" });

            var verifiedKey = $"otp_verified_{request.Email}";
            _cache.Set(verifiedKey, true, TimeSpan.FromMinutes(10));

            return Ok(new { message = "OTP verified successfully" });
        }

        // POST: api/forgotpassword/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var verifiedKey = $"otp_verified_{request.Email}";

            if (!_cache.TryGetValue(verifiedKey, out bool isVerified) || !isVerified)
                return BadRequest(new { message = "Please verify OTP first" });

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return BadRequest(removeResult.Errors);

            var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addResult.Succeeded)
                return BadRequest(addResult.Errors);

            _cache.Remove($"otp_{request.Email}");
            _cache.Remove(verifiedKey);

            return Ok(new { message = "Password reset successfully" });
        }
    }

    public record SendOtpRequest(string Email);
    public record VerifyOtpRequest(string Email, string Otp);
    public record ResetPasswordRequest(string Email, string NewPassword);
}