namespace Mapping_Tools.Application.Persistence;

public interface IStateStore
{
    Task<T?> LoadAsync<T>(string key, CancellationToken ct = default);
    Task SaveAsync<T>(string key, T value, CancellationToken ct = default);
}