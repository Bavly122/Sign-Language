namespace EnTouch.Application.DTOs
{
    public class FriendDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsDeaf { get; set; }
        public bool IsMute { get; set; }
        public string FriendshipStatus { get; set; }
    }

    public class FriendSuggestionDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsDeaf { get; set; }
        public bool IsMute { get; set; }
    }
}