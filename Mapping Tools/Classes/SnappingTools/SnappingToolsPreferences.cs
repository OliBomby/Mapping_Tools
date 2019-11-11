using System.Collections.Generic;
using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Linq;
using System.Windows.Input;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.SnappingTools {
    public class SnappingToolsPreferences : BindableBase, ICloneable{
        #region private storage
        private Dictionary<string, RelevantObjectPreferences> relevantObjectPreferences;

        private Hotkey snapHotkey;
        private double offsetLeft;
        private double offsetTop;
        private double offsetRight;
        private double offsetBottom;
        private double acceptableDifference;
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

        #region global settings
        public Hotkey SnapHotkey {
            get => snapHotkey;
            set => Set(ref snapHotkey, value);
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

            snapHotkey = new Hotkey(Key.M, ModifierKeys.None);
            offsetLeft = 0;
            offsetTop = 1;
            offsetRight = 0;
            offsetBottom = 1;
            acceptableDifference = 2;
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