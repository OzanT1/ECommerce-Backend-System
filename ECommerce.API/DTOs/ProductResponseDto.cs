namespace ECommerce.API.DTOs;

public class ProductResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
