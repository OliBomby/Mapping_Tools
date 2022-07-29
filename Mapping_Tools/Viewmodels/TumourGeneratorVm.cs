﻿using System;
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

        private bool _isProcessingPreview;
        [JsonIgnore]
        public bool IsProcessingPreview {
            get => _isProcessingPreview;
            set => Set(ref _isProcessingPreview, value);
        }
        
        private HitObject _previewHitObject;
        public HitObject PreviewHitObject {
            get => _previewHitObject;
            set => Set(ref _previewHitObject, value, action: RegeneratePreview);
        }

        private HitObject _tumouredPreviewHitObject;
        [JsonIgnore]
        public HitObject TumouredPreviewHitObject {
            get => _tumouredPreviewHitObject;
            set {
                if (Set(ref _tumouredPreviewHitObject, value)) {
                    IsProcessingPreview = false;
                }
            }
        }

        private ImportMode _importModeSetting;
        public ImportMode ImportModeSetting {
            get => _importModeSetting;
            set {
                if (Set(ref _importModeSetting, value)) {
                    RaisePropertyChanged(nameof(TimeCodeBoxVisibility));
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        private string _timeCode;
        public string TimeCode {
            get => _timeCode;
            set => Set(ref _timeCode, value);
        }

        [JsonIgnore]
        public Visibility TimeCodeBoxVisibility => ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        private ObservableCollection<TumourLayer> _tumourLayers;
        public ObservableCollection<TumourLayer> TumourLayers {
            get => _tumourLayers;
            set {
                if (Set(ref _tumourLayers, value)) {
                    RegeneratePreview();
                    foreach (TumourLayer layer in _tumourLayers) {
                        layer.PropertyChanged += TumourLayerOnPropertyChanged;
                    }
                    _tumourLayers.CollectionChanged += TumourLayersOnCollectionChanged;
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

        private TumourLayer _currentLayer;
        [JsonIgnore]
        public TumourLayer CurrentLayer {
            get => _currentLayer;
            set {
                if (Set(ref _currentLayer, value)) {
                    if (_currentLayer is not null) {
                        _currentLayer.PropertyChanged += TumourLayerOnPropertyChanged;
                    }
                }
            }
        }

        private int _currentLayerIndex;
        public int CurrentLayerIndex {
            get => _currentLayerIndex;
            set => Set(ref _currentLayerIndex, value);
        }

        private bool _justMiddleAnchors;
        public bool JustMiddleAnchors {
            get => _justMiddleAnchors;
            set => Set(ref _justMiddleAnchors, value, action: RegeneratePreview);
        }

        private bool _fixSv;
        public bool FixSv {
            get => _fixSv;
            set => Set(ref _fixSv, value);
        }

        private bool _delegateToBpm;
        public bool DelegateToBpm {
            get => _delegateToBpm;
            set => Set(ref _delegateToBpm, value);
        }

        private bool _removeSliderTicks;
        public bool RemoveSliderTicks {
            get => _removeSliderTicks;
            set => Set(ref _removeSliderTicks, value);
        }

        private bool _advancedOptions;
        public bool AdvancedOptions {
            get => _advancedOptions;
            set {
                if (Set(ref _advancedOptions, value)) {
                    RaisePropertyChanged(nameof(TumourStartSliderMin));
                }
            }
        }

        private bool _debugConstruction;
        public bool DebugConstruction {
            get => _debugConstruction;
            set => Set(ref _debugConstruction, value, action: RegeneratePreview);
        }

        private double _scale;
        public double Scale {
            get => _scale;
            set => Set(ref _scale, value, action: RegeneratePreview);
        }

        [JsonIgnore]
        public double TumourStartSliderMin => AdvancedOptions ? CurrentLayer is not null && CurrentLayer.UseAbsoluteRange ? -500 : -1 : 0;

        [JsonIgnore]
        public double TumourRangeSliderMax => CurrentLayer is not null && CurrentLayer.UseAbsoluteRange ? 500 : 1;

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

                BeatmapEditor editor = null;
                List<HitObject> markedObjects = null;

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

                if (markedObjects == null || !markedObjects.Any(o => o.IsSlider)) {
                    Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue(@"Could not find any sliders in imported hit objects."));
                    return;
                }

                PreviewHitObject = markedObjects.First(s => s.IsSlider);
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