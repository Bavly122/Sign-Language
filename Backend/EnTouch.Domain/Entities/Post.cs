namespace EnTouch.Domain.Entities
{
    public class Post
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Content { get; set; }

        public string? MediaUrl { get; set; }

        public string MediaType { get; set; } = "None";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
        public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    }
}