namespace Mapping_Tools.Application.Types;

public interface IPersistenceCoordinator {
    void Register(IPersistable persistable);

    Task LoadAllAsync(CancellationToken ct = default);
    Task SaveAllAsync(CancellationToken ct = default);
}