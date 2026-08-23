using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.SystemTools;
using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Interactions;

/// <summary>Returns Pattern Gallery's two multi-field import dialog results.</summary>
public interface IPatternGalleryInputDialog
{
    /// <summary>Shows the raw-code import form.</summary>
    /// <param name="defaultName">The next suggested pattern name.</param>
    /// <returns>Submitted values, or <see langword="null" /> when cancelled.</returns>
    Task<PatternGalleryCodeInput?> ShowCodeAsync(string defaultName);

    /// <summary>Shows the source-file import form.</summary>
    /// <param name="defaultName">The next suggested pattern name.</param>
    /// <param name="defaultPath">The path selected by the file picker.</param>
    /// <returns>Submitted values, or <see langword="null" /> when cancelled.</returns>
    Task<PatternGalleryFileInput?> ShowFileAsync(string defaultName, string defaultPath);

    /// <summary>Shows editable pattern details and returns the submitted name.</summary>
    /// <param name="pattern">The metadata displayed by the dialog.</param>
    /// <returns>The new name, or <see langword="null" /> when cancelled.</returns>
    Task<string?> ShowDetailsAsync(PatternGalleryPattern pattern);
}

/// <summary>Carries submitted raw-code import values.</summary>
public sealed record PatternGalleryCodeInput(
    string Name,
    string HitObjects,
    string TimingPoints,
    double GlobalSv,
    GameMode GameMode);

/// <summary> Carries submitted source-file import values.</summary>
public sealed record PatternGalleryFileInput(
    string Name,
    string FilePath,
    string Filter,
    double StartTime,
    double EndTime);

/// <summary>Displays the typed Pattern Gallery import forms as owner-modal Avalonia windows.</summary>
public sealed class PatternGalleryInputDialog : IPatternGalleryInputDialog
{
    private readonly Func<Window> owner;

    /// <summary>Creates the dialog adapter.</summary>
    /// <param name="owner">Returns the initialized shell window.</param>
    public PatternGalleryInputDialog(Func<Window> owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc />
    public async Task<PatternGalleryCodeInput?> ShowCodeAsync(string defaultName)
    {
        var viewModel = PatternGalleryInputViewModel.ForCode(defaultName);
        PatternGalleryInputDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner());
        return result is PatternGalleryCodeInput input ? input : null;
    }

    /// <inheritdoc />
    public async Task<PatternGalleryFileInput?> ShowFileAsync(string defaultName, string defaultPath)
    {
        var viewModel = PatternGalleryInputViewModel.ForFile(defaultName, defaultPath);
        PatternGalleryInputDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner());
        return result is PatternGalleryFileInput input ? input : null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowDetailsAsync(PatternGalleryPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        PatternGalleryDetailsViewModel viewModel = new(pattern);
        PatternGalleryDetailsDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(owner());
        return result as string;
    }
}

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
