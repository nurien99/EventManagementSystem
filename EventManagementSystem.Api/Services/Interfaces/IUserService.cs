using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserDto>> RegisterAsync(UserRegisterDto registerDto);
        Task<ApiResponse<UserLoginResponseDto>> LoginAsync(UserLoginDto loginDto);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(int userId);
        Task<ApiResponse<UserDto>> UpdateUserAsync(int userId, UserDto userDto);
        Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<ApiResponse<bool>> SendEmailVerificationAsync(int userId);
        Task<ApiResponse<bool>> VerifyEmailAsync(string email, string token);
    }
}
