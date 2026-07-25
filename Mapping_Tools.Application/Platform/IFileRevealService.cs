namespace Mapping_Tools.ApplicationServices.Platform;

/// <summary>
/// Locates a file or directory in the platform's graphical file manager.
/// </summary>
public interface IFileRevealService
{
    /// <summary>
    /// Opens the containing directory and, where supported, selects the requested item.
    /// </summary>
    /// <param name="path">An existing local file or directory.</param>
    /// <param name="cancellationToken">Cancels the handoff before it starts.</param>
    /// <returns><see langword="true"/> when the file manager accepted the request.</returns>
    /// <exception cref="FileNotFoundException">The supplied path does not exist.</exception>
    Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default);
}
