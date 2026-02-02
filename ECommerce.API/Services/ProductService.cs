using Microsoft.EntityFrameworkCore;
using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<ProductResponseDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? category, string? search, decimal? minPrice, decimal? maxPrice)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category!.Slug == category);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var total = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = products.Select(MapToDto);
        return (items, total);
    }

    public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        if (product == null) return null;
        return MapToDto(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Price = dto.Price,
            CategoryId = dto.CategoryId ?? Guid.Empty,
            StockQuantity = dto.Stock,
            ImageUrl = dto.ImageUrl ?? string.Empty,
            Slug = GenerateSlug(dto.Name),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) throw new KeyNotFoundException("Product not found");

        product.Name = dto.Name;
        product.Description = dto.Description ?? string.Empty;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId ?? Guid.Empty;
        product.StockQuantity = dto.Stock;
        product.ImageUrl = dto.ImageUrl ?? string.Empty;
        product.Slug = GenerateSlug(dto.Name);
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) throw new KeyNotFoundException("Product not found");

        product.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static ProductResponseDto MapToDto(Product p)
    {
        return new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            Slug = p.Slug,
            Stock = p.StockQuantity,
            ImageUrl = p.ImageUrl,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }

    private static string GenerateSlug(string name)
    {
        return name.Trim().ToLower().Replace(" ", "-");
    }
}
