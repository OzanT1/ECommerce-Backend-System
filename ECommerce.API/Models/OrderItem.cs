namespace ECommerce.API.Models;

using System.Text.Json.Serialization;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    [JsonIgnore]
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    [JsonIgnore]
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public decimal SubTotal { get; set; }
}