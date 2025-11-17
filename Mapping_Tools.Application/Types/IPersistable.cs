namespace Mapping_Tools.Application.Types;

public interface IPersistable {
    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}