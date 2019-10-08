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
        private List<RelevantObjectPreferences> releventObjectPreferences;

        private Hotkey snapHotkey;
        private double offsetLeft;
        private double offsetTop;
        private double offsetRight;
        private double offsetBottom;
        private bool debugEnabled;
        private bool pressToViewEnabled;
        #endregion

        public List<RelevantObjectPreferences> RelevantObjectPreferences {
            get => releventObjectPreferences;
            set => Set(ref releventObjectPreferences, value);
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

        public bool DebugEnabled {
            get => debugEnabled;
            set => Set(ref debugEnabled, value);
        }

        public bool PressToViewEnabled {
            get => pressToViewEnabled;
            set => Set(ref pressToViewEnabled, value);
        }
        #endregion

        #region helper methods
        /// <summary>
        /// Finds and returns an existing instance of <see cref="RelevantObjectPreferences"/> based on <see cref="RelevantObjectPreferences.Name"/> property.
        /// </summary>
        /// <param name="input"><see cref="RelevantObjectPreferences.Name"/> property of the desired instance of <see cref="RelevantObjectPreferences.Name"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public RelevantObjectPreferences GetReleventObjectPreferences(string input) {
            List<RelevantObjectPreferences> output = releventObjectPreferences.Where(o => o.Name == input).ToList();
            return output[0] ?? throw new ArgumentOutOfRangeException();
        }
        #endregion

        #region IClonable members
        public object Clone() {
            return MemberwiseClone();
        }
        #endregion

        #region default constructor
        public SnappingToolsPreferences() {
            releventObjectPreferences = new List<RelevantObjectPreferences> {
                new RelevantObjectPreferences {
                    Name = "Virtual point preferences",
                    Color = Colors.Cyan,
                    Dashstyle = DashStylesEnum.Solid,
                    Opacity = 0.8,
                    Size = 5,
                    Thickness = 3,
                    HasSizeOption = true,
                },
                new RelevantObjectPreferences {
                    Name = "Virtual line preferences",
                    Color = Colors.LawnGreen,
                    Dashstyle = DashStylesEnum.Dash,
                    Opacity = 0.8,
                    Thickness = 3,
                    HasSizeOption = false,
                },
                new RelevantObjectPreferences {
                    Name = "Virtual circle preferences",
                    Color = Colors.Red,
                    Dashstyle = DashStylesEnum.Dash,
                    Opacity = 0.8,
                    Thickness = 3,
                    HasSizeOption = false,
                }
            };
            snapHotkey = new Hotkey(Key.M, ModifierKeys.None);
            offsetLeft = 0;
            offsetTop = 1;
            offsetRight = 0;
            offsetBottom = 1;
            debugEnabled = false;
            pressToViewEnabled = false;
        }
        #endregion

        public void CopyTo(SnappingToolsPreferences other) {
            foreach (var prop in typeof(SnappingToolsPreferences).GetProperties()) {
                if (!prop.CanWrite || !prop.CanRead) continue;
                try { prop.SetValue(other, prop.GetValue(this)); } catch { }
            }
        }
    }
}
