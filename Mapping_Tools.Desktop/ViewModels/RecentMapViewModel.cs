using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Displays one persisted recent-map entry without loading it.</summary>
/// <param name="FileName">Filename used in the compact list.</param>
/// <param name="FullPath">Complete path retained for a later open-map workflow.</param>
/// <param name="DisplayDate">Legacy-compatible timestamp text.</param>
public sealed record RecentMapViewModel(
    string FileName,
    string FullPath,
    string DisplayDate);

