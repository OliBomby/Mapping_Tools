using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.ViewSettings;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class PropertyTransformerView : UserControl {
        public PropertyTransformerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            try {
                bool clip = (bool)ClipBox.IsChecked;
                double tpom = TPOffsetMultiplierBox.GetDouble(defaultValue: 1);
                double tpoo = TPOffsetOffsetBox.GetDouble(defaultValue: 0);
                double tpbpmm = TPBPMMultiplierBox.GetDouble(defaultValue: 1);
                double tpbpmo = TPBPMOffsetBox.GetDouble(defaultValue: 0);
                double tpsvm = TPSVMultiplierBox.GetDouble(defaultValue: 1);
                double tpsvo = TPSVOffsetBox.GetDouble(defaultValue: 0);
                double tpim = TPIndexMultiplierBox.GetDouble(defaultValue: 1);
                double tpio = TPIndexOffsetBox.GetDouble(defaultValue: 0);
                double tpvm = TPVolumeMultiplierBox.GetDouble(defaultValue: 1);
                double tpvo = TPVolumeOffsetBox.GetDouble(defaultValue: 0);
                double hotm = HOTimeMultiplierBox.GetDouble(defaultValue: 1);
                double hoto = HOTimeOffsetBox.GetDouble(defaultValue: 0);
                double btm = BookTimeMultiplierBox.GetDouble(defaultValue: 1);
                double bto = BookTimeOffsetBox.GetDouble(defaultValue: 0);

                Editor editor = new Editor(MainWindow.AppWindow.GetCurrentMap());
                Beatmap beatmap = editor.Beatmap;

                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints) {
                    // Offset
                    if (tpom != 1 || tpoo != 0) {
                        tp.Offset = Math.Round(tp.Offset * tpom + tpoo);
                    }

                    // BPM
                    if (tpbpmm != 1 || tpbpmo != 0) {
                        if (tp.Inherited) {
                            double newBPM = tp.GetBPM() * tpbpmm + tpbpmo;
                            newBPM = clip ? MathHelper.Clamp(newBPM, 15, 10000) : newBPM;  // Clip the value if specified
                            tp.MpB = 60000 / newBPM;
                        }
                    }

                    // Slider Velocity
                    if (tpsvm != 1 || tpsvo != 0) {
                        TimingPoint tpchanger = tp.Copy();
                        double newSV = beatmap.BeatmapTiming.GetSVMultiplierAtTime(tp.Offset) * tpsvm + tpsvo;
                        newSV = clip ? MathHelper.Clamp(newSV, 0.1, 10) : newSV;  // Clip the value if specified
                        tpchanger.MpB = -100 / newSV;
                        timingPointsChanges.Add(new TimingPointsChange(tpchanger, mpb: true));
                    }

                    // Index
                    if (tpim != 1 || tpio != 0) {
                        int newIndex = (int)Math.Round(tp.SampleIndex * tpim + tpio);
                        tp.SampleIndex = clip ? MathHelper.Clamp(newIndex, 0, 99) : newIndex;
                    }

                    // Volume
                    if (tpvm != 1 || tpvo != 0) {
                        int newVolume = (int)Math.Round(tp.Volume * tpvm + tpvo);
                        tp.Volume = clip ? MathHelper.Clamp(newVolume, 5, 100) : newVolume;
                    }
                }

                // Hitobject Time
                if (hotm != 1 || hoto != 0) {
                    foreach (HitObject ho in beatmap.HitObjects) {
                        ho.Time = Math.Round(ho.Time * hotm + hoto);
                    }
                }
                
                // Bookmark Time
                if (btm != 1 || bto != 0) {
                    List<double> newBookmarks = new List<double>();
                    List<double> bookmarks = beatmap.GetBookmarks();
                    foreach (double bookmark in bookmarks) {
                        newBookmarks.Add(Math.Round(bookmark * btm + bto));
                    }
                    beatmap.SetBookmarks(newBookmarks);
                }

                foreach (TimingPointsChange c in timingPointsChanges) {
                    c.AddChange(beatmap.BeatmapTiming.TimingPoints);
                }

                // Save the file
                editor.SaveFile();

                MessageBox.Show("Done!");
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
