using EnTouch.API.Hubs;
using EnTouch.API.Services;
using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EnTouch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignLanguageController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hub;
        private readonly OnlineUsersService _onlineUsers;

        public SignLanguageController(
            IWebHostEnvironment env,
            ApplicationDbContext context,
            IHubContext<ChatHub> hub,
            OnlineUsersService onlineUsers)
        {
            _env = env;
            _context = context;
            _hub = hub;
            _onlineUsers = onlineUsers;
        }

        [HttpPost("translate")]
        public async Task<IActionResult> TranslateVideo(
                        IFormFile video,
                        [FromServices] IAIService aiService,
                        bool flip = false)
        {
            if (video == null || video.Length == 0)
                return BadRequest("No video uploaded");

            
            var uploadsFolder = Path.Combine(_env.WebRootPath, "videos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(video.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await video.CopyToAsync(stream);

            
            var rawResult = await aiService.SendVideoToAIAsync(fileName);

            if (rawResult == null)
                return Ok(new { success = false, message = "Could not recognize sign" });

            var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(rawResult);
            var prediction = parsed.GetProperty("prediction").GetString();
            var confidence = Math.Round(parsed.GetProperty("confidence").GetDouble() * 100, 2);

            return Ok(new
            {
                success = true,
                prediction = prediction,
                confidence = $"{confidence}%",
                videoPath = fileName
            });
        }
    }
}