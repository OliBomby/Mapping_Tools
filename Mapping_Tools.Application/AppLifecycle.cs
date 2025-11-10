using Mapping_Tools.Application.Persistence;

namespace Mapping_Tools.Application;

public sealed class AppLifecycle(IPersistenceCoordinator persistence) : IAppLifecycle {
    public async Task OnStartAsync(CancellationToken ct = default) {
        // load state, prime caches, run migrations, etc.
        // Register any application services that need to be persisted here.
        await persistence.LoadAllAsync(ct);
    }

    public Task OnShutdownAsync(CancellationToken ct = default) {
        return persistence.SaveAllAsync(ct);
    }
}