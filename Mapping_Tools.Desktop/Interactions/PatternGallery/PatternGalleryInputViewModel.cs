using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Desktop.Interactions.PatternGallery;

/// <summary>Owns validation and binding state for a Pattern Gallery import form.</summary>
public sealed partial class PatternGalleryInputViewModel : ObservableValidator
{
    private PatternGalleryInputViewModel(bool isCode, string defaultName, string? defaultPath)
    {
        IsCode = isCode;
        Name = defaultName;
        FilePath = defaultPath ?? string.Empty;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the pattern display name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Gets or sets raw hit-object lines.</summary>
    [ObservableProperty]
    public partial string HitObjects { get; set; } = string.Empty;

    /// <summary>Gets or sets raw timing-point lines.</summary>
    [ObservableProperty]
    public partial string TimingPoints { get; set; } = string.Empty;

    /// <summary>Gets or sets the source global slider multiplier as text.</summary>
    [ObservableProperty]
    public partial string GlobalSvText { get; set; } = "1.4";

    /// <summary>Gets or sets the selected game mode.</summary>
    [ObservableProperty]
    public partial GameMode GameMode { get; set; } = GameMode.Standard;

    /// <summary>Gets or sets the source file path.</summary>
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional time-code filter.</summary>
    [ObservableProperty]
    public partial string Filter { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional lower time bound as text.</summary>
    [ObservableProperty]
    public partial string StartTimeText { get; set; } = "-1";

    /// <summary>Gets or sets the optional upper time bound as text.</summary>
    [ObservableProperty]
    public partial string EndTimeText { get; set; } = "-1";

    /// <summary>Gets the game modes shown in the code form.</summary>
    public IReadOnlyList<GameMode> GameModes { get; } = Enum.GetValues<GameMode>();

    /// <summary>Gets whether the raw-code fields are visible.</summary>
    public bool IsCode { get; }

    /// <summary>Gets whether the source-file fields are visible.</summary>
    public bool IsFile => !IsCode;

    /// <summary>Gets the latest correction message.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets the command that validates and accepts the form.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that dismisses the form.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets or sets the window-close callback installed by the adapter.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    /// <summary>Creates a raw-code import form.</summary>
    /// <param name="defaultName">The suggested display name.</param>
    /// <returns>The initialized form state.</returns>
    public static PatternGalleryInputViewModel ForCode(string defaultName)
    {
        return new PatternGalleryInputViewModel(true, defaultName, null);
    }

    /// <summary>Creates a source-file import form.</summary>
    /// <param name="defaultName">The suggested display name.</param>
    /// <param name="defaultPath">The selected source path.</param>
    /// <returns>The initialized form state.</returns>
    public static PatternGalleryInputViewModel ForFile(string defaultName, string defaultPath)
    {
        return new PatternGalleryInputViewModel(false, defaultName, defaultPath);
    }

    private void Accept()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(Name))
        {
            Error = "A pattern name is required.";
            return;
        }

        if (IsCode)
        {
            if (!double.TryParse(GlobalSvText, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double globalSv))
            {
                Error = "Global SV must be a valid number.";
                return;
            }

            Close(new PatternGalleryCodeInput(Name, HitObjects, TimingPoints, globalSv, GameMode));
            return;
        }

        if (string.IsNullOrWhiteSpace(FilePath))
        {
            Error = "A pattern file path is required.";
            return;
        }

        if (!TryParseOptionalTime(StartTimeText, out double startTime) || !TryParseOptionalTime(EndTimeText, out double endTime))
        {
            Error = "Start and end time must be valid numbers.";
            return;
        }

        Close(new PatternGalleryFileInput(Name, FilePath, Filter, startTime, endTime));
    }

    private static bool TryParseOptionalTime(string value, out double result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = -1;
            return true;
        }

        if (double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out result))
            return true;

        try
        {
            result = TypeConverters.ParseOsuTimestamp(value).TotalMilliseconds;
            return true;
        }
        catch (Exception)
        {
            result = -1;
            return false;
        }
    }
}

