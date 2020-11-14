using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using NAudio.Wave;

namespace Mapping_Tools.Views.HitsoundStudio
{

    /// <summary>
    /// Interactielogica voor HitsoundStudioView.xaml
    /// </summary>
    public partial class HitsoundStudioView : ISavable<HitsoundStudioVm>, IHaveExtraProjectMenuItems
    {
        private HitsoundStudioVm Settings;

        private bool suppressEvents;

        private List<HitsoundLayer> selectedLayers;
        private HitsoundLayer selectedLayer;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "hsstudioproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Hitsound Studio Projects");

        public static readonly string ToolName = "Hitsound Studio";

        public static readonly string ToolDescription = $@"Hitsound Studio is the tool that lets you import data from multiple outside sources and convert them to osu! standard hitsounds in the form of a hitsounding difficulty that can you copy to other beatmaps.{Environment.NewLine}It represents hitsounds as a list of layers (hitsound layers). One layer contains a unique sound, the sampleset and hitsound that accompany that sound and a list of times that sound has to be played.";

        public HitsoundStudioView()
        {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            Settings = new HitsoundStudioVm();
            DataContext = Settings;
            LayersList.SelectedIndex = 0;
            Num_Layers_Changed();
            GetSelectedLayers();
            ProjectManager.LoadProject(this, message: false);

            // This tool is verbose because of the 'show results' option
            Verbose = true;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = Make_Hitsounds((HitsoundStudioVm)e.Argument, bgw, e);
        }

        private string Make_Hitsounds(HitsoundStudioVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            string result = string.Empty;

            bool validateSampleFile =
                !(arg.SingleSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords ||
                  arg.MixedSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords);

            var comparer = new SampleGeneratingArgsComparer(validateSampleFile);

            if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Standard) {
                // Convert the multiple layers into packages that have the samples from all the layers at one specific time
                // Don't add default sample when exporting midi files because that's not a final export.
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, arg.ZipLayersLeniency, validateSampleFile);
                UpdateProgressBar(worker, 10);

                // Balance the volume between greenlines and samples
                HitsoundConverter.BalanceVolumes(samplePackages, 0, false);
                UpdateProgressBar(worker, 20);

                // Load the samples so validation can be done
                HashSet<SampleGeneratingArgs> allSampleArgs = new HashSet<SampleGeneratingArgs>(comparer);
                foreach (SamplePackage sp in samplePackages) {
                    allSampleArgs.UnionWith(sp.Samples.Select(o => o.SampleArgs));
                }

                var loadedSamples = SampleImporter.ImportSamples(allSampleArgs, comparer);
                UpdateProgressBar(worker, 30);

                // Convert the packages to hitsounds that fit on an osu standard map
                CompleteHitsounds completeHitsounds =
                    HitsoundConverter.GetCompleteHitsounds(samplePackages, loadedSamples, 
                        arg.UsePreviousSampleSchema ? arg.PreviousSampleSchema.GetCustomIndices() : null, 
                        arg.AllowGrowthPreviousSampleSchema, arg.FirstCustomIndex, validateSampleFile, comparer);
                UpdateProgressBar(worker, 60);

                // Save current sample schema
                if (!arg.UsePreviousSampleSchema) {
                    arg.PreviousSampleSchema = new SampleSchema(completeHitsounds.CustomIndices);
                } else if (arg.AllowGrowthPreviousSampleSchema) {
                    arg.PreviousSampleSchema.MergeWith(new SampleSchema(completeHitsounds.CustomIndices));
                }

                if (arg.ShowResults) {
                    // Count the number of samples
                    int samples = completeHitsounds.CustomIndices.SelectMany(ci => ci.Samples.Values)
                        .Count(h => h.Any(o => 
                            SampleImporter.ValidateSampleArgs(o, loadedSamples, validateSampleFile)));

                    // Count the number of changes of custom index
                    int greenlines = 0;
                    int lastIndex = -1;
                    foreach (var hit in completeHitsounds.Hitsounds.Where(hit => hit.CustomIndex != lastIndex)) {
                        lastIndex = hit.CustomIndex;
                        greenlines++;
                    }

                    result = $"Number of sample indices: {completeHitsounds.CustomIndices.Count}, " +
                             $"Number of samples: {samples}, Number of greenlines: {greenlines}";
                }

                if (arg.DeleteAllInExportFirst && (arg.ExportSamples || arg.ExportMap)) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }

                UpdateProgressBar(worker, 70);

                // Export the hitsound map and sound samples
                if (arg.ExportMap) {
                    HitsoundExporter.ExportHitsounds(completeHitsounds.Hitsounds, 
                        arg.BaseBeatmap, arg.ExportFolder, arg.HitsoundDiffName, arg.HitsoundExportGameMode, true, false);
                }

                UpdateProgressBar(worker, 80);

                if (arg.ExportSamples) {
                    HitsoundExporter.ExportCustomIndices(completeHitsounds.CustomIndices, arg.ExportFolder,
                        loadedSamples, arg.SingleSampleExportFormat, arg.MixedSampleExportFormat, comparer);
                }

                UpdateProgressBar(worker, 99);
            } else if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Coinciding) {
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, 0, false);

                HitsoundConverter.BalanceVolumes(samplePackages, 0, false, true);
                UpdateProgressBar(worker, 20);

                Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null;
                Dictionary<SampleGeneratingArgs, string> sampleNames = arg.UsePreviousSampleSchema ? arg.PreviousSampleSchema?.GetSampleNames(comparer) : null;
                Dictionary<SampleGeneratingArgs, Vector2> samplePositions = null;
                var hitsounds = HitsoundConverter.GetHitsounds(samplePackages, ref loadedSamples, ref sampleNames, ref samplePositions,
                    arg.HitsoundExportGameMode == GameMode.Mania, arg.AddCoincidingRegularHitsounds, arg.AllowGrowthPreviousSampleSchema,
                    validateSampleFile, comparer);

                // Save current sample schema
                if (!arg.UsePreviousSampleSchema || arg.PreviousSampleSchema == null) {
                    arg.PreviousSampleSchema = new SampleSchema(sampleNames);
                } else if (arg.AllowGrowthPreviousSampleSchema) {
                    arg.PreviousSampleSchema.MergeWith(new SampleSchema(sampleNames));
                }

                // Load the samples so validation can be done
                UpdateProgressBar(worker, 50);

                if (arg.ShowResults) {
                    result = "Number of sample indices: 0, " +
                             $"Number of samples: {loadedSamples.Count}, Number of greenlines: 0";
                }

                if (arg.DeleteAllInExportFirst && (arg.ExportSamples || arg.ExportMap)) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }
                UpdateProgressBar(worker, 60);

                if (arg.ExportMap) {
                    HitsoundExporter.ExportHitsounds(hitsounds, 
                        arg.BaseBeatmap, arg.ExportFolder, arg.HitsoundDiffName, arg.HitsoundExportGameMode, false, false);
                }
                UpdateProgressBar(worker, 70);

                if (arg.ExportSamples) {
                    HitsoundExporter.ExportLoadedSamples(loadedSamples, arg.ExportFolder, sampleNames, arg.SingleSampleExportFormat, comparer);
                }
            } else if (arg.HitsoundExportModeSetting == HitsoundStudioVm.HitsoundExportMode.Storyboard) {
                List<SamplePackage> samplePackages = HitsoundConverter.ZipLayers(arg.HitsoundLayers, arg.DefaultSample, 0, false);

                HitsoundConverter.BalanceVolumes(samplePackages, 0, false, true);
                UpdateProgressBar(worker, 20);

                Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null;
                Dictionary<SampleGeneratingArgs, string> sampleNames = arg.UsePreviousSampleSchema ? arg.PreviousSampleSchema?.GetSampleNames(comparer) : null;
                Dictionary<SampleGeneratingArgs, Vector2> samplePositions = null;
                var hitsounds = HitsoundConverter.GetHitsounds(samplePackages, ref loadedSamples, ref sampleNames, ref samplePositions,
                    false, false, arg.AllowGrowthPreviousSampleSchema, validateSampleFile, comparer);

                // Save current sample schema
                if (!arg.UsePreviousSampleSchema || arg.PreviousSampleSchema == null) {
                    arg.PreviousSampleSchema = new SampleSchema(sampleNames);
                } else if (arg.AllowGrowthPreviousSampleSchema) {
                    arg.PreviousSampleSchema.MergeWith(new SampleSchema(sampleNames));
                }

                // Load the samples so validation can be done
                UpdateProgressBar(worker, 50);

                if (arg.ShowResults) {
                    result = "Number of sample indices: 0, " +
                             $"Number of samples: {loadedSamples.Count}, Number of greenlines: 0";
                }

                if (arg.DeleteAllInExportFirst && (arg.ExportSamples || arg.ExportMap)) {
                    // Delete all files in the export folder before filling it again
                    DirectoryInfo di = new DirectoryInfo(arg.ExportFolder);
                    foreach (FileInfo file in di.GetFiles()) {
                        file.Delete();
                    }
                }
                UpdateProgressBar(worker, 60);

                if (arg.ExportMap) {
                    HitsoundExporter.ExportHitsounds(hitsounds, 
                        arg.BaseBeatmap, arg.ExportFolder, arg.HitsoundDiffName, arg.HitsoundExportGameMode, false, true);
                }
                UpdateProgressBar(worker, 70);

                if (arg.ExportSamples) {
                    HitsoundExporter.ExportLoadedSamples(loadedSamples, arg.ExportFolder, sampleNames, arg.SingleSampleExportFormat, comparer);
                }
            }

            // Open export folder
            if (arg.ExportSamples || arg.ExportMap) {
                System.Diagnostics.Process.Start(arg.ExportFolder);
            }

            // Collect garbage
            GC.Collect();

            UpdateProgressBar(worker, 100);

            return result;
        }

        private async void Start_Click(object sender, RoutedEventArgs e) {
            var dialog = new HitsoundStudioExportDialog(Settings);
            var result = await DialogHost.Show(dialog, "RootDialog");

            if (!(bool) result) return;

            if (Settings.BaseBeatmap == null || Settings.DefaultSample == null)
            {
                MessageBox.Show("Please select a base beatmap and default hitsound first.");
                return;
            }
            
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackgroundWorker.RunWorkerAsync(Settings);
            CanRun = false;
        }

        private void GetSelectedLayers()
        {
            selectedLayers = new List<HitsoundLayer>();

            if (LayersList.SelectedItems.Count == 0)
            {
                selectedLayer = null;
                return;
            }

            foreach (HitsoundLayer hsl in LayersList.SelectedItems)
            {
                selectedLayers.Add(hsl);
            }

            selectedLayer = selectedLayers[0];
        }

        private void SelectedSamplePathBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.SampleFileDialog();
                if (path != "")
                {
                    SelectedSamplePathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void SelectedImportSamplePathBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.SampleFileDialog();
                if (path != "")
                {
                    SelectedImportSamplePathBox.Text = path;
                    SelectedStoryboardImportSamplePathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void SelectedImportPathBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.FileDialog();
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedImportPathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void SelectedImportPathLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.GetCurrentBeatmap();
                if (path != "")
                {
                    SelectedImportPathBox.Text = path;
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void DefaultSampleBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.SampleFileDialog();
                if (path != "")
                {
                    Settings.DefaultSample.SampleArgs.Path = path;
                    DefaultSamplePathBox.Text = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void BaseBeatmapBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                if (paths.Length != 0)
                {
                    Settings.BaseBeatmap = paths[0];
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void BaseBeatmapLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = IOHelper.GetCurrentBeatmap();
                if (path != "")
                {
                    Settings.BaseBeatmap = path;
                }
            } catch (Exception ex) { ex.Show(); }
        }

        private void ReloadFromSource_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var seperatedByImportArgsForReloading = new Dictionary<ImportReloadingArgs, List<HitsoundLayer>>(new ImportReloadingArgsComparer());

                foreach (var layer in selectedLayers)
                {
                    var reloadingArgs = layer.ImportArgs.GetImportReloadingArgs();
                    if (seperatedByImportArgsForReloading.TryGetValue(reloadingArgs, out List<HitsoundLayer> value))
                    {
                        value.Add(layer);
                    }
                    else
                    {
                        seperatedByImportArgsForReloading.Add(reloadingArgs, new List<HitsoundLayer> { layer });
                    }
                }

                foreach (var pair in seperatedByImportArgsForReloading)
                {
                    var reloadingArgs = pair.Key;
                    var layers = pair.Value;

                    var importedLayers = HitsoundImporter.ImportReloading(reloadingArgs);

                    layers.ForEach(o => o.Reload(importedLayers));
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void LayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            GetSelectedLayers();
            UpdateEditingField();
        }

        private void UpdateEditingField()
        {
            if (selectedLayers.Count == 0) { return; }

            suppressEvents = true;

            // Populate the editing fields
            SelectedNameBox.Text = selectedLayers.AllToStringOrDefault(o => o.Name);
            SelectedSampleSetBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleSetString);
            SelectedHitsoundBox.Text = selectedLayers.AllToStringOrDefault(o => o.HitsoundString);
            TimesBox.Text = selectedLayers.AllToStringOrDefault(o => o.Times, HitsoundLayerExtension.DoubleListToStringConverter);

            SelectedSamplePathBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Path);
            SelectedSampleVolumeBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Volume * 100, CultureInfo.InvariantCulture);
            SelectedSampleBankBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Bank);
            SelectedSamplePatchBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Patch);
            SelectedSampleInstrumentBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Instrument);
            SelectedSampleKeyBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Key);
            SelectedSampleLengthBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Length, CultureInfo.InvariantCulture);
            SelectedSampleVelocityBox.Text = selectedLayers.AllToStringOrDefault(o => o.SampleArgs.Velocity);

            SelectedImportTypeBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.ImportType);
            SelectedImportPathBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Path);
            SelectedImportXCoordBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.X, CultureInfo.InvariantCulture);
            SelectedImportYCoordBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Y, CultureInfo.InvariantCulture);
            SelectedImportSamplePathBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.SamplePath);
            SelectedHitsoundImportDiscriminateVolumesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.DiscriminateVolumes);
            SelectedHitsoundImportDetectDuplicateSamplesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.DetectDuplicateSamples);
            SelectedHitsoundImportRemoveDuplicatesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.RemoveDuplicates);
            SelectedStoryboardImportSamplePathBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.SamplePath);
            SelectedStoryboardImportDiscriminateVolumesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.DiscriminateVolumes);
            SelectedStoryboardImportRemoveDuplicatesBox.IsChecked = selectedLayers.All(o => o.ImportArgs.RemoveDuplicates);
            SelectedImportBankBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Bank);
            SelectedImportPatchBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Patch);
            SelectedImportKeyBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Key);
            SelectedImportLengthBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Length, CultureInfo.InvariantCulture);
            SelectedImportLengthRoughnessBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.LengthRoughness, CultureInfo.InvariantCulture);
            SelectedImportVelocityBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.Velocity);
            SelectedImportVelocityRoughnessBox.Text = selectedLayers.AllToStringOrDefault(o => o.ImportArgs.VelocityRoughness, CultureInfo.InvariantCulture);

            // Update visibility
            SoundFontArgsPanel.Visibility = selectedLayers.Any(o => o.SampleArgs.UsesSoundFont || string.IsNullOrEmpty(o.SampleArgs.GetExtension())) ? Visibility.Visible : Visibility.Collapsed;
            SelectedStackPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.Stack) ? Visibility.Visible : Visibility.Collapsed;
            SelectedHitsoundsPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.Hitsounds) ? Visibility.Visible : Visibility.Collapsed;
            SelectedStoryboardPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.Storyboard) ? Visibility.Visible : Visibility.Collapsed;
            SelectedMIDIPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.ImportType == ImportType.MIDI) ? Visibility.Visible : Visibility.Collapsed;
            ImportArgsPanel.Visibility = selectedLayers.Any(o => o.ImportArgs.CanImport) ? Visibility.Visible : Visibility.Collapsed;

            suppressEvents = false;
        }

        private void HitsoundLayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SampleGeneratingArgs args = selectedLayer.SampleArgs;
                var mainOutputStream = SampleImporter.ImportSample(args);

                if (mainOutputStream == null)
                {
                    MessageBox.Show("Could not load the specified sample.");
                    return;
                }

                WaveOutEvent player = new WaveOutEvent();

                player.Init(mainOutputStream.GetSampleProvider());
                player.PlaybackStopped += PlayerStopped;

                player.Play();
            }
            catch (FileNotFoundException) { MessageBox.Show("Could not find the specified sample."); }
            catch (DirectoryNotFoundException) { MessageBox.Show("Could not find the specified sample's directory."); }
            catch (Exception ex) { ex.Show(); }
        }

        private static void PlayerStopped(object sender, StoppedEventArgs e)
        {
            ((WaveOutEvent)sender).Dispose();
            GC.Collect();
        }

        private void Num_Layers_Changed()
        {
            if (Settings.HitsoundLayers.Count == 0)
            {
                FirstGrid.ColumnDefinitions[0].Width = new GridLength(0);
                EditPanel.IsEnabled = false;
            }
            else if (FirstGrid.ColumnDefinitions[0].Width.Value < 100)
            {
                FirstGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                FirstGrid.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
                EditPanel.IsEnabled = true;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HitsoundLayerImportWindow importWindow = new HitsoundLayerImportWindow(Settings.HitsoundLayers.Count);
                importWindow.ShowDialog();

                LayersList.SelectedItems.Clear();
                foreach (HitsoundLayer layer in importWindow.HitsoundLayers)
                {
                    if (layer != null)
                    {
                        Settings.HitsoundLayers.Add(layer);
                        LayersList.SelectedItems.Add(layer);
                    }
                }

                RecalculatePriorities();
                Num_Layers_Changed();
                GetSelectedLayers();
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ask for confirmation
                MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure?", "Confirm deletion", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) { return; }

                if (selectedLayers.Count == 0 || selectedLayers == null) { return; }

                suppressEvents = true;

                int index = Settings.HitsoundLayers.IndexOf(selectedLayer);

                foreach (HitsoundLayer hsl in selectedLayers)
                {
                    Settings.HitsoundLayers.Remove(hsl);
                }
                suppressEvents = false;

                LayersList.SelectedIndex = Math.Max(Math.Min(index - 1, Settings.HitsoundLayers.Count - 1), 0);

                RecalculatePriorities();
                Num_Layers_Changed();
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void Raise_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int repeats = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                for (int n = 0; n < repeats; n++)
                {
                    suppressEvents = true;

                    int selectedIndex = Settings.HitsoundLayers.IndexOf(selectedLayer);
                    List<HitsoundLayer> moveList = new List<HitsoundLayer>();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        moveList.Add(hsl);
                    }

                    foreach (HitsoundLayer hsl in Settings.HitsoundLayers)
                    {
                        if (moveList.Contains(hsl))
                        {
                            moveList.Remove(hsl);
                        }
                        else
                            break;
                    }

                    foreach (HitsoundLayer hsl in moveList)
                    {
                        int index = Settings.HitsoundLayers.IndexOf(hsl);

                        //Dont move left if it is the first item in the list or it is not in the list
                        if (index <= 0)
                            continue;

                        //Swap with this item with the one to its left
                        Settings.HitsoundLayers.Remove(hsl);
                        Settings.HitsoundLayers.Insert(index - 1, hsl);
                    }

                    LayersList.SelectedItems.Clear();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        LayersList.SelectedItems.Add(hsl);
                    }

                    suppressEvents = false;

                    RecalculatePriorities();
                    GetSelectedLayers();
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void Lower_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int repeats = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 10 : 1;
                for (int n = 0; n < repeats; n++)
                {
                    suppressEvents = true;

                    int selectedIndex = Settings.HitsoundLayers.IndexOf(selectedLayer);
                    List<HitsoundLayer> moveList = new List<HitsoundLayer>();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        moveList.Add(hsl);
                    }

                    for (int i = Settings.HitsoundLayers.Count - 1; i >= 0; i--)
                    {
                        HitsoundLayer hsl = Settings.HitsoundLayers[i];
                        if (moveList.Contains(hsl))
                        {
                            moveList.Remove(hsl);
                        }
                        else
                            break;
                    }

                    for (int i = moveList.Count - 1; i >= 0; i--)
                    {
                        HitsoundLayer hsl = moveList[i];
                        int index = Settings.HitsoundLayers.IndexOf(hsl);

                        //Dont move left if it is the first item in the list or it is not in the list
                        if (index >= Settings.HitsoundLayers.Count - 1)
                            continue;

                        //Swap with this item with the one to its left
                        Settings.HitsoundLayers.Remove(hsl);
                        Settings.HitsoundLayers.Insert(index + 1, hsl);
                    }

                    LayersList.SelectedItems.Clear();
                    foreach (HitsoundLayer hsl in selectedLayers)
                    {
                        LayersList.SelectedItems.Add(hsl);
                    }

                    suppressEvents = false;

                    RecalculatePriorities();
                    GetSelectedLayers();
                }
            }
            catch (Exception ex) { ex.Show(); }
        }

        private void RecalculatePriorities()
        {
            for (int i = 0; i < Settings.HitsoundLayers.Count; i++)
            {
                Settings.HitsoundLayers[i].Priority = i;
            }
        }

        #region HitsoundLayerChangeEventHandlers

        private void SelectedNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.Name = t;
            }
        }

        private void SelectedSampleSetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleSetString = t;
            }
        }

        private void SelectedHitsoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.HitsoundString = t;
            }
        }

        private void TimesBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;
            if ((sender as TextBox).GetBindingExpression(TextBox.TextProperty).HasValidationError) return;

            try
            {
                List<double> t = (sender as TextBox).Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).OrderBy(o => o).ToList();

                foreach (HitsoundLayer hitsoundLayer in selectedLayers)
                {
                    hitsoundLayer.Times = t;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
        }

        private void SelectedSamplePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Path = t;
            }
            UpdateEditingField();
        }

        private void SelectedSampleVolumeBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(100);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Volume = t / 100;
            }
            UpdateEditingField();
        }

        private void SelectedSampleBankBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Bank = t;
            }
        }

        private void SelectedSamplePatchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Patch = t;
            }
        }

        private void SelectedSampleInstrumentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Instrument = t;
            }
        }

        private void SelectedSampleKeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Key = t;
            }
        }

        private void SelectedSampleLengthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Length = t;
            }
        }

        private void SelectedSampleVelocityBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(127);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.SampleArgs.Velocity = t;
            }
            UpdateEditingField();
        }

        private void SelectedImportTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content.ToString();
            ImportType type = (ImportType)Enum.Parse(typeof(ImportType), t);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.ImportType = type;
            }
            UpdateEditingField();
        }

        private void SelectedImportPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Path = t;
            }
        }

        private void SelectedImportXCoordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.X = t;
            }
        }

        private void SelectedImportYCoordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Y = t;
            }
        }

        private void SelectedImportSamplePathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            string t = (sender as TextBox).Text;
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.SamplePath = t;
            }
        }

        private void SelectedImportBankBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Bank = t;
            }
        }

        private void SelectedImportPatchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Patch = t;
            }
        }

        private void SelectedImportKeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Key = t;
            }
        }

        private void SelectedImportLengthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Length = t;
            }
        }

        private void SelectedImportVelocityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            int t = (sender as TextBox).GetInt(-1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.Velocity = t;
            }
        }

        private void SelectedImportLengthRoughnessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.LengthRoughness = t;
            }
        }

        private void SelectedImportVelocityRoughnessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (suppressEvents) return;

            double t = (sender as TextBox).GetDouble(1);
            foreach (HitsoundLayer hitsoundLayer in selectedLayers)
            {
                hitsoundLayer.ImportArgs.VelocityRoughness = t;
            }
        }

        private void SelectedImportDiscriminateVolumesBox_OnChecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DiscriminateVolumes = true;
            }
        }

        private void SelectedImportDiscriminateVolumesBox_OnUnchecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DiscriminateVolumes = false;
            }
        }

        private void SelectedImportRemoveDuplicatesBox_OnChecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.RemoveDuplicates = true;
            }
        }

        private void SelectedImportRemoveDuplicatesBox_OnUnchecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.RemoveDuplicates = false;
            }
        }

        private void SelectedHitsoundImportDetectDuplicateSamplesBox_OnChecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DetectDuplicateSamples = true;
            }
        }

        private void SelectedHitsoundImportDetectDuplicateSamplesBox_OnUnchecked(object sender, RoutedEventArgs e) {
            if (suppressEvents) return;

            foreach (HitsoundLayer hitsoundLayer in selectedLayers) {
                hitsoundLayer.ImportArgs.DetectDuplicateSamples = false;
            }
        }

        #endregion

        public HitsoundStudioVm GetSaveData()
        {
            return Settings;
        }

        public void SetSaveData(HitsoundStudioVm saveData)
        {
            suppressEvents = true;

            Settings = saveData;
            DataContext = Settings;

            suppressEvents = false;

            LayersList.SelectedIndex = 0;
            Num_Layers_Changed();
        }

        #region IHaveExtraMenuItems members

        public MenuItem[] GetMenuItems() {
            var menu = new MenuItem {
                Header = "_Load sample schema", Icon = new PackIcon {Kind = PackIconKind.FileMusic},
                ToolTip = "Load sample schema from a project file."
            };
            menu.Click += LoadSampleSchemaFromFile;

            return new[] {menu};
        }

        private void LoadSampleSchemaFromFile(object sender, RoutedEventArgs e) {
            try {
                var project = ProjectManager.GetProject(this, true);
                Settings.PreviousSampleSchema = project.PreviousSampleSchema;

                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully loaded sample schema!"));
            } catch (ArgumentException) { }
            catch (Exception ex) {
                ex.Show();
            }
        }

        #endregion
    }
}