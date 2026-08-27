using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectCollection;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

internal static class RelevantObjectCollectionExtensions
{
    public static List<IRelevantObject> GetOrCreate(this RelevantObjectCollection collection, Type type)
    {
        if (!collection.TryGetValue(type, out var values))
        {
            values = [];
            collection[type] = values;
        }

        return values;
    }
}
