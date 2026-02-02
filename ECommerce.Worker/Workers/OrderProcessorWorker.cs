using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ECommerce.Worker.Workers;

public class OrderProcessorWorker : BackgroundService
{
    private readonly ILogger<OrderProcessorWorker> _logger;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderProcessorWorker(
        ILogger<OrderProcessorWorker> logger,
        IConfiguration config,
        IEmailService emailService)
    {
        _logger = logger;
        _config = config;
        _emailService = emailService;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order Processor Worker starting...");

        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"],
            UserName = _config["RabbitMQ:Username"],
            Password = _config["RabbitMQ:Password"]
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declare queues
        await _channel.QueueDeclareAsync("order-paid", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Process order-paid events (order confirmation emails sent AFTER payment is confirmed)
        var orderPaidConsumer = new AsyncEventingBasicConsumer(_channel);
        orderPaidConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var orderEvent = JsonSerializer.Deserialize<OrderPaidEvent>(message);

                _logger.LogInformation("Processing order paid: {OrderId}", orderEvent?.OrderId);

                // Send order confirmation email that payment is confirmed
                await _emailService.SendOrderConfirmationAsync(
                    orderEvent.UserEmail,
                    orderEvent!.OrderId.ToString(),
                    orderEvent.TotalAmount);

                _logger.LogInformation("Order confirmation email sent for order {OrderId}", orderEvent.OrderId);

                // Additional processing: generate invoice, update inventory analytics, etc.

                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order-paid event");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, true); // Requeue
            }
        };

        await _channel.BasicConsumeAsync("order-paid", autoAck: false, consumer: orderPaidConsumer);

        _logger.LogInformation("Worker started and listening for order-paid events");

        // Keep the worker running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order Processor Worker stopping...");
        await base.StopAsync(cancellationToken);

        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
