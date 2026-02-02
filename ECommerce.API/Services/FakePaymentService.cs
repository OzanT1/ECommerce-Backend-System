using Microsoft.Extensions.Logging;

public class FakePaymentService : IPaymentService
{
    private readonly IConfiguration _config;
    private readonly ILogger<FakePaymentService> _logger;

    public FakePaymentService(IConfiguration config, ILogger<FakePaymentService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
        // Simulate a Stripe-like payment intent creation. Return a fake client secret.
        var fakeId = $"pi_fake_{Guid.NewGuid():N}";
        var clientSecret = $"fake_client_secret_{Guid.NewGuid():N}";
        _logger.LogInformation("[FakePayment] Created fake payment intent {PaymentIntentId} amount={Amount} {Currency}", fakeId, amount, currency);

        // Optionally allow overriding behavior with env var FAKE_PAYMENT_SUCCEEDS=false
        return Task.FromResult(clientSecret);
    }

    public Task<bool> ConfirmPaymentAsync(string paymentIntentId)
    {
        var env = _config["FAKE_PAYMENT_SUCCEEDS"];
        var succeed = string.IsNullOrEmpty(env) || !env.Equals("false", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("[FakePayment] ConfirmPayment {PaymentIntentId} => {Result}", paymentIntentId, succeed);
        return Task.FromResult(succeed);
    }
}
