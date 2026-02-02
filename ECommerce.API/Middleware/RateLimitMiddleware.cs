public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        var endpoint = context.Request.Path.ToString();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rate_limit:{ipAddress}:{endpoint}";

        if (!await rateLimitService.IsAllowedAsync(key, limit: 100, window: TimeSpan.FromMinutes(1)))
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }
}