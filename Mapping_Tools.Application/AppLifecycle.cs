using Mapping_Tools.Application.Types;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Application;

public sealed class AppLifecycle(IPersistenceCoordinator persistence, ILogger<AppLifecycle> logger) : IAppLifecycle {
    private CancellationTokenSource _uiCleanupCts = null!;
    
    public async Task OnStartAsync(CancellationToken ct = default) {
        logger.LogInformation("Starting app lifecycle");
        
        // load state, prime caches, run migrations, etc.
        _uiCleanupCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        UICleanup = _uiCleanupCts.Token;
        
        // Register any application services that need to be persisted here.
        await persistence.LoadAllAsync(ct);
    }

    public async Task OnShutdownAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Stopping app lifecycle");
        
        await _uiCleanupCts.CancelAsync();
        await persistence.SaveAllAsync(ct);
    }

    public CancellationToken UICleanup { get; private set; }
}