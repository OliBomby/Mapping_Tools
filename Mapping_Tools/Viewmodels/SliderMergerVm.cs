using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels;

public class SliderMergerVm : BindableBase
{
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

    private string timeCode;
    public string TimeCode {
        get => timeCode;
        set => Set(ref timeCode, value);
    }

    private ConnectionMode connectionModeSetting;
    public ConnectionMode ConnectionModeSetting {
        get => connectionModeSetting;
        set => Set(ref connectionModeSetting, value);
    }

    [JsonIgnore]
    public IEnumerable<ConnectionMode> ConnectionModes => Enum.GetValues(typeof(ConnectionMode)).Cast<ConnectionMode>();

    private double leniency;
    public double Leniency {
        get => leniency;
        set => Set(ref leniency, value);
    }

    private bool linearOnLinear;
    public bool LinearOnLinear {
        get => linearOnLinear;
        set => Set(ref linearOnLinear, value);
    }

    private bool mergeOnSliderEnd;
    public bool MergeOnSliderEnd {
        get => mergeOnSliderEnd;
        set => Set(ref mergeOnSliderEnd, value);
    }

    #endregion

    public SliderMergerVm() {
        ImportModeSetting = ImportMode.Selected;
        ConnectionModeSetting = ConnectionMode.Move;
        Leniency = 256;
        LinearOnLinear = false;
        MergeOnSliderEnd = true;
    }

    public enum ImportMode
    {
        Selected,
        Bookmarked,
        Time,
        Everything
    }

    public enum ConnectionMode
    {
        Move,
        Linear
    }
}