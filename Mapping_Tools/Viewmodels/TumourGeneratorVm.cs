using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Classes.Tools.TumourGenerating;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class TumourGeneratorVm : BindableBase {
        #region Properties

        private CancellationTokenSource previewTokenSource;
        private readonly object previewTokenLock = new();

        private bool isProcessingPreview;
        [JsonIgnore]
        public bool IsProcessingPreview {
            get => isProcessingPreview;
            set => Set(ref isProcessingPreview, value);
        }
        
        private HitObject previewHitObject;
        public HitObject PreviewHitObject {
            get => previewHitObject;
            set => Set(ref previewHitObject, value, action: RegeneratePreview);
        }

        private HitObject tumouredPreviewHitObject;
        [JsonIgnore]
        public HitObject TumouredPreviewHitObject {
            get => tumouredPreviewHitObject;
            set {
                if (Set(ref tumouredPreviewHitObject, value)) {
                    IsProcessingPreview = false;
                }
            }
        }

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

        private string timeCode;
        public string TimeCode {
            get => timeCode;
            set => Set(ref timeCode, value);
        }

        [JsonIgnore]
        public Visibility TimeCodeBoxVisibility => ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        private ObservableCollection<TumourLayer> tumourLayers;
        public ObservableCollection<TumourLayer> TumourLayers {
            get => tumourLayers;
            set {
                if (Set(ref tumourLayers, value)) {
                    RegeneratePreview();
                    foreach (TumourLayer layer in tumourLayers) {
                        layer.PropertyChanged += TumourLayerOnPropertyChanged;
                    }
                    tumourLayers.CollectionChanged += TumourLayersOnCollectionChanged;
                }
            }
        }

        private void TumourLayersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems is null) return;
            foreach (object newObj in e.NewItems) {
                if (newObj is not TumourLayer layer) continue;
                layer.PropertyChanged += TumourLayerOnPropertyChanged;
            }
        }

        private void TumourLayerOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(TumourLayer.UseAbsoluteRange)) {
                RaisePropertyChanged(nameof(TumourStartSliderMin));
                RaisePropertyChanged(nameof(TumourRangeSliderMax));
                RaisePropertyChanged(nameof(TumourRangeSliderSmallChange));
            }
            RegeneratePreview();
        }

        private TumourLayer currentLayer;
        [JsonIgnore]
        public TumourLayer CurrentLayer {
            get => currentLayer;
            set {
                if (Set(ref currentLayer, value)) {
                    if (currentLayer is not null) {
                        currentLayer.PropertyChanged += TumourLayerOnPropertyChanged;
                    }
                    RaisePropertyChanged(nameof(TumourRangeSliderSmallChange));
                }
            }
        }

        private int currentLayerIndex;
        public int CurrentLayerIndex {
            get => currentLayerIndex;
            set {
                if (Set(ref currentLayerIndex, value)) {
                    RaisePropertyChanged(nameof(TumourStartSliderMin));
                    RaisePropertyChanged(nameof(TumourRangeSliderMax));
                }
            }
        }

        private bool justMiddleAnchors;
        public bool JustMiddleAnchors {
            get => justMiddleAnchors;
            set => Set(ref justMiddleAnchors, value, action: RegeneratePreview);
        }

        private bool fixSv;
        public bool FixSv {
            get => fixSv;
            set => Set(ref fixSv, value);
        }

        private bool delegateToBpm;
        public bool DelegateToBpm {
            get => delegateToBpm;
            set => Set(ref delegateToBpm, value);
        }

        private bool removeSliderTicks;
        public bool RemoveSliderTicks {
            get => removeSliderTicks;
            set => Set(ref removeSliderTicks, value);
        }

        private bool advancedOptions;
        public bool AdvancedOptions {
            get => advancedOptions;
            set {
                if (Set(ref advancedOptions, value)) {
                    RaisePropertyChanged(nameof(TumourStartSliderMin));
                }
            }
        }

        private bool debugConstruction;
        public bool DebugConstruction {
            get => debugConstruction;
            set => Set(ref debugConstruction, value, action: RegeneratePreview);
        }

        private double scale;
        public double Scale {
            get => scale;
            set => Set(ref scale, value, action: RegeneratePreview);
        }

        private double circleSize;
        public double CircleSize {
            get => circleSize;
            set => Set(ref circleSize, value, action: RegeneratePreview);
        }

        [JsonIgnore]
        public double TumourStartSliderMin => AdvancedOptions ? CurrentLayerIndex >= 0 &&
                                                                CurrentLayerIndex < TumourLayers.Count &&
                                                                CurrentLayerIndex < layerRangeSliderMaxes.Count &&
                                                                TumourLayers[CurrentLayerIndex].UseAbsoluteRange ? -layerRangeSliderMaxes[CurrentLayerIndex] : -1 : 0;

        [JsonIgnore]
        public double TumourRangeSliderMax => CurrentLayerIndex >= 0 &&
                                              CurrentLayerIndex < TumourLayers.Count &&
                                              CurrentLayerIndex < layerRangeSliderMaxes.Count &&
                                              TumourLayers[CurrentLayerIndex].UseAbsoluteRange ? layerRangeSliderMaxes[CurrentLayerIndex] : 1;

        private readonly List<double> layerRangeSliderMaxes = new();

        [JsonIgnore]
        public double TumourRangeSliderSmallChange => CurrentLayer is not null && CurrentLayer.UseAbsoluteRange ? 1 : 0.0001;

        [JsonIgnore]
        public IEnumerable<TumourTemplate> TumourTemplates => Enum.GetValues(typeof(TumourTemplate)).Cast<TumourTemplate>();

        [JsonIgnore]
        public IEnumerable<WrappingMode> WrappingModes => Enum.GetValues(typeof(WrappingMode)).Cast<WrappingMode>();

        [JsonIgnore]
        public IEnumerable<TumourSidedness> TumourSides => Enum.GetValues(typeof(TumourSidedness)).Cast<TumourSidedness>();

        [JsonIgnore]
        public string[] Paths { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        [JsonIgnore]
        public bool Reload { get; set; }

        [JsonIgnore]
        public CommandImplementation ImportCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddCommand { get; }
        [JsonIgnore]
        public CommandImplementation CopyCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveCommand { get; }
        [JsonIgnore]
        public CommandImplementation RaiseCommand { get; }
        [JsonIgnore]
        public CommandImplementation LowerCommand { get; }

        #endregion

        public TumourGeneratorVm() {
            PreviewHitObject = new HitObject("0,0,0,2,0,L|256:0,1,256");
            ImportModeSetting = ImportMode.Selected;
            JustMiddleAnchors = false;
            Scale = 1;
            CircleSize = 4;
            FixSv = true;
            TumourLayers = new ObservableCollection<TumourLayer>();

            ImportCommand = new CommandImplementation(_ => Import(ImportModeSetting == ImportMode.Selected ?
                IOHelper.GetCurrentBeatmapOrCurrentBeatmap() :
                MainWindow.AppWindow.GetCurrentMaps()[0])
            );
            AddCommand = new CommandImplementation(
                _ => {
                    try {
                        var newLayer = TumourLayer.GetDefaultLayer();
                        newLayer.Name = "Layer " + (TumourLayers.Count + 1);
                        newLayer.TumourEnd = layerRangeSliderMaxes.Count > 0 ? layerRangeSliderMaxes.LastOrDefault() : PreviewHitObject.PixelLength;
                        TumourLayers.Insert(CurrentLayerIndex + 1, newLayer);
                        CurrentLayerIndex++;
                        RegeneratePreview();
                    } catch (Exception ex) { ex.Show(); }
                });
            CopyCommand = new CommandImplementation(
                _ => {
                    try {
                        var copy = TumourLayers[CurrentLayerIndex].Copy();
                        copy.Name = $"{copy.Name} (Copy)";
                        TumourLayers.Insert(CurrentLayerIndex + 1, copy);
                        CurrentLayerIndex++;
                        RegeneratePreview();
                    } catch (Exception ex) { ex.Show(); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
                        if (TumourLayers.Count > 1) {
                            var indexToRemove = CurrentLayerIndex;
                            CurrentLayerIndex = indexToRemove == 0 ? 1 : CurrentLayerIndex - 1;
                            TumourLayers.RemoveAt(indexToRemove);
                            RegeneratePreview();
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
            RaiseCommand = new CommandImplementation(
                _ => {
                    try {
                        if (CurrentLayerIndex < TumourLayers.Count - 1) {
                            TumourLayers.Move(CurrentLayerIndex, CurrentLayerIndex + 1);
                            RegeneratePreview();
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
            LowerCommand = new CommandImplementation(
                _ => {
                    try {
                        if (CurrentLayerIndex > 0) {
                            TumourLayers.Move(CurrentLayerIndex, CurrentLayerIndex - 1);
                            RegeneratePreview();
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        public void Import(string path) {
            try {
                Editor_Reader.EditorReader reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

                if (ImportModeSetting == ImportMode.Selected && editorReaderException1 != null) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException1);
                }

                BeatmapEditor editor;
                List<HitObject> markedObjects;

                switch (ImportModeSetting) {
                    case ImportMode.Selected:
                        editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                        if (editorReaderException2 != null) {
                            throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                        }

                        markedObjects = selected;
                        break;
                    case ImportMode.Bookmarked:
                        editor = new BeatmapEditor(path);
                        markedObjects = editor.Beatmap.GetBookmarkedObjects();
                        break;
                    case ImportMode.Time:
                        editor = new BeatmapEditor(path);
                        markedObjects = editor.Beatmap.QueryTimeCode(TimeCode).ToList();
                        break;
                    default:
                        editor = new BeatmapEditor(path);
                        markedObjects = editor.Beatmap.HitObjects;
                        break;
                }

                if (markedObjects is null || !markedObjects.Any(o => o.IsSlider)) {
                    Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue(@"Could not find any sliders in imported hit objects."));
                    return;
                }

                PreviewHitObject = markedObjects.First(s => s.IsSlider);
                CircleSize = editor.Beatmap.Difficulty["CircleSize"].DoubleValue;
                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue(@"Successfully imported slider."));
            } catch (Exception ex) {
                ex.Show();
            }
        }

        /// <summary>
        /// Asynchronously starts regenerating the tumoured preview slider with the current settings. <see cref="TumouredPreviewHitObject"/>
        /// At all times, only the latest slider is being tumoured.
        /// </summary>
        public void RegeneratePreview() {
            if (TumourLayers is null) return;

            // Cancel any task that might be running right now and make a new token
            CancellationToken ct;
            lock (previewTokenLock) {
                if (previewTokenSource is not null) {
                    previewTokenSource.Cancel();
                    previewTokenSource.Dispose();
                }
                previewTokenSource = new CancellationTokenSource();
                ct = previewTokenSource.Token;
            }

            // Raise property changed for the load indicator in the preview
            IsProcessingPreview = true;

            // Freeze all layers for thread safety
            foreach (var tumourLayer in TumourLayers) {
                tumourLayer.Freeze();
            }
            
            Task.Run(() => {
                var args = PreviewHitObject.DeepCopy();

                ct.ThrowIfCancellationRequested();
                // Do a lot of tumour generating
                var tumourGenerator = new TumourGenerator {
                    TumourLayers = TumourLayers,
                    JustMiddleAnchors = JustMiddleAnchors,
                    Scalar = Scale,
                    Reconstructor = new Reconstructor {
                        DebugConstruction = DebugConstruction
                    }
                };
                tumourGenerator.TumourGenerate(args, ct);

                // Send the tumoured slider to the main thread
                Application.Current.Dispatcher.Invoke(() => {
                    TumouredPreviewHitObject = args;
                    layerRangeSliderMaxes.Clear();
                    layerRangeSliderMaxes.AddRange(tumourGenerator.LayerLengths);
                    RaisePropertyChanged(nameof(TumourStartSliderMin));
                    RaisePropertyChanged(nameof(TumourRangeSliderMax));
                });

                // Clean up the cancellation token
                lock (previewTokenLock) {
                    ct.ThrowIfCancellationRequested();
                    previewTokenSource.Dispose();
                    previewTokenSource = null;
                }
            }, ct).ContinueWith(task => {
                // Show the error if one occured while generating preview
                if (task.IsFaulted) {
                    task.Exception.Show();
                }
                // Stop the processing indicator
                Application.Current.Dispatcher.Invoke(() => {
                    IsProcessingPreview = false;
                });
            }, ct);
        }

        public enum ImportMode {
            Selected,
            Bookmarked,
            Time,
            Everything
        }
    }
}