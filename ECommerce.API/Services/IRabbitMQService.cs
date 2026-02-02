using RabbitMQ.Client;
using System.Text;

namespace ECommerce.API.Services;

public interface IRabbitMQService
{
    Task PublishMessageAsync(string queueName, string message);
}

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMQService(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"],
            UserName = config["RabbitMQ:Username"],
            Password = config["RabbitMQ:Password"]
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declare queues
        _channel.QueueDeclareAsync("order-paid", durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
    }

    public async Task PublishMessageAsync(string queueName, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: properties, body: body);
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}