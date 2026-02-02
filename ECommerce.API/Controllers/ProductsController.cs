using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommerce.API.Data;
using ECommerce.API.Models;
using ECommerce.API.Services;
using ECommerce.API.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICacheService _cache;

    public ProductsController(IProductService productService, ICacheService cache)
    {
        _productService = productService;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        var cacheKey = $"products:{page}:{pageSize}:{category}:{search}:{minPrice}:{maxPrice}";
        var cached = await _cache.GetAsync<object>(cacheKey);
        if (cached != null) return Ok(cached);

        var (items, total) = await _productService.GetPagedAsync(page, pageSize, category, search, minPrice, maxPrice);

        var result = new
        {
            data = items,
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateProduct(CreateProductDto dto)
    {
        var created = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        await _productService.UpdateAsync(id, dto);
        // Invalidate cache
        await _cache.RemoveAsync($"products:*");
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}