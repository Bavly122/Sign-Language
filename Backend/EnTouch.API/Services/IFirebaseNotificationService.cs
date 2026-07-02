namespace EnTouch.API.Services
{
    public interface IFirebaseNotificationService
    {
        Task SendMessageNotificationAsync(
            string fcmToken, 
            string senderName, 
            string messageContent, 
            string messageType,
            string senderId,
            string messageId);
    }
}