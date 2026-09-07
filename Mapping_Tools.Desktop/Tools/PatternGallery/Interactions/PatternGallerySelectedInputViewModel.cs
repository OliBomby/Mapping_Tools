using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary>Owns the name form for importing selected hit objects.</summary>
public sealed partial class PatternGallerySelectedInputViewModel : ObservableObject
{
    /// <summary>Creates a selected-object import form.</summary>
    /// <param name="defaultName">The suggested display name.</param>
    public PatternGallerySelectedInputViewModel(string defaultName)
    {
        Name = defaultName;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the pattern display name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Gets the latest correction message.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets the command that validates and accepts the form.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the window-close callback installed by the adapter.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Error = "A pattern name is required.";
            return;
        }

        Error = string.Empty;
        Close(Name);
    }
}
