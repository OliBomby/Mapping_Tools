using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Provides Desktop change notification for sample-generation fields.</summary>
public sealed class ObservableSampleGeneratingArgs : ObservableObject
{
    private readonly SampleGeneratingArgs model;

    /// <summary>Creates an adapter around the supplied sample-generation settings.</summary>
    /// <param name="model">The plain settings edited by this adapter.</param>
    public ObservableSampleGeneratingArgs(SampleGeneratingArgs model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>Gets the plain settings represented by this adapter.</summary>
    public SampleGeneratingArgs Model => model;

    /// <summary>Gets or sets the source audio or SoundFont path.</summary>
    public string Path
    {
        get => model.Path;
        set => SetValue(model.Path, value, next => model.Path = next);
    }

    /// <summary>Gets or sets the linear sample gain.</summary>
    public double Volume
    {
        get => model.Volume;
        set
        {
            if (Math.Abs(model.Volume - value) < double.Epsilon) return;
            model.Volume = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Velocity));
        }
    }

    /// <summary>Gets or sets the stereo pan.</summary>
    public double Panning
    {
        get => model.Panning;
        set => SetValue(model.Panning, value, next => model.Panning = next);
    }

    /// <summary>Gets or sets the pitch adjustment.</summary>
    public double PitchShift
    {
        get => model.PitchShift;
        set => SetValue(model.PitchShift, value, next => model.PitchShift = next);
    }

    /// <summary>Gets or sets the SoundFont bank, or -1 when unused.</summary>
    public int Bank
    {
        get => model.Bank;
        set => SetValue(model.Bank, value, next => model.Bank = next);
    }

    /// <summary>Gets or sets the SoundFont patch, or -1 when unused.</summary>
    public int Patch
    {
        get => model.Patch;
        set => SetValue(model.Patch, value, next => model.Patch = next);
    }

    /// <summary>Gets or sets the SoundFont instrument, or -1 when unused.</summary>
    public int Instrument
    {
        get => model.Instrument;
        set => SetValue(model.Instrument, value, next => model.Instrument = next);
    }

    /// <summary>Gets or sets the MIDI key, or -1 when unused.</summary>
    public int Key
    {
        get => model.Key;
        set => SetValue(model.Key, value, next => model.Key = next);
    }

    /// <summary>Gets or sets the generated SoundFont note length.</summary>
    public double Length
    {
        get => model.Length;
        set => SetValue(model.Length, value, next => model.Length = next);
    }

    /// <summary>Gets or sets the MIDI velocity and updates the corresponding gain.</summary>
    public int Velocity
    {
        get => model.Velocity;
        set
        {
            if (model.Velocity == value) return;
            model.Velocity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Volume));
        }
    }

    /// <summary>Gets whether the source is a SoundFont.</summary>
    public bool UsesSoundFont => model.UsesSoundFont;

    /// <summary>Gets whether this configuration supports copy and paste.</summary>
    public bool CanCopyPaste => model.CanCopyPaste;

    /// <summary>Gets the source-file extension.</summary>
    /// <returns>The extension including its leading period, or an empty string.</returns>
    public string GetExtension() => model.GetExtension();

    /// <summary>Creates an independent plain snapshot of the settings.</summary>
    /// <returns>A detached sample-generation snapshot.</returns>
    public SampleGeneratingArgs Snapshot() => model.Copy();

    private void SetValue<T>(T current, T value, Action<T> setter, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return;
        setter(value);
        OnPropertyChanged(propertyName);
    }
}
