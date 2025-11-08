using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class StoryboardSoundSampleEncoder : IEncoder<StoryboardSoundSample>
{
    public string Encode(StoryboardSoundSample obj)
    {
        return $"Sample,{obj.StartTime.ToInvariant()},{obj.Layer.ToIntInvariant()},\"{obj.FilePath}\",{obj.Volume.ToRoundInvariant()}";
    }
}