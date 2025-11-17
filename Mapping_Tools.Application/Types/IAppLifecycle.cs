namespace Mapping_Tools.Application.Types;

public interface IAppLifecycle
{
    Task OnStartAsync(CancellationToken ct = default);
    Task OnShutdownAsync(CancellationToken ct = default);
    
    CancellationToken UICleanup { get; }
}