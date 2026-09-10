using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary>Owns the raw osu! code import dialog state.</summary>
public sealed partial class PatternGalleryCodeImportViewModel : ObservableValidator
{
    /// <summary>Creates a raw-code import form.</summary>
    /// <param name="defaultName">The suggested display name.</param>
    public PatternGalleryCodeImportViewModel(string defaultName)
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

    /// <summary>Gets or sets raw hit-object lines.</summary>
    [ObservableProperty]
    public partial string HitObjects { get; set; } = string.Empty;

    /// <summary>Gets or sets raw timing-point lines.</summary>
    [ObservableProperty]
    public partial string TimingPoints { get; set; } = string.Empty;

    /// <summary>Gets or sets the global slider multiplier.</summary>
    [ObservableProperty]
    public partial double GlobalSv { get; set; } = 1.4;

    /// <summary>Gets or sets the selected game mode.</summary>
    [ObservableProperty]
    public partial GameMode GameMode { get; set; } = GameMode.Standard;

    /// <summary>Gets the game modes shown in the form.</summary>
    public IReadOnlyList<GameMode> GameModes { get; } = Enum.GetValues<GameMode>();

    /// <summary>Gets the command that validates and accepts the form.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the dialog-close callback installed by the adapter.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    private void Accept()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        Close(new PatternGalleryCodeInput(Name, HitObjects, TimingPoints, GlobalSv, GameMode));
    }
}
