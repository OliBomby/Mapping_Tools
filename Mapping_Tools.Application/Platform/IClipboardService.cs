namespace Mapping_Tools.ApplicationServices.Platform;

/// <summary>
/// Provides asynchronous text-only access to the platform clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Reads text currently stored on the clipboard.
    /// </summary>
    /// <param name="cancellationToken">Cancels before or after the native operation.</param>
    /// <returns>The clipboard text, or <see langword="null"/> when it contains no text.</returns>
    Task<string?> ReadTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the clipboard contents with text.
    /// </summary>
    /// <param name="text">The text to place on the clipboard.</param>
    /// <param name="cancellationToken">Cancels before or after the native operation.</param>
    Task WriteTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all clipboard content.
    /// </summary>
    /// <param name="cancellationToken">Cancels before or after the native operation.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests that clipboard content remain available after this process exits.
    /// </summary>
    /// <param name="cancellationToken">Cancels before or after the native operation.</param>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
