using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class VideoEncoder : IEncoder<Video>
{
    public string Encode(Video obj)
    {
        if (obj.XOffset == 0 && obj.YOffset == 0)
            return $"{obj.EventType},{obj.StartTime.ToRoundInvariant()},\"{obj.Filename}\"";
        return $"{obj.EventType},{obj.StartTime.ToRoundInvariant()},\"{obj.Filename}\",{obj.XOffset.ToInvariant()},{obj.YOffset.ToInvariant()}";
    }
}