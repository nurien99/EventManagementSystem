// Replace your existing UsersController.cs with this:

using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="registerDto">User registration information</param>
        /// <returns>Newly created user information</returns>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] UserRegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<UserDto>.ErrorResult("Validation failed.", errors));
            }

            var result = await _userService.RegisterAsync(registerDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetUserById), new { id = result.Data.UserID }, result);
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        /// <param name="loginDto">User login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<UserLoginResponseDto>>> Login([FromBody] UserLoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<UserLoginResponseDto>.ErrorResult("Validation failed.", errors));
            }

            var result = await _userService.LoginAsync(loginDto);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int id)
        {
            // Check if user is requesting their own data or is an admin
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid("You can only access your own user information.");
            }

            var result = await _userService.GetUserByIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _userService.GetUserByIdAsync(currentUserId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="userDto">Updated user information</param>
        /// <returns>Updated user information</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UserDto userDto)
        {
            // Check if user is updating their own data or is an admin
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();

            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid("You can only update your own profile.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<UserDto>.ErrorResult("Validation failed.", errors));
            }

            var result = await _userService.UpdateUserAsync(id, userDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        /// <param name="forgotPasswordDto">Email address for password reset</param>
        /// <returns>Success message</returns>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed.", errors));
            }

            var result = await _userService.ForgotPasswordAsync(forgotPasswordDto);
            return Ok(result);
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        /// <param name="resetPasswordDto">Password reset information</param>
        /// <returns>Success message</returns>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed.", errors));
            }

            var result = await _userService.ResetPasswordAsync(resetPasswordDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Verify user's email address
        /// </summary>
        /// <param name="token">Email verification token</param>
        /// <param name="email">User's email address</param>
        /// <returns>Verification result</returns>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail([FromQuery] string token, [FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid verification parameters."));
            }

            var result = await _userService.VerifyEmailAsync(email, token);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Resend email verification
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>Result of resend operation</returns>
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ResendEmailVerification([FromBody] ResendVerificationDto resendDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed.", errors));
            }

            // Find user by email
            var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == resendDto.Email);

            if (user == null)
            {
                // Don't reveal if email exists for security
                return Ok(ApiResponse<bool>.SuccessResult(true, "If an account with that email exists, a verification email has been sent."));
            }

            if (user.IsEmailVerified)
            {
                return Ok(ApiResponse<bool>.SuccessResult(true, "Email is already verified."));
            }

            // Generate new verification token
            user.EmailVerificationToken = GenerateVerificationToken();
            await context.SaveChangesAsync();

            // Send verification email
            var result = await _userService.SendEmailVerificationAsync(user.UserID);

            return Ok(ApiResponse<bool>.SuccessResult(true, "If an account with that email exists, a verification email has been sent."));
        }

        // Add this helper method to your UsersController (in the private helper methods section)
        private string GenerateVerificationToken()
        {
            return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Attendee";
        }

        #endregion
    }
}