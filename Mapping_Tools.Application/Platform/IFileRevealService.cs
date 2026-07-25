namespace Mapping_Tools.ApplicationServices.Platform;

public interface IFileRevealService
{
    Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default);
}
