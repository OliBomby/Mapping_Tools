using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using Newtonsoft.Json;

namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Stores the editable state of a dialog independently from its owning project.</summary>
public sealed class DialogUndoHistory : IProjectUndoHistory, IDisposable
{
    private const int maxStates = 51;
    private readonly object model;
    private readonly Action refreshBindings;
    private readonly List<DialogState> states = [];
    private readonly HashSet<INotifyPropertyChanged> observedProperties = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<INotifyCollectionChanged> observedCollections = new(ReferenceEqualityComparer.Instance);
    private int cursor;
    private int editDepth;
    private int gestureDepth;
    private int suspendedDepth;
    private bool capturePending;
    private bool captureScheduled;
    private bool restoring;

    /// <summary>Creates a history for a dialog's editable data context.</summary>
    /// <param name="model">The dialog data context.</param>
    /// <param name="refreshBindings">Refreshes controls after a state is restored.</param>
    public DialogUndoHistory(object model, Action refreshBindings)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        this.refreshBindings = refreshBindings ?? throw new ArgumentNullException(nameof(refreshBindings));
        states.Add(ReadState());
        RefreshObservers();
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
        capturePending = true;
        editDepth++;
        return new Scope(() => EndEdit(false));
    }

    /// <inheritdoc />
    public IDisposable BeginGesture()
    {
        editDepth++;
        gestureDepth++;
        return new Scope(() => EndEdit(true));
    }

    /// <inheritdoc />
    public IDisposable SuspendRecording()
    {
        suspendedDepth++;
        return new Scope(() => suspendedDepth--);
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
        DialogState next = ReadState();
        capturePending = false;
        if (next.SameAs(states[cursor])) return;

        if (CanRedo) states.RemoveRange(cursor + 1, states.Count - cursor - 1);
        states.Add(next);
        cursor++;
        if (states.Count > maxStates)
        {
            states.RemoveAt(0);
            cursor--;
        }

        RefreshObservers();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void AddExternalChange(IProjectUndoExternalChange change)
    {
        throw new NotSupportedException("Dialog drafts do not edit external files.");
    }

    /// <inheritdoc />
    public void Undo()
    {
        if (editDepth > gestureDepth) return;
        CaptureCurrent(true);
        if (CanUndo) Restore(cursor - 1);
    }

    /// <inheritdoc />
    public void Redo()
    {
        if (editDepth > gestureDepth) return;
        CaptureCurrent(true);
        if (CanRedo) Restore(cursor + 1);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var item in observedProperties) item.PropertyChanged -= OnPropertyChanged;
        foreach (var item in observedCollections) item.CollectionChanged -= OnCollectionChanged;
        observedProperties.Clear();
        observedCollections.Clear();
    }

    private void EndEdit(bool gesture)
    {
        editDepth--;
        if (gesture) gestureDepth--;
        if (editDepth == 0 && capturePending) Capture();
    }

    private void Restore(int target)
    {
        restoring = true;
        try
        {
            DialogState state = states[target];
            foreach (var collection in state.Collections)
            {
                collection.Target.Clear();
                foreach (var item in collection.Items) collection.Target.Add(item);
            }

            foreach (var value in state.Values) value.Property.SetValue(value.Target, value.Value);
            cursor = target;
            capturePending = false;
            refreshBindings();
            RefreshObservers();
        }
        finally
        {
            restoring = false;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private DialogState ReadState()
    {
        List<ValueEntry> values = [];
        List<CollectionEntry> collections = [];
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        Visit(model, values, collections, visited);
        return new DialogState(values, collections);
    }

    private static void Visit(
        object? value,
        List<ValueEntry> values,
        List<CollectionEntry> collections,
        HashSet<object> visited)
    {
        if (value is null || IsScalar(value.GetType()) || !visited.Add(value)
            || value is ICommand or Delegate or Control or Task) return;

        if (value is IList list)
        {
            if (!list.IsReadOnly && !list.IsFixedSize)
                collections.Add(new CollectionEntry(list, list.Cast<object?>().ToArray()));
            foreach (var item in list) Visit(item, values, collections, visited);
            return;
        }

        if (value is IDictionary dictionary)
        {
            foreach (var item in dictionary.Values) Visit(item, values, collections, visited);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable) Visit(item, values, collections, visited);
            return;
        }

        string? nameSpace = value.GetType().Namespace;
        if (nameSpace is null || !nameSpace.StartsWith("Mapping_Tools.", StringComparison.Ordinal)) return;

        foreach (var property in GetStateProperties(value))
        {
            object? child = property.GetValue(value);
            if (property.SetMethod?.IsPublic == true
                && (IsScalar(property.PropertyType) || child is not null && IsScalar(child.GetType())
                    || IsRestorableReference(property.PropertyType)))
                values.Add(new ValueEntry(value, property, child));
            Visit(child, values, collections, visited);
        }
    }

    private static bool IsScalar(Type type)
    {
        return type.IsValueType || type == typeof(string);
    }

    private static bool IsRestorableReference(Type type)
    {
        return type.IsClass
               && type.Namespace?.StartsWith("Mapping_Tools.", StringComparison.Ordinal) == true
               && !typeof(IEnumerable).IsAssignableFrom(type)
               && !typeof(ICommand).IsAssignableFrom(type);
    }

    private void RefreshObservers()
    {
        foreach (var item in observedProperties) item.PropertyChanged -= OnPropertyChanged;
        foreach (var item in observedCollections) item.CollectionChanged -= OnCollectionChanged;
        observedProperties.Clear();
        observedCollections.Clear();

        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        Observe(model, visited);
    }

    private void Observe(object? value, HashSet<object> visited)
    {
        if (value is null || IsScalar(value.GetType()) || !visited.Add(value)
            || value is ICommand or Delegate or Control or Task) return;

        if (value is INotifyPropertyChanged changed)
        {
            changed.PropertyChanged += OnPropertyChanged;
            observedProperties.Add(changed);
        }

        if (value is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnCollectionChanged;
            observedCollections.Add(collection);
        }

        if (value is IDictionary dictionary)
        {
            foreach (var item in dictionary.Values) Observe(item, visited);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable) Observe(item, visited);
            return;
        }

        string? nameSpace = value.GetType().Namespace;
        if (nameSpace is null || !nameSpace.StartsWith("Mapping_Tools.", StringComparison.Ordinal)) return;
        foreach (var property in GetStateProperties(value))
        {
            if (IsScalar(property.PropertyType)) continue;
            Observe(property.GetValue(value), visited);
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (restoring || suspendedDepth > 0) return;
        capturePending = true;
        ScheduleCapture();
    }

    private static IEnumerable<PropertyInfo> GetStateProperties(object owner)
    {
        return owner.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0
                               && (owner is INotifyPropertyChanged
                                   ? property.IsDefined(typeof(UndoableAttribute))
                                   : !property.IsDefined(typeof(JsonIgnoreAttribute))));
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (restoring || suspendedDepth > 0) return;
        capturePending = true;
        ScheduleCapture();
    }

    private void ScheduleCapture()
    {
        if (captureScheduled) return;
        captureScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            captureScheduled = false;
            if (capturePending) Capture();
        }, DispatcherPriority.Background);
    }

    private sealed record ValueEntry(object Target, PropertyInfo Property, object? Value);

    private sealed record CollectionEntry(IList Target, IReadOnlyList<object?> Items);

    private sealed record DialogState(
        IReadOnlyList<ValueEntry> Values,
        IReadOnlyList<CollectionEntry> Collections)
    {
        public bool SameAs(DialogState other)
        {
            if (Values.Count != other.Values.Count || Collections.Count != other.Collections.Count) return false;
            for (int index = 0; index < Values.Count; index++)
            {
                var left = Values[index];
                var right = other.Values[index];
                if (!ReferenceEquals(left.Target, right.Target) || left.Property != right.Property
                    || !Equals(left.Value, right.Value)) return false;
            }

            for (int index = 0; index < Collections.Count; index++)
            {
                var left = Collections[index];
                var right = other.Collections[index];
                if (!ReferenceEquals(left.Target, right.Target)
                    || !left.Items.SequenceEqual(right.Items, ReferenceEqualityComparer.Instance)) return false;
            }

            return true;
        }
    }

    private sealed class Scope(Action close) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            close();
        }
    }
}
