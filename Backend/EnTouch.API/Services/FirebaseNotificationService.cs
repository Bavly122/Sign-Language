using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace EnTouch.API.Services
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("firebase-service-account.json")
                });
            }
        }

        public async Task SendMessageNotificationAsync(
                                    string fcmToken,
                                    string senderName,
                                    string messageContent,
                                    string messageType,
                                    string senderId,
                                    string messageId)
        {
            try
            {
                string body = messageType.ToLower() switch
                {
                    "text" => messageContent,
                    "video" => "Sent a video",
                    "sign" => "Sent a sign",
                    _ => "Sent a message"
                };

                var message = new Message
                {
                    Token = fcmToken,
                    Notification = new Notification
                    {
                        Title = senderName,
                        Body = body
                    },
                    Data = new Dictionary<string, string>
            {
                { "senderName", senderName },
                { "content", messageContent },
                { "messageType", messageType },
                { "senderId", senderId },
                { "messageId", messageId }
            }
                };

                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification");
            }
        }
    }
}