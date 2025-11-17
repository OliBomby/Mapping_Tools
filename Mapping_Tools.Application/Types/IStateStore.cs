namespace Mapping_Tools.Application.Types;

public interface IStateStore
{
    Task<T?> LoadAsync<T>(string key, CancellationToken ct = default);
    Task SaveAsync<T>(string key, T state, CancellationToken ct = default);
}