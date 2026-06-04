namespace EnTouch.API.Services
{
    public interface IAIService
    {
        Task<bool> ProcessTranslationAsync(Guid translationId);
        Task<string?> SendVideoToAIAsync(string? videoPath, bool flip = false, int angle = 0);
        Task<string?> TextToSignAsync(string sentence);
    }
}