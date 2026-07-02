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
        [HttpPost("register-device")]
        public async Task<IActionResult> RegisterDevice(RegisterDeviceDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.FcmToken == dto.FcmToken);

            if (existing != null)
                return Ok(new { message = "Device already registered" });

            var device = new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FcmToken = dto.FcmToken,
                CreatedAt = DateTime.UtcNow
            };

            await _context.UserDevices.AddAsync(device);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Device registered successfully" });
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

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var allMessages = await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .ToListAsync();

            var contactIds = allMessages
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var result = new List<ConversationDto>();

            foreach (var contactId in contactIds)
            {
                var lastMessage = allMessages
                    .Where(m =>
                        (m.SenderId == currentUserId && m.ReceiverId == contactId) ||
                        (m.SenderId == contactId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .First();

                var unreadCount = allMessages.Count(m =>
                    m.SenderId == contactId &&
                    m.ReceiverId == currentUserId &&
                    !m.IsRead);

                var contact = await _context.Users.FindAsync(contactId);
                if (contact == null) continue;

                result.Add(new ConversationDto
                {
                    UserId = contact.Id,
                    FullName = contact.FullName,
                    ProfileImageUrl = contact.ProfileImageUrl,
                    IsDeaf = contact.IsDeaf,
                    IsMute = contact.IsMute,
                    LastMessage = lastMessage.MessageType.ToLower() == "text"
                            ? lastMessage.Content
                            : lastMessage.MessageType.ToLower() == "video" ? "Video" : "Sign",
                    LastMessageType = lastMessage.MessageType,
                    LastMessageAt = lastMessage.SentAt,
                    IsLastMessageMine = lastMessage.SenderId == currentUserId,
                    UnreadCount = unreadCount
                });
            }

            var sorted = result.OrderByDescending(c => c.LastMessageAt).ToList();

            return Ok(sorted);
        }

    }
}