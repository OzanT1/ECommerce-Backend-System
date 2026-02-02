using ECommerce.API.Services;

public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
}

public class RateLimitService : IRateLimitService
{
    private readonly ICacheService _cache;

    public RateLimitService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
    {
        var count = await _cache.GetAsync<int>(key);

        if (count >= limit)
            return false;

        await _cache.SetAsync(key, count + 1, window);
        return true;
    }
}