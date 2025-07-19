using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync();
        Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int categoryId);
        Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
        Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int categoryId, CreateCategoryDto updateCategoryDto);
        Task<ApiResponse<bool>> DeleteCategoryAsync(int categoryId);
        Task<ApiResponse<bool>> ToggleCategoryStatusAsync(int categoryId, bool isActive);
        Task<ApiResponse<List<CategoryDto>>> GetActiveCategoriesAsync();
        Task<ApiResponse<List<CategoryDto>>> SearchCategoriesAsync(string searchTerm);
    }
}