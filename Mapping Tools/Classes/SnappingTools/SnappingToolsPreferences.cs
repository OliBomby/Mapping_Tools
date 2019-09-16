using System.Collections.Generic;
using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Mapping_Tools.Classes.SnappingTools {
    public class SnappingToolsPreferences : BindableBase, ICloneable{
        #region private storage
        private Color pointColor = Colors.Cyan;
        private double pointOpacity = 0.8;
        private double pointThickness = 3;
        private DashStylesEnum pointDashstyle = DashStylesEnum.Solid;
        private double pointSize = 5;

        private Color lineColor = Colors.LawnGreen;
        private double lineOpacity = 0.8;
        private double lineThickness = 3;
        private DashStylesEnum lineDashstyle = DashStylesEnum.Dash;

        private Color circleColor = Colors.Red;
        private double circleOpacity = 0.8;
        private double circleThickness = 3;
        private DashStylesEnum circleDashstyle = DashStylesEnum.Dash;

        private Hotkey snapHotkey = new Hotkey(Key.M, ModifierKeys.None);
        private double scale = 1;
        private int offsetX = 0;
        private int offsetY = 0;
        #endregion

        #region point settings
        public Color PointColor {
            get => pointColor;
            set => Set(ref pointColor, value);
        }
        public double PointOpacity {
            get => pointOpacity;
            set => Set(ref pointOpacity, value);
        }
        public double PointThickness {
            get => pointThickness;
            set => Set(ref pointThickness, value);
        }
        public DashStylesEnum PointDashstyle {
            get => pointDashstyle;
            set => Set(ref pointDashstyle, value);
        }
        public double PointSize {
            get => pointSize;
            set => Set(ref pointSize, value);
        }
        #endregion

        #region line settings
        public Color LineColor {
            get => lineColor;
            set => Set(ref lineColor, value);
        }
        public double LineOpacity {
            get => lineOpacity;
            set => Set(ref lineOpacity, value);
        }
        public double LineThickness {
            get => lineThickness;
            set => Set(ref lineThickness, value);
        }
        public DashStylesEnum LineDashstyle {
            get => lineDashstyle;
            set => Set(ref lineDashstyle, value);
        }
        #endregion

        #region circle settings
        public Color CircleColor {
            get => circleColor;
            set => Set(ref circleColor, value);
        }
        public double CircleOpacity {
            get => circleOpacity;
            set => Set(ref circleOpacity, value);
        }
        public double CircleThickness {
            get => circleThickness;
            set => Set(ref circleThickness, value);
        }
        public DashStylesEnum CircleDashstyle {
            get => circleDashstyle;
            set => Set(ref circleDashstyle, value);
        }
        #endregion

        #region global settings
        public Hotkey SnapHotkey {
            get => snapHotkey;
            set => Set(ref snapHotkey, value);
        }

        public double Scale {
            get => scale;
            set => Set(ref scale, value);
        }

        public int OffsetX {
            get => offsetX;
            set => Set(ref offsetX, value);
        }

        public int OffsetY {
            get => offsetY;
            set => Set(ref offsetY, value);
        }

        public int[] Offset {
            get {
                return new int[] { offsetX, offsetY };
            }
        }
        #endregion

        #region dashstyle helpers
        public static IEnumerable<string> DashStylesEnumerable => Enum.GetNames(typeof(DashStylesEnum));

        public DashStyle GetDashStyle(DashStylesEnum input) {
            switch (input) {
                case DashStylesEnum.Dash:
                    return DashStyles.Dash;
                case DashStylesEnum.Dot:
                    return DashStyles.Dot;
                case DashStylesEnum.DashDot:
                    return DashStyles.DashDot;
                case DashStylesEnum.DashDotDot:
                    return DashStyles.DashDotDot;
                case DashStylesEnum.Solid:
                    return DashStyles.Solid;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region IClonable members
        public object Clone() {
            return MemberwiseClone();
        }
        #endregion
    }
}
