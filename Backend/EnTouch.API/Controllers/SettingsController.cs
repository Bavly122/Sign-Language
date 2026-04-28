using EnTouch.Application.DTOs;
using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnTouch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
                return Ok(new UserSettingsDto
                {
                    DarkMode = false,
                    PreferredLanguage = "Arabic",
                    AccessibilityLanguage = "Arabic"
                });

            return Ok(new UserSettingsDto
            {
                DarkMode = settings.DarkMode,
                PreferredLanguage = settings.PreferredLanguage,
                AccessibilityLanguage = settings.AccessibilityLanguage
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings(UserSettingsDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DarkMode = dto.DarkMode,
                    PreferredLanguage = dto.PreferredLanguage,
                    AccessibilityLanguage = dto.AccessibilityLanguage
                };
                await _context.UserSettings.AddAsync(settings);
            }
            else
            {
                settings.DarkMode = dto.DarkMode;
                settings.PreferredLanguage = dto.PreferredLanguage;
                settings.AccessibilityLanguage = dto.AccessibilityLanguage;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Settings updated successfully" });
        }
    }
}