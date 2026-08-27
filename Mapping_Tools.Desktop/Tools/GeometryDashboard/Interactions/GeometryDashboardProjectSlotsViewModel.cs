using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Interactions;

/// <summary>Edits ordered Geometry Dashboard save slots.</summary>
public sealed partial class GeometryDashboardProjectSlotsViewModel : ObservableObject
{
    private readonly Action<SnappingToolsSaveSlot> loadSlot;
    private readonly Action refreshHotkeys;

    /// <summary>Creates the save-slot editor over the live project.</summary>
    public GeometryDashboardProjectSlotsViewModel(
        SnappingToolsProject project,
        Action<SnappingToolsSaveSlot> loadSlot,
        Action refreshHotkeys)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        this.loadSlot = loadSlot ?? throw new ArgumentNullException(nameof(loadSlot));
        this.refreshHotkeys = refreshHotkeys ?? throw new ArgumentNullException(nameof(refreshHotkeys));
    }

    /// <summary>Gets the live project slots.</summary>
    public SnappingToolsProject Project { get; }

    /// <summary>Gets or sets the selected slot.</summary>
    [ObservableProperty]
    public partial SnappingToolsSaveSlot? SelectedSlot { get; set; }

    /// <summary>Gets the extended-selection save slots currently selected in the list.</summary>
    public ObservableCollection<SnappingToolsSaveSlot> SelectedSlots { get; } = [];

    /// <summary>Receives the window close action.</summary>
    public Action? Close { get; set; }

    /// <summary>Replaces the extended list selection supplied by the Avalonia list control.</summary>
    /// <param name="slots">The selected live save slots.</param>
    public void SetSelectedSlots(IEnumerable<SnappingToolsSaveSlot> slots)
    {
        SelectedSlots.Clear();
        foreach (var slot in slots) SelectedSlots.Add(slot);
        SelectedSlot = SelectedSlots.LastOrDefault();
    }

    /// <summary>Adds a default slot after the existing slots.</summary>
    [RelayCommand]
    private void Add()
    {
        SnappingToolsSaveSlot slot;
        lock (Project)
        {
            slot = new SnappingToolsSaveSlot { Name = $"Save {Project.SaveSlots.Count + 1}" };
            Project.SaveToSlot(slot);
            Project.SaveSlots.Add(slot);
        }

        SelectedSlot = slot;
    }

    /// <summary>Removes the selected slot or the last slot.</summary>
    [RelayCommand]
    private void Remove()
    {
        var slots = SelectedSlots.Count > 0
            ? SelectedSlots.ToArray()
            : SelectedSlot is not null
                ? [SelectedSlot]
                : [];
        if (slots.Length == 0 && Project.SaveSlots.Count > 0) slots = [Project.SaveSlots[^1]];
        lock (Project)
        {
            foreach (var slot in slots) Project.SaveSlots.Remove(slot);
        }

        SelectedSlots.Clear();
        lock (Project)
        {
            SelectedSlot = Project.SaveSlots.LastOrDefault();
        }
    }

    /// <summary>Duplicates the selected slot using the legacy copy suffix.</summary>
    [RelayCommand]
    private void Duplicate()
    {
        var slots = SelectedSlots.Count > 0
            ? SelectedSlots.ToArray()
            : SelectedSlot is not null
                ? [SelectedSlot]
                : [];
        SnappingToolsSaveSlot? lastCopy = null;
        lock (Project)
        {
            foreach (var slot in slots)
            {
                var copy = (SnappingToolsSaveSlot)slot.Clone();
                copy.Name += " - Copy";
                Project.SaveSlots.Insert(Project.SaveSlots.IndexOf(slot) + 1, copy);
                lastCopy = copy;
            }
        }

        if (lastCopy is not null) SetSelectedSlots([lastCopy]);
    }

    /// <summary>Loads the selected slot into the active dashboard.</summary>
    [RelayCommand]
    private void Load(SnappingToolsSaveSlot? slot = null)
    {
        if (slot is not null) loadSlot(slot);
        else if (SelectedSlot is not null) loadSlot(SelectedSlot);
    }

    /// <summary>Saves current dashboard preferences into the selected slot.</summary>
    [RelayCommand]
    private void Save(SnappingToolsSaveSlot? slot = null)
    {
        lock (Project)
        {
            if (slot is not null) Project.SaveToSlot(slot);
            else if (SelectedSlot is not null) Project.SaveToSlot(SelectedSlot);
        }
    }

    /// <summary>Re-registers all save-slot hotkeys after editing their definitions.</summary>
    [RelayCommand]
    private void RefreshHotkeys()
    {
        refreshHotkeys();
    }
}
