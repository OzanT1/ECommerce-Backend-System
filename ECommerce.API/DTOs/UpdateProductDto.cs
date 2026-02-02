using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.DTOs;

public class UpdateProductDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public Guid? CategoryId { get; set; }

    public int Stock { get; set; }

    [Url]
    public string? ImageUrl { get; set; }
}
