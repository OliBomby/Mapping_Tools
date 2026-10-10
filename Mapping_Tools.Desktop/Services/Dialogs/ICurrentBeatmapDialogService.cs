namespace Mapping_Tools.Desktop.Services.Dialogs;

/// <summary>Fetches the current osu! beatmap and presents shared failure feedback.</summary>
public interface ICurrentBeatmapDialogService
{
    /// <summary>
    ///     Fetches the current beatmap, showing an error dialog for lookup failures
    ///     and a warning snackbar when the reported file is missing.
    /// </summary>
    /// <param name="cancellationToken">Cancels lookup or feedback publication.</param>
    /// <returns>The existing current beatmap path, or <see langword="null" />.</returns>
    Task<string?> FetchAsync(CancellationToken cancellationToken = default);
}
