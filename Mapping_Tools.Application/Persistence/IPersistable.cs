namespace Mapping_Tools.Application.Persistence;

public interface IPersistable {
    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}