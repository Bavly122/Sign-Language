using EnTouch.API.Services;
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
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("conversation/{userId}")]
        public async Task<IActionResult> GetConversation(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender.FullName,
                    m.ReceiverId,
                    m.Content,
                    m.VideoPath,
                    m.MessageType,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }
        [HttpPost("mark-as-read/{userId}")]
        public async Task<IActionResult> MarkAsRead(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == userId &&
                            m.ReceiverId == currentUserId &&
                            !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers([FromServices] OnlineUsersService onlineUsers)
        {
            var users = onlineUsers.GetOnlineUsers();
            return Ok(users);
        }
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadMessagesCount()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unreadCounts = await _context.Messages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UnreadCount = g.Count()
                })
                .ToListAsync();

            return Ok(unreadCounts);
        }
        [HttpGet("last-seen/{userId}")]
        public async Task<IActionResult> GetLastSeen(string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                userId = user.Id,
                lastSeen = user.LastSeen
            });
        }

    }
}