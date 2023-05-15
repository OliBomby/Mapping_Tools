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

        private TumourTemplate tumourTemplateEnum;
        private WrappingMode wrappingMode;
        private TumourSidedness tumourSidedness;
        private GraphState tumourLength;
        private GraphState tumourScale;
        private GraphState tumourRotation;
        private GraphState tumourParameter;
        private GraphState tumourDistance;
        private int tumourCount;
        private double tumourStart;
        private double tumourEnd;
        private int randomSeed;
        private bool recalculate;
        private bool useAbsoluteRange;
        private bool isActive;
        private string name;

        public TumourTemplate TumourTemplateEnum {
            get => tumourTemplateEnum;
            set {
                if (Set(ref tumourTemplateEnum, value)) {
                    RaisePropertyChanged(nameof(TumourTemplate));
                }
            }
        }

        [JsonIgnore]
        public ITumourTemplate TumourTemplate => tumourTemplateEnum switch {
            Enums.TumourTemplate.Triangle => TriangleTemplate,
            Enums.TumourTemplate.Square => SquareTemplate,
            Enums.TumourTemplate.Circle => CircleTemplate,
            Enums.TumourTemplate.Parabola => ParabolaTemplate,
            _ => TriangleTemplate
        };

        public WrappingMode WrappingMode {
            get => wrappingMode;
            set => Set(ref wrappingMode, value);
        }

        public TumourSidedness TumourSidedness {
            get => tumourSidedness;
            set => Set(ref tumourSidedness, value);
        }

        public GraphState TumourLength {
            get => tumourLength;
            set => Set(ref tumourLength, value);
        }

        public GraphState TumourScale {
            get => tumourScale;
            set => Set(ref tumourScale, value);
        }

        public GraphState TumourRotation {
            get => tumourRotation;
            set => Set(ref tumourRotation, value);
        }

        public GraphState TumourParameter {
            get => tumourParameter;
            set => Set(ref tumourParameter, value);
        }

        public GraphState TumourDistance {
            get => tumourDistance;
            set => Set(ref tumourDistance, value);
        }

        public int TumourCount {
            get => tumourCount;
            set => Set(ref tumourCount, value);
        }

        public double TumourStart {
            get => tumourStart;
            set => Set(ref tumourStart, value);
        }

        public double TumourEnd {
            get => tumourEnd;
            set => Set(ref tumourEnd, value);
        }

        public int RandomSeed {
            get => randomSeed;
            set => Set(ref randomSeed, value);
        }

        public bool UseAbsoluteRange {
            get => useAbsoluteRange;
            set => Set(ref useAbsoluteRange, value);
        }

        public bool Recalculate {
            get => recalculate;
            set => Set(ref recalculate, value);
        }

        public bool IsActive {
            get => isActive;
            set => Set(ref isActive, value);
        }

        public string Name {
            get => name;
            set => Set(ref name, value);
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