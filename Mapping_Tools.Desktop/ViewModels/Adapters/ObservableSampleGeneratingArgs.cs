using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Provides Desktop change notification for sample-generation fields.</summary>
public sealed partial class ObservableSampleGeneratingArgs : ObservableObject
{
    /// <summary>Creates an adapter around the supplied sample-generation settings.</summary>
    /// <param name="model">The plain settings edited by this adapter.</param>
    public ObservableSampleGeneratingArgs(SampleGeneratingArgs model)
    {
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        Path = model.Path;
        Volume = model.Volume;
        Panning = model.Panning;
        PitchShift = model.PitchShift;
        Bank = model.Bank;
        Patch = model.Patch;
        Instrument = model.Instrument;
        Key = model.Key;
        Length = model.Length;
        Velocity = model.Velocity;
    }

    /// <summary>Gets the plain settings represented by this adapter.</summary>
    public SampleGeneratingArgs Model { get; }

    /// <summary>Gets or sets the source audio or SoundFont path.</summary>
    [ObservableProperty]
    public partial string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the linear sample gain.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Velocity))]
    public partial double Volume { get; set; }

    /// <summary>Gets or sets the stereo pan.</summary>
    [ObservableProperty]
    public partial double Panning { get; set; }

    /// <summary>Gets or sets the pitch adjustment.</summary>
    [ObservableProperty]
    public partial double PitchShift { get; set; }

    /// <summary>Gets or sets the SoundFont bank, or -1 when unused.</summary>
    [ObservableProperty]
    public partial int Bank { get; set; }

    /// <summary>Gets or sets the SoundFont patch, or -1 when unused.</summary>
    [ObservableProperty]
    public partial int Patch { get; set; }

    /// <summary>Gets or sets the SoundFont instrument, or -1 when unused.</summary>
    [ObservableProperty]
    public partial int Instrument { get; set; }

    /// <summary>Gets or sets the MIDI key, or -1 when unused.</summary>
    [ObservableProperty]
    public partial int Key { get; set; }

    /// <summary>Gets or sets the generated SoundFont note length.</summary>
    [ObservableProperty]
    public partial double Length { get; set; }

    /// <summary>Gets or sets the MIDI velocity and updates the corresponding gain.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Volume))]
    public partial int Velocity { get; set; }

    /// <summary>Gets whether the source is a SoundFont.</summary>
    public bool UsesSoundFont => Model.UsesSoundFont;

    /// <summary>Gets whether this configuration supports copy and paste.</summary>
    public bool CanCopyPaste => Model.CanCopyPaste;

    /// <summary>Gets the source-file extension.</summary>
    /// <returns>The extension including its leading period, or an empty string.</returns>
    public string GetExtension()
    {
        return Model.GetExtension();
    }

    /// <summary>Creates an independent plain snapshot of the settings.</summary>
    /// <returns>A detached sample-generation snapshot.</returns>
    public SampleGeneratingArgs Snapshot()
    {
        return Model.Copy();
    }

    partial void OnPathChanged(string value)
    {
        Model.Path = value;
    }

    partial void OnVolumeChanged(double value)
    {
        Model.Volume = value;
    }

    partial void OnPanningChanged(double value)
    {
        Model.Panning = value;
    }

    partial void OnPitchShiftChanged(double value)
    {
        Model.PitchShift = value;
    }

    partial void OnBankChanged(int value)
    {
        Model.Bank = value;
    }

    partial void OnPatchChanged(int value)
    {
        Model.Patch = value;
    }

    partial void OnInstrumentChanged(int value)
    {
        Model.Instrument = value;
    }

    partial void OnKeyChanged(int value)
    {
        Model.Key = value;
    }

    partial void OnLengthChanged(double value)
    {
        Model.Length = value;
    }

    partial void OnVelocityChanged(int value)
    {
        Model.Velocity = value;
    }
}
