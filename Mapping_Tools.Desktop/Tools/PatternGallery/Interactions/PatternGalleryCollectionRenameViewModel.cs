using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary>Owns the two names edited by the Pattern Gallery collection rename form.</summary>
public sealed partial class PatternGalleryCollectionRenameViewModel : ObservableObject
{
    /// <summary>Creates the rename form with the current collection names.</summary>
    /// <param name="newName">The current display name.</param>
    /// <param name="newFolderName">The current collection directory name.</param>
    public PatternGalleryCollectionRenameViewModel(string newName, string newFolderName)
    {
        NewName = newName;
        NewFolderName = newFolderName;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the collection's new display name.</summary>
    [ObservableProperty]
    public partial string NewName { get; set; }

    /// <summary>Gets or sets the collection's new directory name.</summary>
    [ObservableProperty]
    public partial string NewFolderName { get; set; }

    /// <summary>Gets the latest correction message.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets the command that validates and accepts both names.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the modal close callback installed by the dialog adapter.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            Error = "A collection name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewFolderName))
        {
            Error = "A collection directory name is required.";
            return;
        }

        Error = string.Empty;
        Close(new PatternGalleryCollectionRenameInput(NewName, NewFolderName));
    }
}
