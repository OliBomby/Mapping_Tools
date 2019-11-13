using System.Collections.Generic;
using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Linq;
using System.Windows.Input;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Classes.SnappingTools {
    public class SnappingToolsPreferences : BindableBase, ICloneable{
        #region private storage
        private Dictionary<string, RelevantObjectPreferences> relevantObjectPreferences;
        private Dictionary<Type, GeneratorSettings> generatorSettings;

        private Hotkey snapHotkey;
        private Hotkey selectHotkey;
        private Hotkey lockHotkey;
        private Hotkey inheritHotkey;
        private double offsetLeft;
        private double offsetTop;
        private double offsetRight;
        private double offsetBottom;
        private double acceptableDifference;
        private bool keepRunning;
        private bool debugEnabled;
        private ViewMode keyDownViewMode;
        private ViewMode keyUpViewMode;
        private SelectedHitObjectMode selectedHitObjectMode;
        private UpdateMode updateMode;
        private int inceptionLevel;
        #endregion

        public Dictionary<string, RelevantObjectPreferences> RelevantObjectPreferences {
            get => relevantObjectPreferences;
            set => Set(ref relevantObjectPreferences, value);
        }

        public Dictionary<Type, GeneratorSettings> GeneratorSettings {
            get => generatorSettings;
            set => Set(ref generatorSettings, value);
        }

        #region global settings
        public Hotkey SnapHotkey {
            get => snapHotkey;
            set => Set(ref snapHotkey, value);
        }
        public Hotkey SelectHotkey {
            get => selectHotkey;
            set => Set(ref selectHotkey, value);
        }
        public Hotkey LockHotkey {
            get => lockHotkey;
            set => Set(ref lockHotkey, value);
        }
        public Hotkey InheritHotkey {
            get => inheritHotkey;
            set => Set(ref inheritHotkey, value);
        }

        public double OffsetLeft {
            get => offsetLeft;
            set => Set(ref offsetLeft, value);
        }

        public double OffsetTop {
            get => offsetTop;
            set => Set(ref offsetTop, value);
        }

        public double OffsetRight {
            get => offsetRight;
            set => Set(ref offsetRight, value);
        }

        public double OffsetBottom {
            get => offsetBottom;
            set => Set(ref offsetBottom, value);
        }

        public Box2 OverlayOffset => new Box2(OffsetLeft, OffsetTop, OffsetRight, OffsetBottom);

        public double AcceptableDifference {
            get => acceptableDifference;
            set => Set(ref acceptableDifference, value);
        }

        public bool KeepRunning {
            get => keepRunning;
            set => Set(ref keepRunning, value);
        }

        public bool DebugEnabled {
            get => debugEnabled;
            set => Set(ref debugEnabled, value);
        }

        public ViewMode KeyDownViewMode {
            get => keyDownViewMode;
            set => Set(ref keyDownViewMode, value);
        }

        public ViewMode KeyUpViewMode {
            get => keyUpViewMode;
            set => Set(ref keyUpViewMode, value);
        }

        public SelectedHitObjectMode SelectedHitObjectMode {
            get => selectedHitObjectMode;
            set => Set(ref selectedHitObjectMode, value);
        }

        public UpdateMode UpdateMode {
            get => updateMode;
            set => Set(ref updateMode, value);
        }

        public int InceptionLevel {
            get => inceptionLevel;
            set => Set(ref inceptionLevel, value);
        }
        #endregion

        #region helper methods
        /// <summary>
        /// Gets the instance of <see cref="RelevantObjectPreferences"/> out of the dictionary based on the <see cref="RelevantObjectPreferences.Name"/> property.
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
        public object Clone() {
            return MemberwiseClone();
        }
        #endregion

        #region default constructor
        public SnappingToolsPreferences() {
            relevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences> {
                {
                    "Virtual point preferences", new RelevantObjectPreferences {
                        Name = "Virtual point preferences",
                        Color = Colors.Cyan,
                        Dashstyle = DashStylesEnum.Solid,
                        Opacity = 0.8,
                        Size = 5,
                        Thickness = 3,
                        HasSizeOption = true,
                    }
                }, {
                    "Virtual line preferences", new RelevantObjectPreferences {
                        Name = "Virtual line preferences",
                        Color = Colors.LawnGreen,
                        Dashstyle = DashStylesEnum.Dash,
                        Opacity = 0.8,
                        Thickness = 3,
                        HasSizeOption = false,
                    }
                }, {
                    "Virtual circle preferences", new RelevantObjectPreferences {
                        Name = "Virtual circle preferences",
                        Color = Colors.Red,
                        Dashstyle = DashStylesEnum.Dash,
                        Opacity = 0.8,
                        Thickness = 3,
                        HasSizeOption = false,
                    }
                }
            };

            generatorSettings = new Dictionary<Type, GeneratorSettings>();

            snapHotkey = new Hotkey(Key.M, ModifierKeys.None);
            selectHotkey = new Hotkey(Key.N, ModifierKeys.None);
            lockHotkey = new Hotkey(Key.N, ModifierKeys.Shift);
            inheritHotkey = new Hotkey(Key.N, ModifierKeys.Alt);
            offsetLeft = 0;
            offsetTop = 1;
            offsetRight = 0;
            offsetBottom = 1;
            acceptableDifference = 2;
            keepRunning = false;
            debugEnabled = false;
            keyDownViewMode = ViewMode.Everything;
            keyUpViewMode = ViewMode.Everything;
            selectedHitObjectMode = SelectedHitObjectMode.VisibleOrSelected;
            inceptionLevel = 4;
        }
        #endregion

        public void CopyTo(SnappingToolsPreferences other) {
            foreach (var prop in typeof(SnappingToolsPreferences).GetProperties()) {
                if (!prop.CanWrite || !prop.CanRead) continue;
                try { prop.SetValue(other, prop.GetValue(this)); } catch { }
            }
        }
    }

    [Flags]
    public enum ViewMode {
        Nothing = 0,
        Children = 1,
        Parents = 1 << 1,
        Everything = 1 << 2,

        ChildrenAndParents = Children | Parents,
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