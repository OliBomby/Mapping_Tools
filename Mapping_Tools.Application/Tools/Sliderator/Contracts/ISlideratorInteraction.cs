namespace Mapping_Tools.Application.Tools.Sliderator.Contracts;

/// <summary>
///     Provides the small frontend-owned interaction needed by Shift navigation:
///     run the current slider through the editor and complete when placement ends.
/// </summary>
public interface ISlideratorInteraction
{
    /// <summary>Runs the current Sliderator placement and waits for its terminal result.</summary>
    /// <param name="cancellationToken">Cancels the placement wait.</param>
    /// <returns><see langword="true" /> when placement completed successfully; otherwise, <see langword="false" />.</returns>
    Task<bool> RunFastAsync(CancellationToken cancellationToken = default);
}
