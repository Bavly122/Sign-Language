using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EnTouch.Domain.Entities;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.IsDeaf,
            user.IsMute
        });
    }
}