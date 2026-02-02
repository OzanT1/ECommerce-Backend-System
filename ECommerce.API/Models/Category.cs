namespace ECommerce.API.Models;

using System.Text.Json.Serialization;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}