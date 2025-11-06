using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels;

public class SliderCompletionatorVm : BindableBase {
    #region Properties

    [JsonIgnore]
    public string[] Paths { get; set; }

    [JsonIgnore]
    public bool Quick { get; set; }

    private ImportMode importModeSetting;
    public ImportMode ImportModeSetting {
        get => importModeSetting;
        set {
            if (Set(ref importModeSetting, value)) {
                RaisePropertyChanged(nameof(TimeCodeBoxVisibility));
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

    [JsonIgnore]
    public Visibility TimeCodeBoxVisibility => ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

    private FreeVariable freeVariableSetting;
    public FreeVariable FreeVariableSetting {
        get => freeVariableSetting;
        set {
            if (Set(ref freeVariableSetting, value)) {
                RaisePropertyChanged(nameof(DurationBoxVisibility));
                RaisePropertyChanged(nameof(EndTimeBoxVisibility));
                RaisePropertyChanged(nameof(LengthBoxVisibility));
                RaisePropertyChanged(nameof(VelocityBoxVisibility));
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<FreeVariable> FreeVariables => Enum.GetValues(typeof(FreeVariable)).Cast<FreeVariable>();

    [JsonIgnore]
    public Visibility DurationBoxVisibility => FreeVariableSetting != FreeVariable.Duration && !UseEndTime ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility EndTimeBoxVisibility => FreeVariableSetting != FreeVariable.Duration && UseEndTime && !UseCurrentEditorTime ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility LengthBoxVisibility => FreeVariableSetting != FreeVariable.Length ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility VelocityBoxVisibility => FreeVariableSetting != FreeVariable.Velocity ? Visibility.Visible : Visibility.Collapsed;

    private string timeCode;
    public string TimeCode {
        get => timeCode;
        set => Set(ref timeCode, value);
    }

    private double duration;
    public double Duration {
        get => duration;
        set => Set(ref duration, value);
    }

    private double endTime;
    public double EndTime {
        get => endTime;
        set => Set(ref endTime, value);
    }

    private double length;
    public double Length {
        get => length;
        set => Set(ref length, value);
    }

    private double sliderVelocity;
    public double SliderVelocity {
        get => sliderVelocity;
        set => Set(ref sliderVelocity, value);
    }

    private bool moveAnchors;
    public bool MoveAnchors {
        get => moveAnchors;
        set => Set(ref moveAnchors, value);
    }

    private bool useEndTime;
    public bool UseEndTime {
        get => useEndTime;
        set {
            if (Set(ref useEndTime, value)) {
                RaisePropertyChanged(nameof(DurationBoxVisibility));
                RaisePropertyChanged(nameof(EndTimeBoxVisibility));
            }
        }
    }

    private bool useCurrentEditorTime;
    public bool UseCurrentEditorTime {
        get => useCurrentEditorTime;
        set {
            if (Set(ref useCurrentEditorTime, value)) {
                RaisePropertyChanged(nameof(DurationBoxVisibility));
                RaisePropertyChanged(nameof(EndTimeBoxVisibility));
            }
        }
    }

    private bool delegateSvToBpm;
    public bool DelegateToBpm {
        get => delegateSvToBpm;
        set => Set(ref delegateSvToBpm, value);
    }

    private bool removeSliderTicks;
    public bool RemoveSliderTicks {
        get => removeSliderTicks;
        set => Set(ref removeSliderTicks, value);
    }

    #endregion

    public SliderCompletionatorVm() {
        ImportModeSetting = ImportMode.Selected;
        Duration = -1;
        EndTime = -1;
        Length = 1;
        SliderVelocity = -1;
        MoveAnchors = false;
        UseEndTime = false;
        UseCurrentEditorTime = false;
        DelegateToBpm = false;
        RemoveSliderTicks = false;
        TimeCode = string.Empty;
    }

    public enum FreeVariable {
        Velocity,
        Length,
        Duration
    }

    public enum ImportMode {
        Selected,
        Bookmarked,
        Time,
        Everything
    }
}