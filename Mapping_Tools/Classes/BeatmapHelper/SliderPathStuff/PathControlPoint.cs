using System;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff {
    public class PathControlPoint : IEquatable<PathControlPoint> {
        /// <summary>
        /// The position of this <see cref="PathControlPoint"/>.
        /// </summary>
        [JsonProperty]
        public Vector2 Position {
            get;
            set;
        }

        /// <summary>
        /// The type of path segment starting at this <see cref="PathControlPoint"/>.
        /// If null, this <see cref="PathControlPoint"/> will be a part of the previous path segment.
        /// </summary>
        [JsonProperty]
        public PathType? Type {
            get;
            set;
        }

        /// <summary>
        /// Creates a new <see cref="PathControlPoint"/>.
        /// </summary>
        public PathControlPoint() { }

        /// <summary>
        /// Creates a new <see cref="PathControlPoint"/> with a provided position and type.
        /// </summary>
        /// <param name="position">The initial position.</param>
        /// <param name="type">The initial type.</param>
        public PathControlPoint(Vector2 position, PathType? type = null)
            : this() {
            Position = position;
            Type = type;
        }

        public bool Equals(PathControlPoint other) => Position == other?.Position && Type == other.Type;

        public override string ToString() => Type == null
            ? $"Position={Position}"
            : $"Position={Position}, Type={Type}";
    }
}