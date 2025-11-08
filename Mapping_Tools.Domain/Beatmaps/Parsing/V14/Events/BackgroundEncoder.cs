using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class BackgroundEncoder : IEncoder<Background>
{
    public string Encode(Background obj)
    {
        // Always write offset, matching osu! format
        return $"{obj.EventType},{obj.StartTime.ToRoundInvariant()},\"{obj.Filename}\",{obj.XOffset.ToInvariant()},{obj.YOffset.ToInvariant()}";
    }
}