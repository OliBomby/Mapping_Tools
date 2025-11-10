namespace Mapping_Tools.Application;

public interface IAppLifecycle
{
    Task OnStartAsync(CancellationToken ct = default);
    Task OnShutdownAsync(CancellationToken ct = default);
}