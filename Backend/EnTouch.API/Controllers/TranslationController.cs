using EnTouch.API.Services;
using EnTouch.Application.DTOs;
using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public TranslationController(
            ApplicationDbContext context,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _env = env;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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
                    OutputVideoPath = t.OutputVideoPath,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(translations);
        }
        [HttpPost("Transcript")]
        public async Task<IActionResult> VideoToText(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
                return BadRequest(new { message = "No video uploaded" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var apiKey = _configuration["OpenAI:ApiKey"];
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            string? transcribedText = null;

            try
            {
                using var formData = new MultipartFormDataContent();
                var fileBytes = new byte[videoFile.Length];
                using var ms = new MemoryStream();
                await videoFile.CopyToAsync(ms);
                var fileContent = new ByteArrayContent(ms.ToArray());
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(videoFile.ContentType);
                formData.Add(fileContent, "file", videoFile.FileName);
                formData.Add(new StringContent("whisper-large-v3"), "model");
                formData.Add(new StringContent("en"), "language");

                var whisperResponse = await httpClient.PostAsync(
                    "https://api.groq.com/openai/v1/audio/transcriptions", formData);

                if (whisperResponse.IsSuccessStatusCode)
                {
                    var whisperJson = await whisperResponse.Content.ReadAsStringAsync();
                    var whisperResult = System.Text.Json.JsonSerializer
                        .Deserialize<System.Text.Json.JsonElement>(whisperJson);
                    transcribedText = whisperResult.GetProperty("text").GetString();
                }
                else
                {
                    var errorBody = await whisperResponse.Content.ReadAsStringAsync();
                    return Ok(new { success = false, message = errorBody });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }

            if (transcribedText == null)
                return Ok(new { success = false, message = "Could not transcribe audio" });

            var translation = new Translation
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Type = TranslationType.SignToText,
                OutputText = transcribedText,
                Status = TranslationStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Translations.AddAsync(translation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                translationId = translation.Id,
                transcribedText = transcribedText
            });
        }
    }
}