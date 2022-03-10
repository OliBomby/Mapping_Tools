using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options {
    public class TumourLayer : BindableBase, ITumourLayer {
        private ITumourTemplate _tumourTemplate;
        private int _tumourSidedness;
        private GraphState _tumourLength;
        private GraphState _tumourScale;
        private GraphState _tumourRotation;
        private GraphState _tumourDistance;
        private int _tumourCount;
        private double _tumourStart;
        private double _tumourEnd;
        private bool _recalculate;

        public ITumourTemplate TumourTemplate {
            get => _tumourTemplate;
            set => Set(ref _tumourTemplate, value);
        }

        public int TumourSidedness {
            get => _tumourSidedness;
            set => Set(ref _tumourSidedness, value);
        }

        public GraphState TumourLength {
            get => _tumourLength;
            set => Set(ref _tumourLength, value);
        }

        public GraphState TumourScale {
            get => _tumourScale;
            set => Set(ref _tumourScale, value);
        }

        public GraphState TumourRotation {
            get => _tumourRotation;
            set => Set(ref _tumourRotation, value);
        }

        public GraphState TumourDistance {
            get => _tumourDistance;
            set => Set(ref _tumourDistance, value);
        }

        public int TumourCount {
            get => _tumourCount;
            set => Set(ref _tumourCount, value);
        }

        public double TumourStart {
            get => _tumourStart;
            set => Set(ref _tumourStart, value);
        }

        public double TumourEnd {
            get => _tumourEnd;
            set => Set(ref _tumourEnd, value);
        }

        public bool Recalculate {
            get => _recalculate;
            set => Set(ref _recalculate, value);
        }
    }
}