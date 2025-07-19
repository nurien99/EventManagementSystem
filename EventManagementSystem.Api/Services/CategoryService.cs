using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.EventCategories
                    .Include(c => c.Events)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = categories.Select(MapToCategoryDto).ToList();
                return ApiResponse<List<CategoryDto>>.SuccessResult(categoryDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CategoryDto>>.ErrorResult("An error occurred while retrieving categories.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<CategoryDto>>> GetActiveCategoriesAsync()
        {
            try
            {
                var categories = await _context.EventCategories
                    .Include(c => c.Events)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = categories.Select(MapToCategoryDto).ToList();
                return ApiResponse<List<CategoryDto>>.SuccessResult(categoryDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CategoryDto>>.ErrorResult("An error occurred while retrieving active categories.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int categoryId)
        {
            try
            {
                var category = await _context.EventCategories
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

                if (category == null)
                {
                    return ApiResponse<CategoryDto>.ErrorResult("Category not found.");
                }

                var categoryDto = MapToCategoryDto(category);
                return ApiResponse<CategoryDto>.SuccessResult(categoryDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<CategoryDto>.ErrorResult("An error occurred while retrieving the category.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            try
            {
                // Check if category with same name already exists
                var existingCategory = await _context.EventCategories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == createCategoryDto.CategoryName.ToLower() && c.IsActive);

                if (existingCategory != null)
                {
                    return ApiResponse<CategoryDto>.ErrorResult("A category with this name already exists.");
                }

                var category = new EventCategory
                {
                    CategoryName = createCategoryDto.CategoryName,
                    Description = createCategoryDto.Description ?? string.Empty,
                    IconClass = createCategoryDto.IconClass ?? string.Empty,
                    ColorCode = createCategoryDto.ColorCode ?? "#007bff",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EventCategories.Add(category);
                await _context.SaveChangesAsync();

                var categoryDto = MapToCategoryDto(category);
                return ApiResponse<CategoryDto>.SuccessResult(categoryDto, "Category created successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<CategoryDto>.ErrorResult("An error occurred while creating the category.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int categoryId, CreateCategoryDto updateCategoryDto)
        {
            try
            {
                var category = await _context.EventCategories.FindAsync(categoryId);
                if (category == null)
                {
                    return ApiResponse<CategoryDto>.ErrorResult("Category not found.");
                }

                // Check if another category with same name exists (excluding current category)
                var existingCategory = await _context.EventCategories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == updateCategoryDto.CategoryName.ToLower() &&
                                            c.CategoryID != categoryId &&
                                            c.IsActive);

                if (existingCategory != null)
                {
                    return ApiResponse<CategoryDto>.ErrorResult("A category with this name already exists.");
                }

                // Update category properties
                category.CategoryName = updateCategoryDto.CategoryName;
                category.Description = updateCategoryDto.Description ?? string.Empty;
                category.IconClass = updateCategoryDto.IconClass ?? string.Empty;
                category.ColorCode = updateCategoryDto.ColorCode ?? "#007bff";

                await _context.SaveChangesAsync();

                var categoryDto = MapToCategoryDto(category);
                return ApiResponse<CategoryDto>.SuccessResult(categoryDto, "Category updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<CategoryDto>.ErrorResult("An error occurred while updating the category.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _context.EventCategories
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

                if (category == null)
                {
                    return ApiResponse<bool>.ErrorResult("Category not found.");
                }

                // Check if category has events
                if (category.Events.Any())
                {
                    return ApiResponse<bool>.ErrorResult("Cannot delete category with existing events. Consider deactivating it instead.");
                }

                _context.EventCategories.Remove(category);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Category deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while deleting the category.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ToggleCategoryStatusAsync(int categoryId, bool isActive)
        {
            try
            {
                var category = await _context.EventCategories.FindAsync(categoryId);
                if (category == null)
                {
                    return ApiResponse<bool>.ErrorResult("Category not found.");
                }

                category.IsActive = isActive;
                await _context.SaveChangesAsync();

                var statusText = isActive ? "activated" : "deactivated";
                return ApiResponse<bool>.SuccessResult(true, $"Category {statusText} successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while updating category status.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<CategoryDto>>> SearchCategoriesAsync(string searchTerm)
        {
            try
            {
                var query = _context.EventCategories
                    .Include(c => c.Events)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(c => c.CategoryName.Contains(searchTerm) ||
                                           c.Description.Contains(searchTerm));
                }

                var categories = await query
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = categories.Select(MapToCategoryDto).ToList();
                return ApiResponse<List<CategoryDto>>.SuccessResult(categoryDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CategoryDto>>.ErrorResult("An error occurred while searching categories.", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private CategoryDto MapToCategoryDto(EventCategory category)
        {
            return new CategoryDto
            {
                CategoryID = category.CategoryID,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IconClass = category.IconClass,
                ColorCode = category.ColorCode,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                EventCount = category.Events?.Count ?? 0,
                ActiveEventCount = category.Events?.Count(e => e.Status == EventStatus.Published) ?? 0
            };
        }

        #endregion
    }
}