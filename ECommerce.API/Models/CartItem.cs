namespace ECommerce.API.Models;

// Models/CartItem.cs (Redis-stored, not in DB)
public class CartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class ShoppingCart
{
    public Guid UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public decimal GetTotal() => Items.Sum(i => i.Price * i.Quantity);
    public int GetItemCount() => Items.Sum(i => i.Quantity);
}