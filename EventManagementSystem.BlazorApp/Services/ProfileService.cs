using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.BlazorApp.Services
{
    public class ProfileService
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(ApiService apiService, ILogger<ProfileService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        /// <returns>User profile information</returns>
        public async Task<ApiResponse<UserDto>?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _apiService.GetAsync<UserDto>("api/users/me");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user profile");
                return new ApiResponse<UserDto> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        /// <param name="updateProfileDto">Updated profile information</param>
        /// <returns>Updated user information</returns>
        public async Task<ApiResponse<UserDto>?> UpdateProfileAsync(UpdateProfileDto updateProfileDto)
        {
            try
            {
                var response = await _apiService.PutAsync<UserDto>("api/users/profile", updateProfileDto);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return new ApiResponse<UserDto> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="changePasswordDto">Password change information</param>
        /// <returns>Success status</returns>
        public async Task<ApiResponse<bool>?> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        {
            try
            {
                var response = await _apiService.PostAsync<bool>("api/users/change-password", changePasswordDto);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return new ApiResponse<bool> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Resend email verification
        /// </summary>
        /// <param name="email">Email address to send verification to</param>
        /// <returns>Success status</returns>
        public async Task<ApiResponse<bool>?> ResendEmailVerificationAsync(string email)
        {
            try
            {
                var resendDto = new ResendVerificationDto { Email = email };
                var response = await _apiService.PostAsync<bool>("api/users/resend-verification", resendDto);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email verification");
                return new ApiResponse<bool> { Success = false, Message = ex.Message };
            }
        }
    }
}