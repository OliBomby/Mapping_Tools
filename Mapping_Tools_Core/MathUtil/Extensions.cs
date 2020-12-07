using System;
using System.Collections.Generic;

namespace Mapping_Tools_Core.MathUtil {
    public static class MyExtensions {
        public static int WordCount(this string str) {
            return str.Split(new char[] { ' ', '.', '?' },
                             StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static int Length(this List<Vector2> list) {
            return list.Count;
        }

        public static List<Vector2> Copy(this List<Vector2> list)
        {
            List<Vector2> newList = new List<Vector2>();
            newList.AddRange(list);
            return newList;
        }

        public static void Round(this IEnumerable<Vector2> list) {
            foreach (var v in list) {
                v.Round();
            }
        }
    }
}