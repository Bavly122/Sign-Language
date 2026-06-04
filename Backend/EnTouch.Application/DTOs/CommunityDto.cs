namespace EnTouch.Application.DTOs
{
    public class CreatePostDto
    {
        public string Content { get; set; }
        public string? MediaUrl { get; set; }
        public string MediaType { get; set; } = "None";
    }

    public class PostResponseDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public string? UserProfileImage { get; set; }
        public string Content { get; set; }
        public string? MediaUrl { get; set; }
        public string MediaType { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByMe { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCommentDto
    {
        public string Content { get; set; }
    }

    public class CommentResponseDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public string? UserProfileImage { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}