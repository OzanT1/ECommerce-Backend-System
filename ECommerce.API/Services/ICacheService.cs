using StackExchange.Redis;
using System.Text.Json;

namespace ECommerce.API.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _redis;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer connection)
    {
        _redis = connection.GetDatabase();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _redis.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _redis.StringSetAsync(key, json, expiry ?? TimeSpan.FromHours(1));
    }

    public async Task RemoveAsync(string key) => await _redis.KeyDeleteAsync(key);

    public async Task<bool> ExistsAsync(string key) => await _redis.KeyExistsAsync(key);
}