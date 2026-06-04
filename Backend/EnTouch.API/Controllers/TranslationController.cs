using EnTouch.API.Services;
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
        private readonly IServiceScopeFactory _scopeFactory;

        public TranslationController(
            ApplicationDbContext context,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTranslation(CreateTranslationDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var scope = _scopeFactory.CreateScope();
            var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
            var videoFileName = await aiService.TextToSignAsync(dto.InputText!);
            if (videoFileName == null)
                return Ok(new { success = false, message = "Could not generate sign video" });
            var translation = new Translation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InputText = dto.InputText,
                OutputVideoPath = videoFileName,
                Status = TranslationStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Translations.AddAsync(translation);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                translationId = translation.Id,
                inputText = dto.InputText,
                outputVideoUrl = $"/videos/{videoFileName}"
            });
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var translations = await _context.Translations
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TranslationResponseDto
                {
                    Id = t.Id,
                    Type = t.Type.ToString(),
                    InputText = t.InputText,
                    OutputText = t.OutputText,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(translations);
        }
    }
}