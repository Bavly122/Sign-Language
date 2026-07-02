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
    public class FriendsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FriendsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFriends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friends = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => f.Status == "Accepted" &&
                            (f.RequesterId == userId || f.AddresseeId == userId))
                .Select(f => new FriendDto
                {
                    Id = f.RequesterId == userId ? f.AddresseeId : f.RequesterId,
                    FullName = f.RequesterId == userId ? f.Addressee.FullName : f.Requester.FullName,
                    ProfileImageUrl = f.RequesterId == userId ? f.Addressee.ProfileImageUrl : f.Requester.ProfileImageUrl,
                    IsDeaf = f.RequesterId == userId ? f.Addressee.IsDeaf : f.Requester.IsDeaf,
                    IsMute = f.RequesterId == userId ? f.Addressee.IsMute : f.Requester.IsMute,
                    FriendshipStatus = "Accepted"
                })
                .ToListAsync();

            return Ok(friends);
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var currentUser = await _context.Users.FindAsync(userId);

            var existingFriendIds = await _context.Friendships
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            existingFriendIds.Add(userId);

            var suggestions = await _context.Users
                .Where(u => !existingFriendIds.Contains(u.Id))
                .Take(10)
                .Select(u => new FriendSuggestionDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    ProfileImageUrl = u.ProfileImageUrl,
                    IsDeaf = u.IsDeaf,
                    IsMute = u.IsMute
                })
                .ToListAsync();

            return Ok(suggestions);
        }

        [HttpPost("request/{addresseeId}")]
        public async Task<IActionResult> SendRequest(string addresseeId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == addresseeId)
                return BadRequest("Cannot send request to yourself");

            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == userId && f.AddresseeId == addresseeId) ||
                    (f.RequesterId == addresseeId && f.AddresseeId == userId));

            if (existing != null)
                return BadRequest("Friend request already exists");

            var friendship = new Friendship
            {
                Id = Guid.NewGuid(),
                RequesterId = userId,
                AddresseeId = addresseeId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Friendships.AddAsync(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request sent" });
        }

        [HttpPut("accept/{requesterId}")]
        public async Task<IActionResult> AcceptRequest(string requesterId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId &&
                                          f.AddresseeId == userId &&
                                          f.Status == "Pending");

            if (friendship == null)
                return NotFound("Friend request not found");

            friendship.Status = "Accepted";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request accepted" });
        }

        [HttpPut("reject/{requesterId}")]
        public async Task<IActionResult> RejectRequest(string requesterId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId &&
                                          f.AddresseeId == userId &&
                                          f.Status == "Pending");

            if (friendship == null)
                return NotFound("Friend request not found");

            friendship.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend request rejected" });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == "Pending")
                .Select(f => new FriendDto
                {
                    Id = f.RequesterId,
                    FullName = f.Requester.FullName,
                    ProfileImageUrl = f.Requester.ProfileImageUrl,
                    IsDeaf = f.Requester.IsDeaf,
                    IsMute = f.Requester.IsMute,
                    FriendshipStatus = "Pending"
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    f.Status == "Accepted" &&
                    ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                     (f.RequesterId == friendId && f.AddresseeId == userId)));

            if (friendship == null)
                return NotFound("Friendship not found");

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend removed" });
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchFriends([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query is required");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friends = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => f.Status == "Accepted" &&
                            (f.RequesterId == userId || f.AddresseeId == userId) &&
                            (f.RequesterId == userId
                                ? f.Addressee.FullName.Contains(q)
                                : f.Requester.FullName.Contains(q)))
                .Select(f => new FriendDto
                {
                    Id = f.RequesterId == userId ? f.AddresseeId : f.RequesterId,
                    FullName = f.RequesterId == userId ? f.Addressee.FullName : f.Requester.FullName,
                    ProfileImageUrl = f.RequesterId == userId ? f.Addressee.ProfileImageUrl : f.Requester.ProfileImageUrl,
                    IsDeaf = f.RequesterId == userId ? f.Addressee.IsDeaf : f.Requester.IsDeaf,
                    IsMute = f.RequesterId == userId ? f.Addressee.IsMute : f.Requester.IsMute,
                    FriendshipStatus = "Accepted"
                })
                .ToListAsync();

            return Ok(friends);
        }
    }
}