using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options {
    public class TumourLayer : BindableBase, ITumourLayer {
        private static readonly ITumourTemplate TriangleTemplate = new TriangleTemplate();
        private static readonly ITumourTemplate SquareTemplate = new SquareTemplate();
        private static readonly ITumourTemplate CircleTemplate = new CircleTemplate();
        private static readonly ITumourTemplate ParabolaTemplate = new ParabolaTemplate();

        private TumourTemplate _tumourTemplateEnum;
        private WrappingMode _wrappingMode;
        private TumourSidedness _tumourSidedness;
        private GraphState _tumourLength;
        private GraphState _tumourScale;
        private GraphState _tumourRotation;
        private GraphState _tumourParameter;
        private GraphState _tumourDistance;
        private int _tumourCount;
        private double _tumourStart;
        private double _tumourEnd;
        private int _randomSeed;
        private bool _recalculate;
        private bool _useAbsoluteRange;
        private bool _isActive;
        private string _name;

        public TumourTemplate TumourTemplateEnum {
            get => _tumourTemplateEnum;
            set {
                if (Set(ref _tumourTemplateEnum, value)) {
                    RaisePropertyChanged(nameof(TumourTemplate));
                }
            }
        }

        [JsonIgnore]
        public ITumourTemplate TumourTemplate => _tumourTemplateEnum switch {
            Enums.TumourTemplate.Triangle => TriangleTemplate,
            Enums.TumourTemplate.Square => SquareTemplate,
            Enums.TumourTemplate.Circle => CircleTemplate,
            Enums.TumourTemplate.Parabola => ParabolaTemplate,
            _ => TriangleTemplate
        };

        public WrappingMode WrappingMode {
            get => _wrappingMode;
            set => Set(ref _wrappingMode, value);
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

        public GraphState TumourParameter {
            get => _tumourParameter;
            set => Set(ref _tumourParameter, value);
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

        public int RandomSeed {
            get => _randomSeed;
            set => Set(ref _randomSeed, value);
        }

        public bool UseAbsoluteRange {
            get => _useAbsoluteRange;
            set => Set(ref _useAbsoluteRange, value);
        }

        public bool Recalculate {
            get => _recalculate;
            set => Set(ref _recalculate, value);
        }

        public bool IsActive {
            get => _isActive;
            set => Set(ref _isActive, value);
        }

        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        [JsonIgnore]
        public CommandImplementation RandomizeRandomSeedCommand { get; }

        public TumourLayer() {
            RandomizeRandomSeedCommand = new CommandImplementation(_ => {
                RandomSeed = new Random().Next();
            });
        }

        public static TumourLayer GetDefaultLayer() {
            var l = new TumourLayer {
                TumourTemplateEnum = Enums.TumourTemplate.Triangle,
                WrappingMode = WrappingMode.Simple,
                IsActive = true,
                Name = "Layer",
                TumourCount = 0,
                TumourStart = 0,
                TumourEnd = 256,
                TumourLength = GetGraphState(15),
                TumourScale = GetGraphState(30),
                TumourRotation = GetGraphState(0),
                TumourParameter = GetGraphState(0),
                TumourDistance = GetGraphState(100),
                RandomSeed = 0,
                UseAbsoluteRange = true,
                Recalculate = true
            };
            return l;
        }

        public static GraphState GetGraphState(double value) {
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
            if (TumourParameter is not null && TumourParameter.CanFreeze) TumourParameter.Freeze();
            if (TumourScale is not null && TumourScale.CanFreeze) TumourScale.Freeze();
        }

        public TumourLayer Copy() {
            Freeze();
            return (TumourLayer) MemberwiseClone();
        }
    }
}