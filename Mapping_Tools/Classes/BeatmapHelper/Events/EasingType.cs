namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public enum EasingType {
        Linear, // Linear: no easing
        EasingOut, // EasingType Out: the changes happen fast at first, but then slow down toward the end
        EasingIn, // EasingType In: the changes happen slowly at first, but then speed up toward the end
        QuadIn, // Quad In
        QuadOut, // Quad Out
        QuadInOut, // Quad In/Out
        CubicIn, // Cubic In
        CubicOut, // Cubic Out
        CubicInOut, // Cubic In/Out
        QuartIn, // Quart In
        QuartOut, // Quart Out
        QuartInOut, // Quart In/Out
        QuintIn, // Quint In
        QuintOut, // Quint Out
        QuintInOut, // Quint In/Out
        SineIn, // Sine In
        SineOut, // Sine Out
        SineInOut, // Sine In/Out
        ExpoIn, // Expo In
        ExpoOut, // Expo Out
        ExpoInOut, // Expo In/Out
        CircIn, // Circ In
        CircOut, // Circ Out
        CircInOut, // Circ In/Out
        ElasticIn, // Elastic In
        ElasticOut, // Elastic Out
        ElasticHalfOut, // ElasticHalf Out
        ElasticQuarterOut, // ElasticQuarter Out
        ElasticInOut, // Elastic In/Out
        BackIn, // Back In
        BackOut, // Back Out
        BackInOut, // Back In/Out
        BounceIn, // Bounce In
        BounceOut, // Bounce Out
        BounceInOut, // Bounce In/Out
    }
}