using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.HitsoundStuff;

/// <summary>
/// 
/// </summary>
public class HitsoundZone : INotifyPropertyChanged
{
    private bool isSelected;
    private string name;
    private string filename;
    private double xPos;
    private double yPos;
    private Hitsound hitsound;
    private SampleSet sampleSet;
    private SampleSet additionsSet;
    private int customIndex;

    public HitsoundZone() {
        isSelected = false;
        name = "";
        filename = "";
        xPos = -1;
        yPos = -1;
        hitsound = Hitsound.Normal;
        sampleSet = SampleSet.None;
        additionsSet = SampleSet.None;
        customIndex = 0;
    }

    public HitsoundZone(bool isSelected, string name, string filename, double xPos, double yPos, Hitsound hitsound, SampleSet sampleSet, SampleSet additionsSet, int customIndex) {
        this.isSelected = isSelected;
        this.name = name;
        this.filename = filename;
        this.xPos = xPos;
        this.yPos = yPos;
        this.hitsound = hitsound;
        this.sampleSet = sampleSet;
        this.additionsSet = additionsSet;
        this.customIndex = customIndex;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public double Distance(Vector2 pos) {
        double dx = XPos == -1 ? 0 : XPos - pos.X;
        double dy = YPos == -1 ? 0 : YPos - pos.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public HitsoundZone Copy() {
        return new HitsoundZone(IsSelected, Name, Filename, XPos, YPos, Hitsound, SampleSet, AdditionsSet, CustomIndex);
    }

    [JsonIgnore]
    public bool IsSelected {
        get => isSelected;
        set {
            if (isSelected == value) return;
            isSelected = value;
            OnPropertyChanged();
        }
    }

    public string Filename {
        get => filename;
        set {
            if (filename == value) return;
            filename = value;
            OnPropertyChanged();
        }
    }

    public string Name {
        get => name;
        set {
            if (name == value) return;
            name = value;
            OnPropertyChanged();
        }
    }

    public double XPos {
        get => xPos;
        set {
            if (xPos == value) return;
            xPos = value;
            OnPropertyChanged();
        }
    }

    public double YPos {
        get => yPos;
        set {
            if (yPos == value) return;
            yPos = value;
            OnPropertyChanged();
        }
    }

    public Hitsound Hitsound {
        get => hitsound;
        set {
            if (hitsound == value) return;
            hitsound = value;
            OnPropertyChanged();
        }
    }

    public SampleSet SampleSet {
        get => sampleSet;
        set {
            if (sampleSet == value) return;
            sampleSet = value;
            OnPropertyChanged();
        }
    }

    public SampleSet AdditionsSet {
        get => additionsSet;
        set {
            if (additionsSet == value) return;
            additionsSet = value;
            OnPropertyChanged();
        }
    }

    public int CustomIndex {
        get => customIndex;
        set {
            if (customIndex == value) return;
            customIndex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}