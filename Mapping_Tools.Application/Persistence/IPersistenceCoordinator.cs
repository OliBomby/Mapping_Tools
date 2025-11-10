namespace Mapping_Tools.Application.Persistence;

public interface IPersistenceCoordinator {
    void Register(IPersistable persistable);

    Task LoadAllAsync(CancellationToken ct = default);
    Task SaveAllAsync(CancellationToken ct = default);
}