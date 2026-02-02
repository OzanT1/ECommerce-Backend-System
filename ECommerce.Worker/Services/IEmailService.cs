using SendGrid;
using SendGrid.Helpers.Mail;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string toEmail, string orderNumber, decimal totalAmount);
    Task SendShippingNotificationAsync(string toEmail, string orderNumber, string trackingNumber);
}

public class SendGridEmailService : IEmailService
{
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly IConfiguration _config;

    public SendGridEmailService(ILogger<SendGridEmailService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendOrderConfirmationAsync(string toEmail, string orderNumber, decimal totalAmount)
    {
        _logger.LogInformation(
            "Sending order confirmation email to {Email} for order {OrderNumber} (Total: ${Total})",
            toEmail, orderNumber, totalAmount);

        // SendGrid integration
        var apiKey = _config["Email:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(_config["Email:FromEmail"], _config["Email:FromName"]);
        var to = new EmailAddress(toEmail);
        var subject = $"Order Confirmation - {orderNumber}";
        var htmlContent = GenerateOrderConfirmationHtml(orderNumber, totalAmount);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
        await client.SendEmailAsync(msg);
    }

    public async Task SendShippingNotificationAsync(string toEmail, string orderNumber, string trackingNumber)
    {
        _logger.LogInformation(
            "Sending shipping notification to {Email} for order {OrderNumber} (Tracking: {Tracking})",
            toEmail, orderNumber, trackingNumber);

        // SendGrid integration
        var apiKey = _config["Email:ApiKey"];
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(_config["Email:FromEmail"], _config["Email:FromName"]);
        var to = new EmailAddress(toEmail);
        var subject = $"Shipping Notification - {orderNumber}";
        var htmlContent = GenerateShippingNotificationHtml(orderNumber, trackingNumber);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
        await client.SendEmailAsync(msg);
    }

    private string GenerateOrderConfirmationHtml(string orderNumber, decimal totalAmount)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Thank You for Your Order!</h2>
                <p>Your order <strong>{orderNumber}</strong> has been confirmed.</p>
                <p>Total Amount: <strong>${totalAmount:F2}</strong></p>
                <p>We'll send you another email when your order ships.</p>
                <br/>
                <p>Best regards,<br/>E-Commerce Team</p>
            </body>
            </html>
        ";
    }

    private string GenerateShippingNotificationHtml(string orderNumber, string trackingNumber)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Your Order Has Shipped!</h2>
                <p>Your order <strong>{orderNumber}</strong> is on its way.</p>
                <p>Tracking Number: <strong>{trackingNumber}</strong></p>
                <p>You can track your package using the tracking number provided.</p>
                <br/>
                <p>Best regards,<br/>E-Commerce Team</p>
            </body>
            </html>
        ";
    }
}