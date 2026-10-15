namespace Mapping_Tools.Desktop.Plugin;

/// <summary>Registers one action or submenu at a shell menu location for the entire application session.</summary>
public sealed class ShellMenuRegistration
{
    /// <summary>Creates a registration to a stable shell menu location.</summary>
    /// <param name="location">The menu to append to; registrations retain service-registration order.</param>
    /// <param name="item">The action or submenu rendered by the host.</param>
    public ShellMenuRegistration(ShellMenuLocation location, ShellMenuItem item)
    {
        if (!Enum.IsDefined(location)) throw new ArgumentOutOfRangeException(nameof(location));

        Location = location;
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>Gets the shell menu where the action or submenu is registered.</summary>
    public ShellMenuLocation Location { get; }

    /// <summary>Gets the registered action or submenu.</summary>
    public ShellMenuItem Item { get; }
}
