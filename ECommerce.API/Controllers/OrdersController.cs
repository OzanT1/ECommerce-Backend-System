using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public OrdersController(IOrderService orderService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var order = await _orderService.CreateOrderAsync(userId, dto);

        // Create Stripe payment intent
        var clientSecret = await _paymentService.CreatePaymentIntentAsync(order.TotalAmount);

        // Store the payment intent ID on the order for tracking
        await _orderService.StorePaymentIntentAsync(order.Id, clientSecret);

        return Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var orders = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var order = await _orderService.GetOrderByIdAsync(id, userId);

        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost("{id}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] ConfirmPaymentDto dto)
    {
        var isConfirmed = await _paymentService.ConfirmPaymentAsync(dto.PaymentIntentId);

        if (isConfirmed)
        {
            await _orderService.UpdateOrderStatusAsync(id, OrderStatus.PaymentReceived);
            return Ok(new { message = "Payment confirmed" });
        }

        return BadRequest(new { message = "Payment confirmation failed" });
    }
}