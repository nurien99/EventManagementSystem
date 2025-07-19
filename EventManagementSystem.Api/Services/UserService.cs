using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Core.DTOs.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EventManagementSystem.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            INotificationService notificationService,
            ILogger<UserService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ApiResponse<UserDto>> RegisterAsync(UserRegisterDto registerDto)
        {
            try
            {
                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                {
                    return ApiResponse<UserDto>.ErrorResult("An account with this email already exists.");
                }

                // Hash password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                // Create new user
                var newUser = new User
                {
                    Name = registerDto.Name,
                    Email = registerDto.Email,
                    Password = hashedPassword,
                    Role = registerDto.Role,
                    PhoneNumber = registerDto.PhoneNumber ?? string.Empty,
                    Organization = registerDto.Organization ?? string.Empty,
                    IsEmailVerified = false,
                    EmailVerificationToken = GenerateVerificationToken(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null,
                    PasswordResetToken = string.Empty,
                    ResetTokenExpires = null
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Convert to DTO for response
                var userDto = MapToUserDto(newUser);

                // Send welcome email with email verification
                try
                {
                    await _notificationService.SendWelcomeEmailAsync(newUser.UserID);
                    _logger.LogInformation("Welcome email sent to new user {UserId} ({Email})", newUser.UserID, newUser.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send welcome email to {Email}", newUser.Email);
                    // Don't fail registration if email fails
                }

                return ApiResponse<UserDto>.SuccessResult(userDto, "User registered successfully. Please check your email to verify your account.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", registerDto.Email);
                return ApiResponse<UserDto>.ErrorResult("An error occurred during registration.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<UserLoginResponseDto>> LoginAsync(UserLoginDto loginDto)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                // Verify user exists and password is correct
                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                {
                    return ApiResponse<UserLoginResponseDto>.ErrorResult("Invalid email or password.");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return ApiResponse<UserLoginResponseDto>.ErrorResult("Your account has been deactivated. Please contact support.");
                }

                // Generate JWT token
                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                // Update last login (optional)
                // user.LastLoginAt = DateTime.UtcNow;
                // await _context.SaveChangesAsync();

                var response = new UserLoginResponseDto
                {
                    Token = token,
                    User = MapToUserDto(user),
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    RefreshToken = refreshToken
                };

                _logger.LogInformation("User {UserId} ({Email}) logged in successfully", user.UserID, user.Email);
                return ApiResponse<UserLoginResponseDto>.SuccessResult(response, "Login successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
                return ApiResponse<UserLoginResponseDto>.ErrorResult("An error occurred during login.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive);

                if (user == null)
                {
                    return ApiResponse<UserDto>.ErrorResult("User not found.");
                }

                var userDto = MapToUserDto(user);
                return ApiResponse<UserDto>.SuccessResult(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", userId);
                return ApiResponse<UserDto>.ErrorResult("An error occurred while retrieving user.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(int userId, UserDto userDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    return ApiResponse<UserDto>.ErrorResult("User not found.");
                }

                // Update user properties
                user.Name = userDto.Name;
                user.PhoneNumber = userDto.PhoneNumber ?? string.Empty;
                user.Organization = userDto.Organization ?? string.Empty;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedUserDto = MapToUserDto(user);
                _logger.LogInformation("User {UserId} updated successfully", userId);
                return ApiResponse<UserDto>.SuccessResult(updatedUserDto, "User updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return ApiResponse<UserDto>.ErrorResult("An error occurred while updating user.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email);

                if (user != null)
                {
                    var resetToken = GenerateResetToken();
                    user.PasswordResetToken = resetToken;
                    user.ResetTokenExpires = DateTime.UtcNow.AddMinutes(15);
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    // Generate reset link for Blazor client
                    var resetLink = $"https://localhost:7155/reset-password?token={resetToken}&email={user.Email}";

                    // Send password reset email using the new template system
                    try
                    {
                        var resetModel = new PasswordResetModel
                        {
                            UserName = user.Name,
                            ResetUrl = resetLink,
                            ExpiresAt = user.ResetTokenExpires.Value
                        };

                        await _emailService.SendTemplateEmailAsync(
                            user.Email,
                            "Reset Your Password - Event Management System",
                            "PasswordReset",
                            resetModel
                        );

                        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                        // Don't reveal if email failed to send for security
                    }
                }

                // Always return success for security (don't reveal if email exists)
                return ApiResponse<bool>.SuccessResult(true, "If an account with that email exists, a password reset link has been sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password for {Email}", forgotPasswordDto.Email);
                return ApiResponse<bool>.ErrorResult("An error occurred while processing your request.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == resetPasswordDto.Email &&
                                            u.PasswordResetToken == resetPasswordDto.Token &&
                                            u.ResetTokenExpires > DateTime.UtcNow);

                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("Invalid or expired password reset token.");
                }

                // Hash new password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
                user.Password = hashedPassword;

                // Clear reset token
                user.PasswordResetToken = string.Empty;
                user.ResetTokenExpires = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user {Email}", user.Email);
                return ApiResponse<bool>.SuccessResult(true, "Password has been reset successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", resetPasswordDto.Email);
                return ApiResponse<bool>.ErrorResult("An error occurred while resetting password.", new List<string> { ex.Message });
            }
        }

        /// <summary>
        /// Send email verification to user
        /// </summary>
        public async Task<ApiResponse<bool>> SendEmailVerificationAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found.");
                }

                if (user.IsEmailVerified)
                {
                    return ApiResponse<bool>.SuccessResult(true, "Email is already verified.");
                }

                // Send email verification using notification service
                var result = await _notificationService.SendEmailVerificationAsync(userId);

                if (result.Success)
                {
                    _logger.LogInformation("Email verification sent to user {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification for user {UserId}", userId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending email verification.", new List<string> { ex.Message });
            }
        }

        /// <summary>
        /// Verify user's email address
        /// </summary>
        public async Task<ApiResponse<bool>> VerifyEmailAsync(string email, string token)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.EmailVerificationToken == token);

                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("Invalid email verification token.");
                }

                if (user.IsEmailVerified)
                {
                    return ApiResponse<bool>.SuccessResult(true, "Email is already verified.");
                }

                // Mark email as verified
                user.IsEmailVerified = true;
                user.EmailVerificationToken = string.Empty; // Clear the token
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Email verified for user {UserId} ({Email})", user.UserID, user.Email);
                return ApiResponse<bool>.SuccessResult(true, "Email verified successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for {Email}", email);
                return ApiResponse<bool>.ErrorResult("An error occurred while verifying email.", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                Organization = user.Organization,
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("userId", user.UserID.ToString()),
                new Claim("isEmailVerified", user.IsEmailVerified.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateResetToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        private string GenerateVerificationToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        #endregion
    }
}