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
    public class TranslationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TranslationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTranslation(CreateTranslationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var translation = new Translation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = dto.Type,
                InputText = dto.InputText,
                InputVideoPath = dto.InputVideoPath,
                Status = TranslationStatus.Pending
            };

            _context.Translations.Add(translation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                translation.Id,
                translation.Status
            });
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var translations = await _context.Translations
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(translations);
        }
    }
}