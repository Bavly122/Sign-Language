using EnTouch.Application.DTOs;
using EnTouch.Domain.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace EnTouch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SocialAuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SocialAuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        // POST: api/socialauth/google
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] SocialLoginRequest request)
        {
            try
            {
                var settings = _configuration.GetSection("GoogleAuthSettings");
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { settings["ClientId"] }
                    });

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        FullName = payload.Name,
                        ProfileImageUrl = payload.Picture,
                        EmailConfirmed = true,
                        PreferredLanguage = "Arabic"
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                        return BadRequest(result.Errors);
                }

                var token = GenerateJwtToken(user);

                return Ok(new AuthResponse
                {
                    Token = token,
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    IsDeaf = user.IsDeaf,
                    IsMute = user.IsMute,
                    PreferredLanguage = user.PreferredLanguage
                });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }
        }

        // POST: api/socialauth/facebook
        [HttpPost("facebook")]
        public async Task<IActionResult> FacebookLogin([FromBody] SocialLoginRequest request)
        {
            var settings = _configuration.GetSection("FacebookAuthSettings");

            var appToken = $"{settings["AppId"]}|{settings["AppSecret"]}";
            var verifyUrl = $"https://graph.facebook.com/debug_token?input_token={request.IdToken}&access_token={appToken}";

            var verifyResponse = await _httpClient.GetAsync(verifyUrl);
            if (!verifyResponse.IsSuccessStatusCode)
                return Unauthorized(new { message = "Invalid Facebook token" });

            var verifyJson = await verifyResponse.Content.ReadAsStringAsync();
            var verifyData = JsonDocument.Parse(verifyJson);
            var isValid = verifyData.RootElement
                .GetProperty("data")
                .GetProperty("is_valid")
                .GetBoolean();

            if (!isValid)
                return Unauthorized(new { message = "Invalid Facebook token" });

            var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={request.IdToken}";
            var userInfoResponse = await _httpClient.GetAsync(userInfoUrl);
            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            var fbUser = JsonDocument.Parse(userInfoJson).RootElement;

            // Some Facebook accounts do not return an email
            var email = fbUser.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()
                : $"fb_{fbUser.GetProperty("id").GetString()}@facebook.com";

            var user = await _userManager.FindByEmailAsync(email!);

            if (user == null)
            {
                var picture = fbUser.TryGetProperty("picture", out var picProp)
                    ? picProp.GetProperty("data").GetProperty("url").GetString()
                    : null;

                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FullName = fbUser.GetProperty("name").GetString()!,
                    ProfileImageUrl = picture,
                    EmailConfirmed = true,
                    PreferredLanguage = "Arabic"
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
            }

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                IsDeaf = user.IsDeaf,
                IsMute = user.IsMute,
                PreferredLanguage = user.PreferredLanguage
            });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(jwtSettings["DurationInMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public record SocialLoginRequest(string IdToken);
}