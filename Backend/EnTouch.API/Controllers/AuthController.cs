using EnTouch.Domain.Entities;
using EnTouch.Application.DTOs;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        var userExists = await _userManager.FindByEmailAsync(model.Email);
        if (userExists != null)
            return BadRequest("User already exists");

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Email,
            IsDeaf = model.IsDeaf,
            IsMute = model.IsMute,
            PreferredLanguage = "Arabic"
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var accessToken = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshToken(user.Id);

        return Ok(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsDeaf = user.IsDeaf,
            IsMute = user.IsMute,
            PreferredLanguage = user.PreferredLanguage
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
            return Unauthorized("Invalid credentials");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);

        if (!isPasswordValid)
            return Unauthorized("Invalid credentials");

        var accessToken = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshToken(user.Id);

        return Ok(new AuthResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsDeaf = user.IsDeaf,
            IsMute = user.IsMute,
            PreferredLanguage = user.PreferredLanguage
        });
    }

    // POST: api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (storedToken == null)
            return Unauthorized(new { message = "Invalid refresh token" });

        if (storedToken.IsRevoked)
            return Unauthorized(new { message = "Refresh token has been revoked" });

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Refresh token has expired, please login again" });

        // Revoke old token
        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        // Generate new tokens
        var newAccessToken = GenerateJwtToken(storedToken.User);
        var newRefreshToken = await GenerateRefreshToken(storedToken.UserId);

        return Ok(new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Logged out successfully" });
    }

    // ── Helper Methods ───────────────────────────────────────

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]));

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

    private async Task<string> GenerateRefreshToken(string userId)
    {
        // Revoke any existing active tokens for this user
        var existingTokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();

        foreach (var t in existingTokens)
            t.IsRevoked = true;

        // Generate new random token
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        return token;
    }
}