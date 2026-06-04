using EnTouch.API.Services;
using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class AIService : IAIService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;

    private const string FastApiUrl = "https://a7medwa2l-ahmedwael.hf.space/predict";

    public AIService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _env = env;
    }

    public async Task<bool> ProcessTranslationAsync(Guid translationId)
    {
        try
        {
            var translation = await _context.Translations
                .FirstOrDefaultAsync(t => t.Id == translationId);

            if (translation == null) return false;

            translation.Status = TranslationStatus.Processing;
            await _context.SaveChangesAsync();

            
            var resultText = await SendVideoToAIAsync(translation.InputVideoPath);

            translation.OutputText = resultText ?? "Could not process video";
            translation.Status = resultText != null
                ? TranslationStatus.Completed
                : TranslationStatus.Failed;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> SendVideoToAIAsync(string? videoPath, bool flip = false, int angle = 0)
    {
        if (string.IsNullOrEmpty(videoPath)) return null;

        try
        {
            
            var fullPath = Path.Combine(_env.WebRootPath, "videos", videoPath);
            if (!File.Exists(fullPath)) return null;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            using var form = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
            form.Add(fileContent, "file", Path.GetFileName(fullPath));

            var url = $"{FastApiUrl}?flip={flip.ToString().ToLower()}&angle={angle}";
            var response = await client.PostAsync(url, form);

            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadAsStringAsync();
            
            return result;
        }
        catch
        {
            return null;
        }
    }
    private const string TextToSignUrl = "https://a7medwa2l-sign-language-api.hf.space/generate";

    public async Task<string?> TextToSignAsync(string sentence)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var body = new { sentence = sentence };
            var json = System.Text.Json.JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(TextToSignUrl, content);
            if (!response.IsSuccessStatusCode) return null;

            
            var videoBytes = await response.Content.ReadAsByteArrayAsync();

            
            var uploadsFolder = Path.Combine(_env.WebRootPath, "videos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}.mp4";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await File.WriteAllBytesAsync(filePath, videoBytes);

            return fileName;
        }
        catch
        {
            return null;
        }
    }
}