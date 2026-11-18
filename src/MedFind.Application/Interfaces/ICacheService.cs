namespace MedFind.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task IncrementAsync(string key, CancellationToken ct = default);
    Task<long> GetCounterAsync(string key, CancellationToken ct = default);
}
