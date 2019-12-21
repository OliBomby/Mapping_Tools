using System;
using System.Collections.Generic;

namespace Mapping_Tools.Components.Graph.Interpolation {
    public class InterpolatorComparer : IComparer<string> {
        public static string[] InterpolatorOrder = {"Single curve", "Single curve 2", "Single curve 3", 
            "Double curve", "Double curve 2", "Double curve 3", "Half sine", "Wave", "Parabola", "Linear"};

        public int Compare(string x, string y) {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (x == null && y == null) {
                return 0;
            }

            if (x == null) {
                return -1;
            }

            if (y == null) {
                return 1;
            }

            var indexX = IndexOf(x);
            var indexY = IndexOf(y);

            if (indexX == -1 && indexY == -1) {
                return string.Compare(x, y, StringComparison.Ordinal);
            }

            return indexX.CompareTo(indexY);
        }

        private int IndexOf(string name) {
            for (int i = 0; i < InterpolatorOrder.Length; i++) {
                if (InterpolatorOrder[i] == name) {
                    return i;
                }
            }

            return -1;
        }
    }
}