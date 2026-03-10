using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EnTouch.API.Services
{
    public class AIService : IAIService
    {
        private readonly ApplicationDbContext _context;

        public AIService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ProcessTranslationAsync(Guid translationId)
        {
            try
            {
                var translation = await _context.Translations
                    .FirstOrDefaultAsync(t => t.Id == translationId);

                if (translation == null)
                    return false;

                // Processing
                translation.Status = TranslationStatus.Processing;
                await _context.SaveChangesAsync();

                // Simulate AI work
                await Task.Delay(3000);

                // Complete
                translation.OutputText = "Simulated AI Result";
                translation.Status = TranslationStatus.Completed;

                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}