using System;

namespace Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff {
    public enum SplineType
    {
        Catmull,
        BSpline,
        Linear,
        PerfectCurve
    }

    public readonly struct PathType : IEquatable<PathType> {
        /// <summary>
        /// The type of the spline that should be used to interpret the control points of the path.
        /// </summary>
        public SplineType Type {
            get;
            init;
        }

        /// <summary>
        /// The degree of a BSpline. Unused if <see cref="Type"/> is not <see cref="SplineType.BSpline"/>.
        /// Null means the degree is equal to the number of control points, 1 means linear, 2 means quadratic, etc.
        /// </summary>
        public int? Degree {
            get;
            init;
        }

        public PathType(SplineType splineType) {
            Type = splineType;
            Degree = null;
        }

        public static readonly PathType Catmull = new(SplineType.Catmull);
        public static readonly PathType Bezier = new(SplineType.BSpline);
        public static readonly PathType Linear = new(SplineType.Linear);
        public static readonly PathType PerfectCurve = new(SplineType.PerfectCurve);

        public static PathType BSpline(int degree) {
            if (degree <= 0) {
                throw new ArgumentOutOfRangeException(nameof(degree), @"Degree must be greater than 0.");
            }

            return new PathType { Type = SplineType.BSpline, Degree = degree };
        }

        public string Description {
            get {
                switch (Type) {
                    case SplineType.Catmull:
                        return "Catmull";

                    case SplineType.BSpline:
                        return Degree == null ? "Bezier" : "B-spline";

                    case SplineType.Linear:
                        return "Linear";

                    case SplineType.PerfectCurve:
                        return "Perfect curve";

                    default:
                        return Type.ToString();
                }
            }
        }

        public override int GetHashCode()
            => HashCode.Combine(Type, Degree);

        public override bool Equals(object? obj)
            => obj is PathType pathType && Equals(pathType);

        public bool Equals(PathType other)
            => Type == other.Type && Degree == other.Degree;

        public static bool operator ==(PathType a, PathType b) => a.Equals(b);
        public static bool operator !=(PathType a, PathType b) => !a.Equals(b);

        public override string ToString() => Description;
    }
}