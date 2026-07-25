namespace Mapping_Tools.ApplicationServices.Platform;

public interface IClipboardService
{
    Task<string?> ReadTextAsync(CancellationToken cancellationToken = default);

    Task WriteTextAsync(string text, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task FlushAsync(CancellationToken cancellationToken = default);
}
