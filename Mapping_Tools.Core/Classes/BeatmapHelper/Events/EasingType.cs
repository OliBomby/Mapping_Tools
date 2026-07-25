namespace Mapping_Tools.Classes.BeatmapHelper.Events {
#nullable disable

    /// <summary>
    /// Identifies the interpolation curve applied by an osu! storyboard command.
    /// </summary>
    public enum EasingType {
        /// <summary>
        /// Constant-rate interpolation without easing.
        /// </summary>
        Linear, // Linear: no easing
        /// <summary>
        /// Legacy ease-out: fast initial change that slows near the end.
        /// </summary>
        EasingOut, // EasingType Out: the changes happen fast at first, but then slow down toward the end
        /// <summary>
        /// Legacy ease-in: slow initial change that accelerates near the end.
        /// </summary>
        EasingIn, // EasingType In: the changes happen slowly at first, but then speed up toward the end
        /// <summary>
        /// Quadratic acceleration from rest.
        /// </summary>
        QuadIn, // Quad In
        /// <summary>
        /// Quadratic deceleration to rest.
        /// </summary>
        QuadOut, // Quad Out
        /// <summary>
        /// Quadratic acceleration followed by deceleration.
        /// </summary>
        QuadInOut, // Quad In/Out
        /// <summary>
        /// Cubic acceleration from rest.
        /// </summary>
        CubicIn, // Cubic In
        /// <summary>
        /// Cubic deceleration to rest.
        /// </summary>
        CubicOut, // Cubic Out
        /// <summary>
        /// Cubic acceleration followed by deceleration.
        /// </summary>
        CubicInOut, // Cubic In/Out
        /// <summary>
        /// Quartic acceleration from rest.
        /// </summary>
        QuartIn, // Quart In
        /// <summary>
        /// Quartic deceleration to rest.
        /// </summary>
        QuartOut, // Quart Out
        /// <summary>
        /// Quartic acceleration followed by deceleration.
        /// </summary>
        QuartInOut, // Quart In/Out
        /// <summary>
        /// Quintic acceleration from rest.
        /// </summary>
        QuintIn, // Quint In
        /// <summary>
        /// Quintic deceleration to rest.
        /// </summary>
        QuintOut, // Quint Out
        /// <summary>
        /// Quintic acceleration followed by deceleration.
        /// </summary>
        QuintInOut, // Quint In/Out
        /// <summary>
        /// Sinusoidal acceleration from rest.
        /// </summary>
        SineIn, // Sine In
        /// <summary>
        /// Sinusoidal deceleration to rest.
        /// </summary>
        SineOut, // Sine Out
        /// <summary>
        /// Smooth sinusoidal acceleration and deceleration.
        /// </summary>
        SineInOut, // Sine In/Out
        /// <summary>
        /// Exponential acceleration from a near-zero rate.
        /// </summary>
        ExpoIn, // Expo In
        /// <summary>
        /// Exponential deceleration toward the final value.
        /// </summary>
        ExpoOut, // Expo Out
        /// <summary>
        /// Exponential acceleration followed by deceleration.
        /// </summary>
        ExpoInOut, // Expo In/Out
        /// <summary>
        /// Circular-arc acceleration from rest.
        /// </summary>
        CircIn, // Circ In
        /// <summary>
        /// Circular-arc deceleration to rest.
        /// </summary>
        CircOut, // Circ Out
        /// <summary>
        /// Circular-arc acceleration followed by deceleration.
        /// </summary>
        CircInOut, // Circ In/Out
        /// <summary>
        /// Oscillates with increasing amplitude before reaching the target.
        /// </summary>
        ElasticIn, // Elastic In
        /// <summary>
        /// Overshoots and oscillates with decreasing amplitude after the change.
        /// </summary>
        ElasticOut, // Elastic Out
        /// <summary>
        /// Ease-out oscillation with half-strength elasticity.
        /// </summary>
        ElasticHalfOut, // ElasticHalf Out
        /// <summary>
        /// Ease-out oscillation with quarter-strength elasticity.
        /// </summary>
        ElasticQuarterOut, // ElasticQuarter Out
        /// <summary>
        /// Elastic oscillation at both the start and end.
        /// </summary>
        ElasticInOut, // Elastic In/Out
        /// <summary>
        /// Pulls briefly away from the target before accelerating toward it.
        /// </summary>
        BackIn, // Back In
        /// <summary>
        /// Overshoots the target before settling back.
        /// </summary>
        BackOut, // Back Out
        /// <summary>
        /// Applies backward anticipation and final overshoot.
        /// </summary>
        BackInOut, // Back In/Out
        /// <summary>
        /// Reversed bounce motion approaching the main change.
        /// </summary>
        BounceIn, // Bounce In
        /// <summary>
        /// Repeatedly rebounds after reaching the target boundary.
        /// </summary>
        BounceOut, // Bounce Out
        /// <summary>
        /// Bounce motion at both the start and end.
        /// </summary>
        BounceInOut, // Bounce In/Out
    }
}
