namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Receives desktop feature lifecycle transitions without depending on a view.
/// </summary>
public interface IShellFeatureActivation
{
    /// <summary>Runs when the feature becomes the shell's current content.</summary>
    void Activate();

    /// <summary>Runs before another feature replaces the current content.</summary>
    void Deactivate();
}
