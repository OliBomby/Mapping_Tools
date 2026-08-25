using Mapping_Tools.Desktop.Shell.Models;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>Exposes additional feature-owned commands in the shell project menu.</summary>
public interface IShellExtraProjectMenuFeature
{
    /// <summary>Gets commands appended after the shell's standard project actions.</summary>
    IReadOnlyList<ShellProjectMenuItem> ExtraProjectMenuItems { get; }
}
