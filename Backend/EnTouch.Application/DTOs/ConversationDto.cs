namespace EnTouch.Application.DTOs
{
    public class ConversationDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsDeaf { get; set; }
        public bool IsMute { get; set; }
        public string LastMessage { get; set; }
        public string LastMessageType { get; set; }
        public DateTime LastMessageAt { get; set; }
        public bool IsLastMessageMine { get; set; }
        public int UnreadCount { get; set; }
    }
}