using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Displays one bundled offline release note.</summary>
/// <param name="Title">Release-note heading.</param>
/// <param name="Text">Release-note body.</param>
public sealed record ChangelogEntryViewModel(string Title, string Text);
