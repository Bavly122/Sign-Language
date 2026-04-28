using EnTouch.Application.DTOs;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnTouch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DictionaryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DictionaryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? language)
        {
            var query = _context.DictionarySigns.AsQueryable();

            if (!string.IsNullOrEmpty(language))
                query = query.Where(d => d.Language == language);

            var signs = await query
                .Select(d => new DictionarySignDto
                {
                    Id = d.Id,
                    Word = d.Word,
                    Language = d.Language,
                    VideoPath = d.VideoPath,
                    Description = d.Description
                })
                .ToListAsync();

            return Ok(signs);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrEmpty(q))
                return BadRequest("Search query is required");

            var signs = await _context.DictionarySigns
                .Where(d => d.Word.Contains(q))
                .Select(d => new DictionarySignDto
                {
                    Id = d.Id,
                    Word = d.Word,
                    Language = d.Language,
                    VideoPath = d.VideoPath,
                    Description = d.Description
                })
                .ToListAsync();

            return Ok(signs);
        }

        [HttpGet("sign-of-the-day")]
        public async Task<IActionResult> GetSignOfTheDay()
        {
            var count = await _context.DictionarySigns.CountAsync();

            if (count == 0)
                return NotFound("No signs available");

            var index = DateTime.UtcNow.DayOfYear % count;

            var sign = await _context.DictionarySigns
                .Skip(index)
                .Select(d => new DictionarySignDto
                {
                    Id = d.Id,
                    Word = d.Word,
                    Language = d.Language,
                    VideoPath = d.VideoPath,
                    Description = d.Description
                })
                .FirstOrDefaultAsync();

            return Ok(sign);
        }
    }
}