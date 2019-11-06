using System.Collections.Generic;

namespace Mapping_Tools.Classes.BeatmapHelper {
    class HitObjectComparer : IEqualityComparer<HitObject> {
        public bool CheckIsSelected { get; set; }

        public HitObjectComparer(bool checkIsSelected = false) {
            CheckIsSelected = checkIsSelected;
        }

        public bool Equals(HitObject x, HitObject y) {
            return x.GetLine() == y.GetLine() && 
                (!CheckIsSelected || x.IsSelected == y.IsSelected);
        }

        public int GetHashCode(HitObject obj) {
            return EqualityComparer<string>.Default.GetHashCode(obj.GetLine());
        }
    }
}
