using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.SystemTools;
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

