using Mapping_Tools.Domain.Beatmaps.Events;
using System.Text;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class OtherCommandEncoder : IEncoder<OtherCommand>
{
    public string Encode(OtherCommand obj)
    {
        var builder = new StringBuilder(8 + obj.Params.Length * 2);

        builder.Append(obj.CommandType.ToString());
        builder.Append(',');
        builder.Append(((int)obj.Easing).ToInvariant());
        builder.Append(',');
        builder.Append(obj.StartTime.ToRoundInvariant());
        builder.Append(',');
        if (!MathUtil.Precision.AlmostEquals(obj.EndTime, obj.StartTime))
            builder.Append(obj.EndTime.ToRoundInvariant());

        if (obj.Params.Length == 0)
        {
            builder.Append(',');
        }
        else
        {
            foreach (var param in obj.Params)
            {
                builder.Append(',');
                builder.Append(param.ToInvariant());
            }
        }

        return builder.ToString();
    }
}