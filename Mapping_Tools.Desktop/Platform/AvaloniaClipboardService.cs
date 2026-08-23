using Avalonia.Input.Platform;
using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Desktop.Platform;

/// <summary>
///     Adapts the clipboard owned by an initialized Avalonia top-level window to
///     the frontend-neutral text clipboard contract.
/// </summary>
public sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly Func<IClipboard?> clipboardAccessor;

    /// <summary>
    ///     Creates an adapter that resolves the clipboard lazily, after the window exists.
    /// </summary>
    /// <param name="clipboardAccessor">Returns the current top-level clipboard, if initialized.</param>
    public AvaloniaClipboardService(Func<IClipboard?> clipboardAccessor)
    {
        this.clipboardAccessor = clipboardAccessor
                                 ?? throw new ArgumentNullException(nameof(clipboardAccessor));
    }

    /// <summary>
    ///     <inheritdoc />
    public async Task<string?> ReadTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? text = await GetClipboard().TryGetTextAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return text;
    }

    /// <summary>
    ///     <inheritdoc />
    public async Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();
        await GetClipboard().SetTextAsync(text);
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    ///     <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await GetClipboard().ClearAsync();
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    ///     <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await GetClipboard().FlushAsync();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private IClipboard GetClipboard()
    {
        return clipboardAccessor()
               ?? throw new InvalidOperationException(
                   "Clipboard access requires an initialized Avalonia top-level window.");
    }
}
