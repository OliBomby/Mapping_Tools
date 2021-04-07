using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper.Contexts {
    public class StackingContext : IContext {
        /// <summary>
        /// The stack count indicates the number of hit objects that this object is stacked upon.
        /// Used for calculating stack offset.
        /// </summary>
        public int StackCount { get; set; }

        /// <summary>
        /// The offset from the original position to the stacked position.
        /// </summary>
        public Vector2 StackOffset { get; set; }

        public StackingContext(int stackCount, Vector2 stackOffset) {
            StackCount = stackCount;
            StackOffset = stackOffset;
        }

        /// <summary>
        /// Calculates the stacked position of a position on the hit object.
        /// </summary>
        /// <param name="pos">The position to calculate stacked position of.</param>
        /// <returns>The stacked position.</returns>
        public Vector2 Stacked(Vector2 pos) {
            return pos + StackOffset;
        }

        public IContext Copy() {
            return new StackingContext(StackCount, StackOffset);
        }
    }
}