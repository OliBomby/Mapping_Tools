using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Avalonia.Threading;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Stores immutable serialized states for one project feature.</summary>
/// <typeparam name="TProject">The feature's persisted project type.</typeparam>
public sealed class ProjectUndoHistory<TProject> : IProjectUndoHistory
{
    private const int maxStates = 51;
    private const int maxCompressedBytes = 8 * 1024 * 1024;
    private readonly IShellProjectFeature<TProject> feature;
    private readonly IProjectSerializer serializer;
    private readonly List<HistoryState> states;
    private readonly List<IProjectUndoExternalChange> pendingExternalChanges = [];
    private readonly HashSet<string> undoableProperties;
    private readonly Dictionary<string, Stack<IDisposable>> propertyEdits = [];
    private readonly HashSet<INotifyPropertyChanged> observedProperties = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<INotifyCollectionChanged> observedCollections = new(ReferenceEqualityComparer.Instance);
    private int cursor;
    private int editDepth;
    private int gestureDepth;
    private int suspendedDepth;
    private bool restoring;
    private bool refreshPending;
    private bool refreshNeeded;
    private bool capturePending;
    private string currentProject;

    /// <summary>Creates a history from the feature's current project state.</summary>
    /// <param name="feature">The project feature to capture and restore.</param>
    /// <param name="serializer">Freezes project states in memory.</param>
    public ProjectUndoHistory(IShellProjectFeature<TProject> feature, IProjectSerializer serializer)
    {
        this.feature = feature ?? throw new ArgumentNullException(nameof(feature));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        currentProject = Snapshot();
        states = [new HistoryState(Compress(currentProject), [])];
        undoableProperties = feature.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(UndoableAttribute)))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (feature is INotifyPropertyChanging changing) changing.PropertyChanging += OnPropertyChanging;
        if (feature is INotifyPropertyChanged changed) changed.PropertyChanged += OnPropertyChanged;
        RefreshObservedObjects();
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public bool CanUndo => cursor > 0;

    /// <inheritdoc />
    public bool CanRedo => cursor < states.Count - 1;

    /// <inheritdoc />
    public bool IsRestoring => restoring;

    /// <inheritdoc />
    public IDisposable BeginEdit()
    {
        if (!restoring && suspendedDepth == 0)
        {
            if (editDepth == 0 && capturePending) Capture();
            editDepth++;
            capturePending = true;
        }
        return new EditScope(this, !restoring && suspendedDepth == 0, false);
    }

    /// <inheritdoc />
    public IDisposable BeginGesture()
    {
        if (!restoring && suspendedDepth == 0)
        {
            if (editDepth == 0 && capturePending) Capture();
            editDepth++;
            gestureDepth++;
        }
        return new EditScope(this, !restoring && suspendedDepth == 0, true);
    }

    /// <inheritdoc />
    public IDisposable SuspendRecording()
    {
        suspendedDepth++;
        return new SuspendScope(this);
    }

    /// <inheritdoc />
    public void Capture()
    {
        CaptureCurrent(false);
    }

    private void CaptureCurrent(bool allowGesture)
    {
        if (restoring || suspendedDepth > 0
            || editDepth > (allowGesture ? gestureDepth : 0)) return;

        string next = Snapshot();
        capturePending = false;
        if (string.Equals(next, currentProject, StringComparison.Ordinal)
            && pendingExternalChanges.Count == 0) return;

        if (CanRedo) states.RemoveRange(cursor + 1, states.Count - cursor - 1);
        states.Add(new HistoryState(Compress(next), pendingExternalChanges.ToArray()));
        pendingExternalChanges.Clear();
        currentProject = next;
        cursor++;

        while (states.Count > maxStates || states.Count > 1 && StoredBytes() > maxCompressedBytes)
        {
            states.RemoveAt(0);
            cursor--;
            states[0] = states[0] with { ExternalFromPrevious = [] };
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void AddExternalChange(IProjectUndoExternalChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (editDepth == 0) throw new InvalidOperationException("External changes require an edit transaction.");
        pendingExternalChanges.Add(change);
    }

    /// <summary>Updates metadata in every saved state without creating an undo step.</summary>
    /// <param name="update">Applies the same non-edit change to each saved project.</param>
    public void RebaseUntracked(Action<TProject> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        for (int index = 0; index < states.Count; index++)
        {
            TProject project = serializer.Deserialize<TProject>(feature.ProjectDefinition.ConfigSchema, Decompress(states[index].Project));
            update(project);
            states[index] = states[index] with
            {
                Project = Compress(serializer.Serialize(feature.ProjectDefinition.ConfigSchema, project)),
            };
        }
        currentProject = Decompress(states[cursor].Project);
    }

    /// <inheritdoc />
    public void Undo()
    {
        if (editDepth > gestureDepth) return;
        CaptureCurrent(true);
        if (!CanUndo) return;
        Restore(cursor - 1);
    }

    /// <inheritdoc />
    public void Redo()
    {
        if (editDepth > gestureDepth) return;
        CaptureCurrent(true);
        if (!CanRedo) return;
        Restore(cursor + 1);
    }

    private string Snapshot()
    {
        return serializer.Serialize(feature.ProjectDefinition.ConfigSchema, feature.Snapshot());
    }

    private void Restore(int target)
    {
        TProject project = serializer.Deserialize<TProject>(feature.ProjectDefinition.ConfigSchema, Decompress(states[target].Project));
        TProject original = serializer.Deserialize<TProject>(feature.ProjectDefinition.ConfigSchema, currentProject);
        IReadOnlyList<IProjectUndoExternalChange> changes = target < cursor
            ? states[cursor].ExternalFromPrevious
            : states[target].ExternalFromPrevious;
        bool undo = target < cursor;
        var applied = new List<IProjectUndoExternalChange>();
        restoring = true;
        try
        {
            foreach (var change in undo ? changes.Reverse() : changes)
            {
                if (undo) change.Undo();
                else change.Redo();
                applied.Add(change);
            }

            try
            {
                feature.Install(project);
            }
            catch
            {
                feature.Install(original);
                throw;
            }

            cursor = target;
            currentProject = Decompress(states[target].Project);
            capturePending = false;
        }
        catch
        {
            foreach (var change in applied.AsEnumerable().Reverse())
            {
                if (undo) change.Redo();
                else change.Undo();
            }

            throw;
        }
        finally
        {
            restoring = false;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleRefresh();
    }

    private void EndEdit(bool gesture)
    {
        editDepth--;
        if (gesture) gestureDepth--;
        if (editDepth == 0 && capturePending) Capture();
    }

    private void OnPropertyChanging(object? sender, PropertyChangingEventArgs eventArgs)
    {
        if (restoring || suspendedDepth > 0
            || eventArgs.PropertyName is not { } name || !undoableProperties.Contains(name)) return;
        if (!propertyEdits.TryGetValue(name, out var scopes)) propertyEdits[name] = scopes = new Stack<IDisposable>();
        scopes.Push(BeginEdit());
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (restoring || suspendedDepth > 0) return;
        bool closedEdit = false;
        if (eventArgs.PropertyName is { } name
            && propertyEdits.TryGetValue(name, out var scopes)
            && scopes.Count > 0)
        {
            scopes.Pop().Dispose();
            closedEdit = true;
        }

        if (eventArgs.PropertyName is { } changedName && undoableProperties.Contains(changedName))
        {
            if (!closedEdit) capturePending = true;
            ScheduleRefresh();
        }
    }

    private void OnObservedPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (restoring || suspendedDepth > 0 || sender is null) return;
        var property = eventArgs.PropertyName is { Length: > 0 } name ? sender.GetType().GetProperty(name) : null;
        if (!string.IsNullOrEmpty(eventArgs.PropertyName)
            && property?.IsDefined(typeof(UndoableAttribute)) != true) return;
        capturePending = true;
        Schedule(property is null || !property.PropertyType.IsValueType && property.PropertyType != typeof(string));
    }

    private void OnObservedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (restoring || suspendedDepth > 0) return;
        capturePending = true;
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        Schedule(true);
    }

    private void Schedule(bool refresh)
    {
        if (restoring || suspendedDepth > 0) return;
        refreshNeeded |= refresh;
        if (refreshPending) return;
        refreshPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            refreshPending = false;
            if (refreshNeeded) RefreshObservedObjects();
            refreshNeeded = false;
            if (capturePending) Capture();
        }, DispatcherPriority.Background);
    }

    private void RefreshObservedObjects()
    {
        foreach (var item in observedProperties) item.PropertyChanged -= OnObservedPropertyChanged;
        foreach (var item in observedCollections) item.CollectionChanged -= OnObservedCollectionChanged;
        observedProperties.Clear();
        observedCollections.Clear();

        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        Visit(feature, visited);
    }

    private void Visit(object? value, HashSet<object> visited)
    {
        if (value is null || value is string || value.GetType().IsValueType || !visited.Add(value)) return;

        if (value is INotifyPropertyChanged changed && !ReferenceEquals(value, feature))
        {
            changed.PropertyChanged += OnObservedPropertyChanged;
            observedProperties.Add(changed);
        }

        if (value is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnObservedCollectionChanged;
            observedCollections.Add(collection);
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable) Visit(item, visited);
            return;
        }

        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0
                || !property.IsDefined(typeof(UndoableAttribute))) continue;
            Visit(property.GetValue(value), visited);
        }
    }

    private long StoredBytes()
    {
        return states.Sum(state => (long)state.Project.Length
                                   + state.ExternalFromPrevious.Sum(change => change.EstimatedBytes));
    }

    private static byte[] Compress(string json)
    {
        using MemoryStream output = new();
        using (BrotliStream stream = new(output, CompressionLevel.Fastest, true))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes);
        }

        return output.ToArray();
    }

    private static string Decompress(byte[] compressed)
    {
        using MemoryStream input = new(compressed);
        using BrotliStream stream = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        stream.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private sealed class EditScope(ProjectUndoHistory<TProject> history, bool active, bool gesture) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (active) history.EndEdit(gesture);
        }
    }

    private sealed class SuspendScope(ProjectUndoHistory<TProject> history) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            history.suspendedDepth--;
        }
    }

    private sealed record HistoryState(byte[] Project, IReadOnlyList<IProjectUndoExternalChange> ExternalFromPrevious);
}
