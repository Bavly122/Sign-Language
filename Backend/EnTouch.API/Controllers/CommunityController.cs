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
    public class CommunityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommunityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/community/feed
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserFullName = p.User.FullName,
                    UserProfileImage = p.User.ProfileImageUrl,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl,
                    MediaType = p.MediaType,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    IsLikedByMe = p.Likes.Any(l => l.UserId == userId),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(posts);
        }

        // POST: api/community/post
        [HttpPost("post")]
        public async Task<IActionResult> CreatePost(CreatePostDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Content is required" });

            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = dto.Content,
                MediaUrl = dto.MediaUrl,
                MediaType = dto.MediaType,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post created successfully", postId = post.Id });
        }

        // DELETE: api/community/post/{postId}
        [HttpDelete("post/{postId}")]
        public async Task<IActionResult> DeletePost(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "Post not found" });

            if (post.UserId != userId)
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Post deleted successfully" });
        }

        // POST: api/community/post/{postId}/like
        [HttpPost("post/{postId}/like")]
        public async Task<IActionResult> ToggleLike(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound(new { message = "Post not found" });

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                // Unlike
                _context.PostLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Unliked", liked = false });
            }
            else
            {
                // Like
                var like = new PostLike
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.PostLikes.AddAsync(like);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Liked", liked = true });
            }
        }

        // GET: api/community/post/{postId}/comments
        [HttpGet("post/{postId}/comments")]
        public async Task<IActionResult> GetComments(Guid postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound(new { message = "Post not found" });

            var comments = await _context.PostComments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentResponseDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserFullName = c.User.FullName,
                    UserProfileImage = c.User.ProfileImageUrl,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        // POST: api/community/post/{postId}/comment
        [HttpPost("post/{postId}/comment")]
        public async Task<IActionResult> AddComment(Guid postId, CreateCommentDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound(new { message = "Post not found" });

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Comment content is required" });

            var comment = new PostComment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PostComments.AddAsync(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment added", commentId = comment.Id });
        }

        // DELETE: api/community/comment/{commentId}
        [HttpDelete("comment/{commentId}")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var comment = await _context.PostComments.FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
                return NotFound(new { message = "Comment not found" });

            if (comment.UserId != userId)
                return Forbid();

            _context.PostComments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comment deleted successfully" });
        }

        // GET: api/community/my-posts
        [HttpGet("my-posts")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostResponseDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserFullName = p.User.FullName,
                    UserProfileImage = p.User.ProfileImageUrl,
                    Content = p.Content,
                    MediaUrl = p.MediaUrl,
                    MediaType = p.MediaType,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    IsLikedByMe = p.Likes.Any(l => l.UserId == userId),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(posts);
        }
    }
}