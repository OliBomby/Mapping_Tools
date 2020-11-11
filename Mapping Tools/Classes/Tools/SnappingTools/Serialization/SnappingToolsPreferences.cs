using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Classes.Tools.SnappingTools.Serialization {
    public class SnappingToolsPreferences : BindableBase, ICloneable {
        #region private storage
        private Dictionary<string, RelevantObjectPreferences> _relevantObjectPreferences;
        private Dictionary<Type, GeneratorSettings> _generatorSettings;

        private Hotkey _snapHotkey;
        private Hotkey _selectHotkey;
        private Hotkey _lockHotkey;
        private Hotkey _inheritHotkey;
        private Hotkey _refreshHotkey;
        private double _offsetLeft;
        private double _offsetTop;
        private double _offsetRight;
        private double _offsetBottom;
        private double _acceptableDifference;
        private bool _keepRunning;
        private bool _visiblePlayfieldBoundary;
        private bool _debugEnabled;
        private ViewMode _keyDownViewMode;
        private ViewMode _keyUpViewMode;
        private SelectedHitObjectMode _selectedHitObjectMode;
        private UpdateMode _updateMode;
        private int _inceptionLevel;
        #endregion

        public Dictionary<string, RelevantObjectPreferences> RelevantObjectPreferences {
            get => _relevantObjectPreferences;
            set => Set(ref _relevantObjectPreferences, value);
        }

        public Dictionary<Type, GeneratorSettings> GeneratorSettings {
            get => _generatorSettings;
            set => Set(ref _generatorSettings, value);
        }

        #region global settings
        public Hotkey SnapHotkey {
            get => _snapHotkey;
            set => Set(ref _snapHotkey, value);
        }
        public Hotkey SelectHotkey {
            get => _selectHotkey;
            set => Set(ref _selectHotkey, value);
        }
        public Hotkey LockHotkey {
            get => _lockHotkey;
            set => Set(ref _lockHotkey, value);
        }
        public Hotkey InheritHotkey {
            get => _inheritHotkey;
            set => Set(ref _inheritHotkey, value);
        }
        public Hotkey RefreshHotkey {
            get => _refreshHotkey;
            set => Set(ref _refreshHotkey, value);
        }

        public double OffsetLeft {
            get => _offsetLeft;
            set => Set(ref _offsetLeft, value);
        }

        public double OffsetTop {
            get => _offsetTop;
            set => Set(ref _offsetTop, value);
        }

        public double OffsetRight {
            get => _offsetRight;
            set => Set(ref _offsetRight, value);
        }

        public double OffsetBottom {
            get => _offsetBottom;
            set => Set(ref _offsetBottom, value);
        }

        public Box2 OverlayOffset => new Box2(OffsetLeft, OffsetTop, OffsetRight, OffsetBottom);

        public double AcceptableDifference {
            get => _acceptableDifference;
            set => Set(ref _acceptableDifference, value);
        }

        public bool KeepRunning {
            get => _keepRunning;
            set => Set(ref _keepRunning, value);
        }

        public bool VisiblePlayfieldBoundary {
            get => _visiblePlayfieldBoundary;
            set => Set(ref _visiblePlayfieldBoundary, value);
        }

        public bool DebugEnabled {
            get => _debugEnabled;
            set => Set(ref _debugEnabled, value);
        }

        public ViewMode KeyDownViewMode {
            get => _keyDownViewMode;
            set => Set(ref _keyDownViewMode, value);
        }

        public ViewMode KeyUpViewMode {
            get => _keyUpViewMode;
            set => Set(ref _keyUpViewMode, value);
        }

        public SelectedHitObjectMode SelectedHitObjectMode {
            get => _selectedHitObjectMode;
            set => Set(ref _selectedHitObjectMode, value);
        }

        public UpdateMode UpdateMode {
            get => _updateMode;
            set => Set(ref _updateMode, value);
        }

        public int InceptionLevel {
            get => _inceptionLevel;
            set => Set(ref _inceptionLevel, value);
        }
        #endregion

        #region helper methods
        /// <summary>
        /// Gets the instance of <see cref="RelevantObjectPreferences"/> out of the dictionary.
        /// </summary>
        public RelevantObjectPreferences GetReleventObjectPreferences(string input) {
            return RelevantObjectPreferences.TryGetValue(input, out var output) ? output : new RelevantObjectPreferences();
        }

        /// <summary>
        /// Applies the generator settings of the preferences to the specified generators
        /// </summary>
        /// <param name="generators">The generators to apply the settings to</param>
        public void ApplyGeneratorSettings(IEnumerable<RelevantObjectsGenerator> generators) {
            foreach (var generator in generators) {
                if (GeneratorSettings.TryGetValue(generator.GetType(), out var settings)) {
                    settings.CopyTo(generator.Settings);
                }
            }
        }

        /// <summary>
        /// Gets the settings from the specified generators and saves it to these preferences
        /// </summary>
        /// <param name="generators">The generators to get the settings from</param>
        public void SaveGeneratorSettings(IEnumerable<RelevantObjectsGenerator> generators) {
            foreach (var generator in generators) {
                GeneratorSettings[generator.GetType()] = generator.Settings;
            }
        }
        #endregion

        #region IClonable members
        /// <summary>
        /// Performs a deep copy
        /// </summary>
        /// <returns></returns>
        public object Clone() {
            var clone = (SnappingToolsPreferences)MemberwiseClone();
            clone.GeneratorSettings = new Dictionary<Type, GeneratorSettings>();
            foreach (var kvp in GeneratorSettings) {
                clone.GeneratorSettings.Add(kvp.Key, (GeneratorSettings)kvp.Value.Clone());
            }
            clone.RelevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences>();
            foreach (var kvp in RelevantObjectPreferences) {
                clone.RelevantObjectPreferences.Add(string.Copy(kvp.Key), (RelevantObjectPreferences)kvp.Value.Clone());
            }
            return clone;
        }
        #endregion

        #region default constructor
        public SnappingToolsPreferences() {
            _relevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences> {
                {
                    RelevantPoint.PreferencesNameStatic, new RelevantObjectPreferences {
                        Name = RelevantPoint.PreferencesNameStatic,
                        Color = Colors.Cyan,
                        Dashstyle = DashStylesEnum.Solid,
                        Opacity = 0.8,
                        Size = 5,
                        Thickness = 3,
                        HasSizeOption = true,
                    }
                }, {
                    RelevantLine.PreferencesNameStatic, new RelevantObjectPreferences {
                        Name = RelevantLine.PreferencesNameStatic,
                        Color = Colors.LawnGreen,
                        Dashstyle = DashStylesEnum.Dash,
                        Opacity = 0.8,
                        Thickness = 3,
                        HasSizeOption = false,
                    }
                }, {
                    RelevantCircle.PreferencesNameStatic, new RelevantObjectPreferences {
                        Name = RelevantCircle.PreferencesNameStatic,
                        Color = Colors.Red,
                        Dashstyle = DashStylesEnum.Dash,
                        Opacity = 0.8,
                        Thickness = 3,
                        HasSizeOption = false,
                    }
                }
            };

            _generatorSettings = new Dictionary<Type, GeneratorSettings>();

            _snapHotkey = new Hotkey(Key.M, ModifierKeys.None);
            _selectHotkey = new Hotkey(Key.N, ModifierKeys.None);
            _lockHotkey = new Hotkey(Key.N, ModifierKeys.Shift);
            _inheritHotkey = new Hotkey(Key.N, ModifierKeys.Alt);
            _refreshHotkey = new Hotkey(Key.B, ModifierKeys.None);
            _offsetLeft = 0;
            _offsetTop = 1;
            _offsetRight = 0;
            _offsetBottom = 1;
            _acceptableDifference = 2;
            _keepRunning = false;
            _debugEnabled = false;
            _keyDownViewMode = ViewMode.Parents;
            _keyUpViewMode = ViewMode.Everything;
            _selectedHitObjectMode = SelectedHitObjectMode.AllwaysAllVisible;
            _updateMode = UpdateMode.OsuActivated;
            _inceptionLevel = 5;
        }
        #endregion
    }

    [Flags]
    public enum ViewMode {
        Nothing = 0,
        Children = 1,
        DirectChildren = 1 << 1,
        Parents = 1 << 2,
        DirectParents = 1 << 3,
        Everything = 1 << 4
    }

    public enum SelectedHitObjectMode {
        AllwaysAllVisible,
        VisibleOrSelected,
        OnlySelected
    }

    public enum UpdateMode {
        AnyChange,
        TimeChange,
        HotkeyDown,
        OsuActivated
    }
}