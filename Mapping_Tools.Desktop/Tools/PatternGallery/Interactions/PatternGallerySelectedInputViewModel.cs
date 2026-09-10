using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary>Owns the name form for importing selected hit objects.</summary>
public sealed partial class PatternGallerySelectedInputViewModel : ObservableValidator
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
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "A pattern name is required.")]
    public partial string Name { get; set; }

    /// <summary>Gets the command that validates and accepts the form.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the window-close callback installed by the adapter.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    private void Accept()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        Close(Name);
    }
}
