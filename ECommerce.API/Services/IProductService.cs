using ECommerce.API.DTOs;

public interface IProductService
{
    Task<(IEnumerable<ProductResponseDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? category, string? search, decimal? minPrice, decimal? maxPrice);
    Task<ProductResponseDto?> GetByIdAsync(Guid id);
    Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
    Task UpdateAsync(Guid id, UpdateProductDto dto);
    Task DeleteAsync(Guid id);
}
