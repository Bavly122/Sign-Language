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

        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo(IFormFile video)
        {
            if (video == null || video.Length == 0)
                return BadRequest("No video uploaded");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "videos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(video.FileName);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await video.CopyToAsync(stream);
            }

            return Ok(new
            {
                videoPath = fileName
            });
        }
        [HttpPost("process")]
        public async Task<IActionResult> ProcessVideo(string receiverId, string videoPath)
        {
            var senderId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Fake AI
            await Task.Delay(2000);

            var resultText = "Hello how are you";

            var message = new Message
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = resultText,
                VideoPath = videoPath,
                MessageType = "Sign",
                SentAt = DateTime.UtcNow,
                IsRead = false,
                IsDelivered = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var receiverConnection = _onlineUsers.GetConnectionId(receiverId);

            if (!string.IsNullOrEmpty(receiverConnection))
            {
                message.IsDelivered = true;
                await _context.SaveChangesAsync();

                await _hub.Clients.Client(receiverConnection).SendAsync(
                    "ReceivePrivateMessage",
                    senderId,
                    resultText,
                    "Sign",
                    videoPath,
                    message.SentAt,
                    message.Id
                );
            }

            return Ok(new
            {
                text = resultText,
                video = videoPath,
                messageId = message.Id
            });
        }
    }
}
