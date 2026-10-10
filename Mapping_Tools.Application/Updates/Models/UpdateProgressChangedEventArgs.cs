namespace Mapping_Tools.Application.Updates.Models;

/// <summary>
///     Reports a normalized package-preparation progress value.
/// </summary>
public sealed class UpdateProgressChangedEventArgs : EventArgs
{
    /// <summary>Creates a progress notification.</summary>
    /// <param name="progress">A value in the inclusive range zero through one.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite or is outside zero through one.</exception>
    public UpdateProgressChangedEventArgs(double progress)
    {
        if (!double.IsFinite(progress) || progress is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(progress));

        Progress = progress;
    }

    /// <summary>Gets the normalized package-preparation progress.</summary>
    public double Progress { get; }
}
