using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Events;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.ToolHelpers;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Exporting {
    public class HitsoundExporter : IHitsoundExporter {
        public GameMode ExportGameMode { get; set; }
        public string BeatmapVersionName { get; set; }
        public bool UseGreenlines { get; set; }
        public bool UseStoryboard { get; set; }

        public HitsoundExporter(GameMode exportGameMode, string beatmapVersionName) {
            ExportGameMode = exportGameMode;
            BeatmapVersionName = beatmapVersionName;
        }

        public void ExportHitsounds(ICollection<IHitsoundEvent> hitsounds, Beatmap beatmap) {
            if (UseStoryboard) {
                beatmap.StoryboardSoundSamples.Clear();
                foreach (var h in hitsounds.Where(h => !string.IsNullOrEmpty(h.Filename))) {
                    beatmap.StoryboardSoundSamples.Add(new StoryboardSoundSample((int)Math.Round(h.Time), 0, h.Filename, h.Volume * 100));
                }
            } else {
                // Make new timing points
                // Add red lines
                var redlines = beatmap.BeatmapTiming.Redlines;
                List<TimingPointsChange> timingPointsChanges = redlines.Select(tp =>
                        new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true))
                    .ToList();

                // Add hitsound stuff
                // Replace all hitobjects with the hitsounds
                beatmap.HitObjects.Clear();
                foreach (IHitsoundEvent h in hitsounds) {
                    var index = h.CustomIndex;
                    var volume = h.Volume;

                    if (UseGreenlines) {
                        TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5).Copy();
                        tp.Offset = h.Time;
                        tp.SampleIndex = h.CustomIndex;
                        index = 0; // Set it to default value because it gets handled by greenlines now
                        tp.Volume = Math.Round(tp.Volume * h.Volume);
                        volume = 0; // Set it to default value because it gets handled by greenlines now
                        timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
                    }

                    beatmap.HitObjects.Add(new HitObject(h.Pos, h.Time, 5, h.GetHitsounds(), h.SampleSet, h.Additions,
                        index, volume * 100, h.Filename));
                }

                // Replace the old timingpoints
                beatmap.BeatmapTiming.Clear();
                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);
            }

            // Change version to hitsounds
            beatmap.General["StackLeniency"] = new TValue("0.0");
            beatmap.General["Mode"] = new TValue(((int)ExportGameMode).ToInvariant());
            beatmap.Metadata["Version"] = new TValue(BeatmapVersionName);

            if (ExportGameMode == GameMode.Mania) {
                // Count the number of distinct X positions
                int numXPositions = hitsounds.Select(h => h.Pos.X).Distinct().Count();
                int numKeys = MathHelper.Clamp(numXPositions, 1, 18);

                beatmap.Difficulty["CircleSize"] = new TValue(numKeys.ToInvariant());
            } else {
                beatmap.Difficulty["CircleSize"] = new TValue("4");
            }
        }
    }
}