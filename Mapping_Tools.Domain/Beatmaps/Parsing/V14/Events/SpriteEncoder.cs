using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class SpriteEncoder : IEncoder<Sprite>
{
    public string Encode(Sprite obj)
    {
        return $"{obj.EventType},{obj.Layer},{obj.Origin},\"{obj.FilePath}\",{obj.Pos.X.ToRoundInvariant()},{obj.Pos.Y.ToRoundInvariant()}";
    }
}