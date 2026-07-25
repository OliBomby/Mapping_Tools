using Avalonia.Input.Platform;
using Mapping_Tools.ApplicationServices.Platform;

namespace Mapping_Tools.Desktop.Platform;

public sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly Func<IClipboard?> _clipboardAccessor;

    public AvaloniaClipboardService(Func<IClipboard?> clipboardAccessor)
    {
        _clipboardAccessor = clipboardAccessor
            ?? throw new ArgumentNullException(nameof(clipboardAccessor));
    }

    public async Task<string?> ReadTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? text = await GetClipboard().TryGetTextAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return text;
    }

    public async Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();
        await GetClipboard().SetTextAsync(text);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await GetClipboard().ClearAsync();
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await GetClipboard().FlushAsync();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private IClipboard GetClipboard()
    {
        return _clipboardAccessor()
            ?? throw new InvalidOperationException(
                "Clipboard access requires an initialized Avalonia top-level window.");
    }
}
