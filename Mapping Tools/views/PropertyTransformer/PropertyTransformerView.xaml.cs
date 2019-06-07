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

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    public partial class PropertyTransformerView : UserControl {
        public PropertyTransformerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            if ((bool) FiltersBox.IsChecked) {
                FiltersBox_Checked();
            } else {
                FiltersBox_Unchecked();
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            try {
                // Backup
                string fileToCopy = MainWindow.AppWindow.currentMap.Text;
                IOHelper.SaveMapBackup(fileToCopy);

                bool clip = (bool)ClipBox.IsChecked;
                bool doFilter = (bool)FiltersBox.IsChecked;
                double match = MatchBox.GetDouble(defaultValue: -1);
                bool doFilterMatch = match != -1 && doFilter;
                double min = MinBox.GetDouble(defaultValue: double.MinValue);
                double max = MaxBox.GetDouble(defaultValue: double.MaxValue);
                bool doFilterRange = (min != double.NegativeInfinity || max != double.PositiveInfinity) && doFilter;

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
                        if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(tp.Offset, match, 0.01)) && (!doFilterRange || (tp.Offset >= min && tp.Offset <= max)))) {
                            tp.Offset = Math.Round(tp.Offset * tpom + tpoo);
                        }
                    }

                    // BPM
                    if (tpbpmm != 1 || tpbpmo != 0) {
                        if (tp.Inherited) {
                            if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(tp.GetBPM(), match, 0.01)) && (!doFilterRange || (tp.Offset >= min && tp.Offset <= max)))) {
                                double newBPM = tp.GetBPM() * tpbpmm + tpbpmo;
                                newBPM = clip ? MathHelper.Clamp(newBPM, 15, 10000) : newBPM;  // Clip the value if specified
                                tp.MpB = 60000 / newBPM;
                            }
                        }
                    }

                    // Slider Velocity
                    if (tpsvm != 1 || tpsvo != 0) {
                        if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(beatmap.BeatmapTiming.GetSVMultiplierAtTime(tp.Offset), match, 0.01)) && (!doFilterRange || (tp.Offset >= min && tp.Offset <= max)))) {
                            TimingPoint tpchanger = tp.Copy();
                            double newSV = beatmap.BeatmapTiming.GetSVMultiplierAtTime(tp.Offset) * tpsvm + tpsvo;
                            newSV = clip ? MathHelper.Clamp(newSV, 0.1, 10) : newSV;  // Clip the value if specified
                            tpchanger.MpB = -100 / newSV;
                            timingPointsChanges.Add(new TimingPointsChange(tpchanger, mpb: true));
                        }
                    }

                    // Index
                    if (tpim != 1 || tpio != 0) {
                        if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(tp.SampleIndex, match, 0.01)) && (!doFilterRange || (tp.Offset >= min && tp.Offset <= max)))) {
                            int newIndex = (int)Math.Round(tp.SampleIndex * tpim + tpio);
                            tp.SampleIndex = clip ? MathHelper.Clamp(newIndex, 0, 100) : newIndex;
                        }
                    }

                    // Volume
                    if (tpvm != 1 || tpvo != 0) {
                        if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(tp.Volume, match, 0.01)) && (!doFilterRange || (tp.Offset >= min && tp.Offset <= max)))) {
                            int newVolume = (int)Math.Round(tp.Volume * tpvm + tpvo);
                            tp.Volume = clip ? MathHelper.Clamp(newVolume, 5, 100) : newVolume;
                        }
                    }
                }

                // Hitobject Time
                if (hotm != 1 || hoto != 0) {
                    foreach (HitObject ho in beatmap.HitObjects) {
                        if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(ho.Time, match, 0.01)) && (!doFilterRange || (ho.Time >= min && ho.Time <= max)))) {
                            ho.Time = Math.Round(ho.Time * hotm + hoto);
                        }
                    }
                }
                
                // Bookmark Time
                if (btm != 1 || bto != 0) {
                    List<double> newBookmarks = new List<double>();
                    List<double> bookmarks = beatmap.GetBookmarks();
                    foreach (double bookmark in bookmarks) {
                        if (!doFilter || ((!doFilterMatch || Precision.AlmostEquals(bookmark, match, 0.01)) && (!doFilterRange || (bookmark >= min && bookmark <= max)))) {
                            newBookmarks.Add(Math.Round(bookmark * btm + bto));
                        } else {
                            newBookmarks.Add(bookmark);
                        }
                    }
                    beatmap.SetBookmarks(newBookmarks);
                }

                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

                // Save the file
                editor.SaveFile();

                MessageBox.Show("Done!");
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void FiltersBox_Checked(object sender=null, RoutedEventArgs e=null) {
            MatchBox.Visibility = Visibility.Visible;
            RangePanel.Visibility = Visibility.Visible;
        }

        private void FiltersBox_Unchecked(object sender=null, RoutedEventArgs e=null) {
            MatchBox.Visibility = Visibility.Collapsed;
            RangePanel.Visibility = Visibility.Collapsed;
        }
    }
}
