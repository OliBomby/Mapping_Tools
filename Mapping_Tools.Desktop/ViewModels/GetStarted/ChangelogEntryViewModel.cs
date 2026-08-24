namespace Mapping_Tools.Desktop.ViewModels.GetStarted;

/// <summary>Displays one bundled offline release note.</summary>
/// <param name="Title">Release-note heading.</param>
/// <param name="Text">Release-note body.</param>
public sealed record ChangelogEntryViewModel(string Title, string Text);
