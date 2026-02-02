using Stripe;

public interface IPaymentService
{
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd");
    Task<bool> ConfirmPaymentAsync(string paymentIntentId);
}

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _config;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IConfiguration config, ILogger<StripePaymentService> logger)
    {
        _config = config;
        _logger = logger;
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Stripe uses cents
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe PaymentIntent: {PaymentIntentId}", paymentIntent.Id);
            return paymentIntent.ClientSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment intent");
            throw;
        }
    }

    public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            return paymentIntent.Status == "succeeded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }
}