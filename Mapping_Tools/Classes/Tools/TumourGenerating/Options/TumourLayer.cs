using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options {
    public class TumourLayer : BindableBase, ITumourLayer {
        private ITumourTemplate _tumourTemplate;
        private TumourSidedness _tumourSidedness;
        private GraphState _tumourLength;
        private GraphState _tumourScale;
        private GraphState _tumourRotation;
        private GraphState _tumourDistance;
        private int _tumourCount;
        private double _tumourStart;
        private double _tumourEnd;
        private bool _recalculate;
        private bool _isActive;

        public ITumourTemplate TumourTemplate {
            get => _tumourTemplate;
            set => Set(ref _tumourTemplate, value);
        }

        public TumourSidedness TumourSidedness {
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

        public bool IsActive {
            get => _isActive;
            set => Set(ref _isActive, value);
        }

        public TumourLayer() {
            TumourTemplate = new TriangleTemplate();
            IsActive = true;
            TumourLength = GetGraphState(15);
            TumourScale = GetGraphState(30);
            TumourRotation = GetGraphState(0);
            TumourDistance = GetGraphState(100);
            TumourCount = -1;
            TumourStart = 0;
            TumourEnd = 1;
        }

        private static GraphState GetGraphState(double value) {
            return new GraphState {
                MinX = 0,
                MinY = Math.Min(0, value * 2),
                MaxX = 1,
                MaxY = Math.Max(0, value * 2),
                Anchors = new List<AnchorState>() {
                    new() { Pos = new Vector2(0, value), Interpolator = new SingleCurveInterpolator() },
                    new() { Pos = new Vector2(1, value), Interpolator = new SingleCurveInterpolator() }
                }
            };
        }

        /// <summary>
        /// Freezes all freezable properties of this tumour layer.
        /// </summary>
        public void Freeze() {
            if (TumourLength is not null && TumourLength.CanFreeze) TumourLength.Freeze();
            if (TumourDistance is not null && TumourDistance.CanFreeze) TumourDistance.Freeze();
            if (TumourRotation is not null && TumourRotation.CanFreeze) TumourRotation.Freeze();
            if (TumourScale is not null && TumourScale.CanFreeze) TumourScale.Freeze();
        }
    }
}