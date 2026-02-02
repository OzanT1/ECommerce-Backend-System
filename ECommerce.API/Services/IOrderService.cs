using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Text.Json;

namespace ECommerce.API.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Guid userId, CreateOrderDto orderDto);
    Task<Order?> GetOrderByIdAsync(Guid orderId, Guid userId);
    Task<List<Order>> GetUserOrdersAsync(Guid userId, int page, int pageSize);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
    Task StorePaymentIntentAsync(Guid orderId, string paymentIntentId);
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly IRabbitMQService _rabbitmq;
    private readonly ILogger<OrderService> _logger;

    private readonly decimal taxRate = 0.1m; // 10% tax
    private readonly decimal fixedShippingCost = 10.00m;


    public OrderService(
        ApplicationDbContext context,
        ICartService cartService,
        IRabbitMQService rabbitmq,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cartService = cartService;
        _rabbitmq = rabbitmq;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(Guid userId, CreateOrderDto orderDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cart = await _cartService.GetCartAsync(userId);
            if (!cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            // Validate stock (without deducting yet - only happens after payment)
            var orderItems = new List<OrderItem>();
            decimal subTotal = 0;

            foreach (var cartItem in cart.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId && p.IsActive);

                if (product == null)
                    throw new InvalidOperationException($"Product {cartItem.ProductId} not found");

                if (product.StockQuantity < cartItem.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}");


                var itemSubTotal = product.Price * cartItem.Quantity;
                subTotal += itemSubTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = cartItem.Quantity,
                    PriceAtPurchase = product.Price,
                    SubTotal = itemSubTotal
                });
            }

            // Calculate tax and shipping (simplified)
            var tax = subTotal * taxRate; // 10% tax
            var shippingCost = fixedShippingCost;  // Default fixed shipping cost, can be modified based on address
            var totalAmount = subTotal + tax + shippingCost;

            // Create order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                Status = OrderStatus.Pending,
                SubTotal = subTotal,
                Tax = tax,
                ShippingCost = shippingCost,
                TotalAmount = totalAmount,
                ShippingAddress = orderDto.ShippingAddress,
                ShippingCity = orderDto.ShippingCity,
                ShippingPostalCode = orderDto.ShippingPostalCode,
                ShippingCountry = orderDto.ShippingCountry,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Clear cart
            await _cartService.ClearCartAsync(userId);

            // Email confirmation will be sent after payment succeeds (order-paid event)

            _logger.LogInformation("Order {OrderNumber} created (awaiting payment) for user {UserId}", order.OrderNumber, userId);

            return order;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating order for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId, Guid userId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
    }

    public async Task<List<Order>> GetUserOrdersAsync(Guid userId, int page, int pageSize)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.OrderItems)
            .ToListAsync();
    }

    public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new InvalidOperationException("Order not found");

            // Deduct stock only when payment is confirmed
            if (status == OrderStatus.PaymentReceived && order.Status == OrderStatus.Pending)
            {
                foreach (var orderItem in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= orderItem.Quantity;
                        if (product.StockQuantity < 0)
                            throw new InvalidOperationException($"Cannot deduct stock for {product.Name}");
                    }
                }
            }

            // Restore stock if order is cancelled
            if (status == OrderStatus.Cancelled && (order.Status == OrderStatus.Pending || order.Status == OrderStatus.PaymentReceived))
            {
                foreach (var orderItem in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += orderItem.Quantity;
                    }
                }
            }

            order.Status = status;

            if (status == OrderStatus.PaymentReceived)
                order.PaidAt = DateTime.UtcNow;
            else if (status == OrderStatus.Shipped)
                order.ShippedAt = DateTime.UtcNow;
            else if (status == OrderStatus.Delivered)
                order.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish order-paid event only when payment is confirmed (for order confirmation email)
            if (status == OrderStatus.PaymentReceived)
            {
                var userEmail = await _context.Users
                    .Where(u => u.Id == order.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync() ?? string.Empty;

                var orderEvent = new { OrderId = order.Id, UserId = order.UserId, UserEmail = userEmail, TotalAmount = order.TotalAmount };
                await _rabbitmq.PublishMessageAsync("order-paid", JsonSerializer.Serialize(orderEvent));

                _logger.LogInformation("Order {OrderNumber} marked as PaymentReceived. Order-paid event published.", order.OrderNumber);
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
            throw;
        }
    }

    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    public async Task StorePaymentIntentAsync(Guid orderId, string paymentIntentId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) throw new InvalidOperationException("Order not found");

        order.StripePaymentIntentId = paymentIntentId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stored payment intent {PaymentIntentId} for order {OrderNumber}", paymentIntentId, order.OrderNumber);
    }
}
