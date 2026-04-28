using EnTouch.Domain.Entities;
using EnTouch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnTouch.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
        private const long MaxImageSize = 10 * 1024 * 1024;  // 10 MB
        private const long MaxVideoSize = 100 * 1024 * 1024; // 100 MB

        public MediaController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // POST: api/media/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadMedia(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var extension = Path.GetExtension(file.FileName).ToLower();

            bool isImage = AllowedImageExtensions.Contains(extension);
            bool isVideo = AllowedVideoExtensions.Contains(extension);

            if (!isImage && !isVideo)
                return BadRequest(new { message = "File type not allowed. Only images and videos are accepted" });

            if (isImage && file.Length > MaxImageSize)
                return BadRequest(new { message = "Image size must be less than 10MB" });

            if (isVideo && file.Length > MaxVideoSize)
                return BadRequest(new { message = "Video size must be less than 100MB" });

            // Determine subfolder: images/ or videos/
            var mediaType = isImage ? "images" : "videos";
            var uploadsFolder = Path.Combine(_env.WebRootPath, "media", mediaType);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative URL
            var relativeUrl = $"/media/{mediaType}/{fileName}";

            return Ok(new
            {
                url = relativeUrl,
                mediaType = mediaType,
                fileName = fileName
            });
        }
    }
}