using ECommerce.API.Models;
using ECommerce.API.Services;
using ECommerce.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        await _cartService.AddToCartAsync(userId, dto.ProductId, dto.Quantity);
        return Ok(new { message = "Item added to cart" });
    }

    [HttpPut("items/{productId}")]
    public async Task<IActionResult> UpdateCartItem(Guid productId, [FromBody] UpdateCartDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        await _cartService.UpdateCartItemAsync(userId, productId, dto.Quantity);
        return Ok(new { message = "Cart updated" });
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveFromCart(Guid productId)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        await _cartService.RemoveFromCartAsync(userId, productId);
        return Ok(new { message = "Item removed" });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        await _cartService.ClearCartAsync(userId);
        return Ok(new { message = "Cart cleared" });
    }
}