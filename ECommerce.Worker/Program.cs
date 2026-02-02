using ECommerce.Worker.Workers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<OrderProcessorWorker>();
builder.Services.AddSingleton<IEmailService, SendGridEmailService>();

var host = builder.Build();
host.Run();