using EnTouch.API.Services;
using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnTouch.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly OnlineUsersService _onlineUsers;
        private readonly IFirebaseNotificationService _notificationService;

        public ChatHub(ApplicationDbContext context, OnlineUsersService onlineUsers, IFirebaseNotificationService notificationService)
        {
            _context = context;
            _onlineUsers = onlineUsers;
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                _onlineUsers.UserConnected(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                _onlineUsers.UserDisconnected(userId);

                var user = await _context.Users.FindAsync(userId);

                if (user != null)
                {
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string receiverId, string content, string messageType, string? mediaUrl = null)
        {
            try
            {
                var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(senderId))
                    throw new HubException("Unauthorized");

                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content ?? "",
                    MessageType = messageType,
                    VideoPath = mediaUrl,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    IsDelivered = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var device = await _context.UserDevices
                            .Where(d => d.UserId == receiverId)
                            .OrderByDescending(d => d.CreatedAt)
                            .FirstOrDefaultAsync();

                if (device != null)
                {
                    await _notificationService.SendMessageNotificationAsync(
                        device.FcmToken,
                        senderName ?? "New message",
                        content,
                        messageType,
                        senderId,
                        message.Id.ToString());
                }

                var receiverConnectionId = _onlineUsers.GetConnectionId(receiverId);

                if (!string.IsNullOrEmpty(receiverConnectionId))
                {
                    message.IsDelivered = true;
                    await _context.SaveChangesAsync();

                    await Clients.Client(receiverConnectionId).SendAsync(
                        "ReceivePrivateMessage",
                        senderId, senderName, message.Content, messageType, 
                        mediaUrl, message.SentAt, message.Id
                    );
                }

                await Clients.Caller.SendAsync("MessageSent", message.Id, message.IsDelivered);
            }
            catch (Exception ex)
            {
                throw new HubException($"{ex.Message} >> {ex.InnerException?.Message}");
            }
        }

        public async Task Typing(string receiverId)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            var receiverConnectionId = _onlineUsers.GetConnectionId(receiverId);

            if (!string.IsNullOrEmpty(receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId)
                    .SendAsync("UserTyping", senderId, senderName);
            }
        }

        public async Task MarkMessageAsRead(Guid messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message != null)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();

                var senderConnection = _onlineUsers.GetConnectionId(message.SenderId);

                if (!string.IsNullOrEmpty(senderConnection))
                {
                    await Clients.Client(senderConnection).SendAsync(
                        "MessageRead",
                        messageId
                    );
                }
            }
        }
    }
}