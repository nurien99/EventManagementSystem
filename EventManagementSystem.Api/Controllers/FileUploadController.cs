using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IWebHostEnvironment environment, ILogger<FileUploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Upload event image file
        /// </summary>
        [HttpPost("event-image")]
        public async Task<ActionResult<ApiResponse<string>>> UploadEventImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<string>.ErrorResult("No file provided"));
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(ApiResponse<string>.ErrorResult("Only image files (JPEG, PNG, GIF, WebP) are allowed"));
                }

                // Validate file size (max 5MB)
                const int maxFileSizeBytes = 5 * 1024 * 1024;
                if (file.Length > maxFileSizeBytes)
                {
                    return BadRequest(ApiResponse<string>.ErrorResult("File size cannot exceed 5MB"));
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "events");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Generate URL for the uploaded file
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var fileUrl = $"{baseUrl}/uploads/events/{uniqueFileName}";

                _logger.LogInformation("Event image uploaded successfully: {FileName} -> {FileUrl}", file.FileName, fileUrl);

                return Ok(ApiResponse<string>.SuccessResult(fileUrl, "Image uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading event image");
                return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred while uploading the image"));
            }
        }

        /// <summary>
        /// Delete uploaded image
        /// </summary>
        [HttpDelete("event-image")]
        public ActionResult<ApiResponse<bool>> DeleteEventImage([FromQuery] string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResult("Image URL is required"));
                }

                // Extract filename from URL
                var uri = new Uri(imageUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                
                if (string.IsNullOrEmpty(fileName))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResult("Invalid image URL"));
                }

                // Build file path
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "events");
                var filePath = Path.Combine(uploadsPath, fileName);

                // Delete file if exists
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Event image deleted successfully: {FilePath}", filePath);
                    return Ok(ApiResponse<bool>.SuccessResult(true, "Image deleted successfully"));
                }
                else
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Image file not found"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event image");
                return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while deleting the image"));
            }
        }

        /// <summary>
        /// Get upload information and limits
        /// </summary>
        [HttpGet("info")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<object>> GetUploadInfo()
        {
            var info = new
            {
                MaxFileSize = "5MB",
                AllowedTypes = new[] { "JPEG", "PNG", "GIF", "WebP" },
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                MaxFileSizeBytes = 5 * 1024 * 1024
            };

            return Ok(ApiResponse<object>.SuccessResult(info, "Upload information retrieved successfully"));
        }
    }
}