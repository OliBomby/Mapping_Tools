using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class AnimationEncoder : IEncoder<Animation>
{
    public string Encode(Animation obj)
    {
        return $"Animation,{obj.Layer},{obj.Origin},\"{obj.FilePath}\",{obj.Pos.X.ToRoundInvariant()},{obj.Pos.Y.ToRoundInvariant()},{obj.FrameCount.ToInvariant()},{obj.FrameDelay.ToInvariant()},{obj.LoopType}";
    }
}