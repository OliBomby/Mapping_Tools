namespace Mapping_Tools.Desktop.Plugin;

/// <summary>Places one plugin action or submenu in shell chrome for the entire application session.</summary>
public sealed class ShellMenuContribution
{
    /// <summary>Creates a contribution to a stable shell menu location.</summary>
    /// <param name="location">The menu to append to; contributions retain service-registration order.</param>
    /// <param name="item">The action or submenu rendered by the host.</param>
    public ShellMenuContribution(ShellMenuLocation location, ShellMenuItem item)
    {
        if (!Enum.IsDefined(location)) throw new ArgumentOutOfRangeException(nameof(location));

        Location = location;
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>Gets the shell menu receiving this contribution.</summary>
    public ShellMenuLocation Location { get; }

    /// <summary>Gets the contributed action or submenu.</summary>
    public ShellMenuItem Item { get; }
}
