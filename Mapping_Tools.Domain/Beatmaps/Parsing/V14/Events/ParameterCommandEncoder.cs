using Mapping_Tools.Domain.Beatmaps.Events;
using System.Text;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class ParameterCommandEncoder : IEncoder<ParameterCommand>
{
    public string Encode(ParameterCommand obj)
    {
        var builder = new StringBuilder(9);

        builder.Append(obj.CommandType.ToString());
        builder.Append(',');
        builder.Append(((int)obj.Easing).ToInvariant());
        builder.Append(',');
        builder.Append(obj.StartTime.ToInvariant());
        builder.Append(',');
        if (!MathUtil.Precision.AlmostEquals(obj.StartTime, obj.EndTime))
        {
            builder.Append(obj.EndTime.ToInvariant());
        }

        builder.Append(',');
        builder.Append(obj.Parameter);

        return builder.ToString();
    }
}