using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.MathUtil {
    public static class MyExtensions {
        public static int WordCount(this String str) {
            return str.Split(new char[] { ' ', '.', '?' },
                             StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static int Length(this List<Vector2> list) {
            return list.Count;
        }
    }
}