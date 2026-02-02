using ECommerce.API.Data;
using ECommerce.API.Models;

namespace ECommerce.API.Services;

public interface ICartService
{
    Task<ShoppingCart> GetCartAsync(Guid userId);
    Task AddToCartAsync(Guid userId, Guid productId, int quantity);
    Task UpdateCartItemAsync(Guid userId, Guid productId, int quantity);
    Task RemoveFromCartAsync(Guid userId, Guid productId);
    Task ClearCartAsync(Guid userId);
}

public class CartService : ICartService
{
    private readonly ICacheService _cache;
    private readonly ApplicationDbContext _context;

    public CartService(ICacheService cache, ApplicationDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    public async Task<ShoppingCart> GetCartAsync(Guid userId)
    {
        var cart = await _cache.GetAsync<ShoppingCart>($"cart:{userId}");
        return cart ?? new ShoppingCart { UserId = userId };
    }

    public async Task AddToCartAsync(Guid userId, Guid productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
            throw new InvalidOperationException("Product not found");

        if (product.StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");

        var cart = await GetCartAsync(userId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            });
        }

        cart.LastUpdated = DateTime.UtcNow;
        await _cache.SetAsync($"cart:{userId}", cart, TimeSpan.FromDays(7));
    }

    public async Task UpdateCartItemAsync(Guid userId, Guid productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

        var cart = await GetCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item == null) throw new InvalidOperationException("Item not in cart");

        var product = await _context.Products.FindAsync(productId);
        if (product!.StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");

        item.Quantity = quantity;
        cart.LastUpdated = DateTime.UtcNow;
        await _cache.SetAsync($"cart:{userId}", cart, TimeSpan.FromDays(7));
    }

    public async Task RemoveFromCartAsync(Guid userId, Guid productId)
    {
        var cart = await GetCartAsync(userId);
        cart.Items.RemoveAll(i => i.ProductId == productId);
        cart.LastUpdated = DateTime.UtcNow;
        await _cache.SetAsync($"cart:{userId}", cart, TimeSpan.FromDays(7));
    }

    public async Task ClearCartAsync(Guid userId)
    {
        await _cache.RemoveAsync($"cart:{userId}");
    }
}