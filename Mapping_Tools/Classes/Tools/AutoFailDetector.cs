using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Classes.Tools;

/// <summary>
/// Legacy WPF adapter. Detection and fix planning live in Core so both frontends execute
/// the same algorithm while this type preserves the old MessageBox-shaped API.
/// </summary>
public sealed class AutoFailDetector
{
    private readonly AutoFailDetectorEngine _engine;

    public AutoFailDetector(
        List<HitObject> hitObjects,
        int mapStartTime,
        int mapEndTime,
        int autoFailCheckTime,
        int approachTime,
        int window50,
        int physicsTime)
    {
        _engine = new AutoFailDetectorEngine(
            hitObjects,
            mapStartTime,
            mapEndTime,
            autoFailCheckTime,
            approachTime,
            window50,
            physicsTime);
    }

    public List<double> UnloadingObjects { get; private set; } = [];
    public List<double> PotentialUnloadingObjects { get; private set; } = [];
    public List<double> Disruptors { get; private set; } = [];

    public void SetHitObjects(List<HitObject> hitObjects) => _engine.SetHitObjects(hitObjects);

    public bool DetectAutoFail()
    {
        AutoFailAnalysis analysis = _engine.Analyze();
        UnloadingObjects = analysis.UnloadingObjects.ToList();
        PotentialUnloadingObjects = analysis.PotentialUnloadingObjects.ToList();
        Disruptors = analysis.Disruptors.ToList();
        return analysis.HasAutoFail;
    }

    public bool AutoFailFixDialogue(bool autoPlaceFix)
    {
        int solutionNumber = 0;
        foreach (AutoFailFixPlan plan in _engine.GetFixPlans())
        {
            MessageBoxResult result = MessageBox.Show(
                plan.Guide + "\n\nDo you want to use this solution?",
                $"Solution {++solutionNumber}",
                MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.No)
            {
                continue;
            }
            if (result == MessageBoxResult.Yes && autoPlaceFix)
            {
                _engine.ApplyFix(plan);
                return true;
            }
            return false;
        }
        return false;
    }
}
