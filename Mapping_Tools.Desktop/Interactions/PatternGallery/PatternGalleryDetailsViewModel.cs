using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.PatternGallery.Models;

namespace Mapping_Tools.Desktop.Interactions.PatternGallery;

/// <summary>Owns the editable and read-only values shown by the properties dialog.</summary>
public sealed partial class PatternGalleryDetailsViewModel : ObservableValidator
{
    /// <summary>Creates a details form from persisted pattern metadata.</summary>
    /// <param name="pattern">The pattern whose values are displayed.</param>
    public PatternGalleryDetailsViewModel(PatternGalleryPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Name = pattern.Name;
        CreationTimeText = pattern.CreationTime.ToString("G");
        LastUsedTimeText = pattern.LastUsedTime.ToString("G");
        UseCountText = pattern.UseCount.ToString(CultureInfo.InvariantCulture);
        ObjectCountText = pattern.ObjectCount.ToString(CultureInfo.InvariantCulture);
        DurationText = pattern.Duration.ToString();
        BeatLengthText = pattern.BeatLength.ToString("0.###", CultureInfo.InvariantCulture);
        FileName = pattern.FileName;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the editable display name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Gets the formatted creation timestamp.</summary>
    public string CreationTimeText { get; }

    /// <summary>Gets the formatted last-use timestamp.</summary>
    public string LastUsedTimeText { get; }

    /// <summary>Gets the formatted use count.</summary>
    public string UseCountText { get; }

    /// <summary>Gets the formatted object count.</summary>
    public string ObjectCountText { get; }

    /// <summary>Gets the formatted duration.</summary>
    public string DurationText { get; }

    /// <summary>Gets the formatted beat length.</summary>
    public string BeatLengthText { get; }

    /// <summary>Gets the persisted pattern filename.</summary>
    public string FileName { get; }

    /// <summary>Gets the latest correction message.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets the command that validates and accepts the form.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the window-close callback.</summary>
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
