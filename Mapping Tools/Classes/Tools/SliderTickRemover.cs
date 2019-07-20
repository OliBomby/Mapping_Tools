using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.Tools
{
    class SliderTickRemover
    {
        public struct Arguments
        {
            public bool VolumeSliders;
            public bool SamplesetSliders;
            public bool VolumeSpinners;
            public bool RemoveMuting;
            public bool ResnapObjects;
            public bool ResnapBookmarks;
            public int Snap1;
            public int Snap2;
            public bool RemoveUnclickableHitsounds;

            public Arguments(bool volumeSliders, bool samplesetSliders, bool volumeSpinners, bool removeMuting, bool resnapObjects, bool resnapBookmarks,
                             int snap1, int snap2, bool removeUnclickableHitsounds) {
                VolumeSliders = volumeSliders;
                SamplesetSliders = samplesetSliders;
                VolumeSpinners = volumeSpinners;
                RemoveMuting = removeMuting;
                ResnapObjects = resnapObjects;
                ResnapBookmarks = resnapBookmarks;
                Snap1 = snap1;
                Snap2 = snap2;
                RemoveUnclickableHitsounds = removeUnclickableHitsounds;
            }

            public static readonly Arguments BasicResnap = new Arguments(true, true, true, false, true, false, 16, 12, false);
        }

        public static int CleanMap(Beatmap beatmap, Arguments arguments, BackgroundWorker worker = null) {
            UpdateProgressBar(worker, 0);

            Timing timing = beatmap.BeatmapTiming;

            int sliderFixed = 0;

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();


            // Add Hitobject stuff
            foreach (HitObject ho in beatmap.HitObjects) {
                if (ho.IsSlider) // SV changes
                {
                    double sv = -100 / ho.SV;

                    TimingPoint tp = ho.TP.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = double.NaN;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));

                    TimingPoint tpr = ho.TP.Copy();
                    tpr.Offset = ho.Time;
                    tpr.MpB = ho.Redline.MpB / sv;
                    tpr.Inherited = true;
                    timingPointsChanges.Add(new TimingPointsChange(tpr, mpb: true, inherited:true));

                    sliderFixed++;
                }
            }
            UpdateProgressBar(worker, 81);

            

            // Replace the old timingpoints
            TimingPointsChange.ApplyChanges(timing, timingPointsChanges);
            beatmap.GiveObjectsGreenlines();

            // Complete progressbar
            UpdateProgressBar(worker, 100);

            return sliderFixed;
        }

        private static void UpdateProgressBar(BackgroundWorker worker, int progress) {
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(progress);
            }
        }
    }
}
