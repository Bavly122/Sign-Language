using EnTouch.Domain.Entities;

namespace EnTouch.API.Services
{
    public interface IAIService
    {
        Task<bool> ProcessTranslationAsync(Guid translationId);
    }
}