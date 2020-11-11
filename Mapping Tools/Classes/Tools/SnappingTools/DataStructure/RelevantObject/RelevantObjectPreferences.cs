using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject {
    public class RelevantObjectPreferences : BindableBase, ICloneable {
        #region private storage
        private string name;
        private Color color;
        private double opacity;
        private double thickness;
        private DashStylesEnum dashstyle;
        private double size;

        private bool hasSizeOption;
        #endregion
        public string Name {
            get => name;
            set => Set(ref name, value);
        }

        public Color Color {
            get => color;
            set => Set(ref color, value);
        }
        public double Opacity {
            get => opacity;
            set => Set(ref opacity, value);
        }
        public double Thickness {
            get => thickness;
            set => Set(ref thickness, value);
        }
        public DashStylesEnum Dashstyle {
            get => dashstyle;
            set => Set(ref dashstyle, value);
        }

        public double Size {
            get => size;
            set => Set(ref size, value);
        }

        public bool HasSizeOption {
            get => hasSizeOption;
            set => Set(ref hasSizeOption, value);
        }

        public static IEnumerable<DashStylesEnum> DashStylesEnumerable => Enum.GetValues(typeof(DashStylesEnum)).Cast<DashStylesEnum>();

        public DashStyle GetDashStyle() {
            switch (Dashstyle) {
                case DashStylesEnum.Dash:
                    return DashStyles.Dash;
                case DashStylesEnum.Dot:
                    return DashStyles.Dot;
                case DashStylesEnum.DashSingleDot:
                    return DashStyles.DashDot;
                case DashStylesEnum.DashDoubleDot:
                    return DashStyles.DashDotDot;
                case DashStylesEnum.Solid:
                    return DashStyles.Solid;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object Clone() {
            return MemberwiseClone();
        }
    }
}