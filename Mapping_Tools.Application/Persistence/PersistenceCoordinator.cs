using Mapping_Tools.Application.Types;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Application.Persistence;

public sealed class PersistenceCoordinator(ILogger<PersistenceCoordinator> logger) : IPersistenceCoordinator {
    private readonly List<IPersistable> persistables = [];

    public void Register(IPersistable persistable) {
        persistables.Add(persistable);
    }

    public async Task LoadAllAsync(CancellationToken ct = default) {
        foreach (var persistable in persistables)
        {
            logger.LogInformation("Loading state for {Type}", persistable.GetType().Name);
            await persistable.LoadAsync(ct);
            logger.LogInformation("Loading state for {Type}", persistable.GetType().Name);
        }
    }

    public async Task SaveAllAsync(CancellationToken ct = default)
    {
        foreach (var persistable in persistables)
        {
            logger.LogInformation("Saving state for {Type}", persistable.GetType().Name);
            await persistable.SaveAsync(ct);
            logger.LogInformation("Saved state for {Type}", persistable.GetType().Name);
        }
    }
}