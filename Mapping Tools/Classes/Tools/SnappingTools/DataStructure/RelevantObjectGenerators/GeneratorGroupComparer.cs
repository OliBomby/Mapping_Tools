using System.Collections;
using System.Windows.Data;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators {
    public class GeneratorGroupComparer : IComparer {
        public int Compare(object x, object y) {
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

            var groupX = (CollectionViewGroup) x;
            var groupY = (CollectionViewGroup) y;
            var typeX = (GeneratorType)groupX.Name;
            var typeY = (GeneratorType)groupY.Name;
            return typeX.CompareTo(typeY);
        }
    }
}