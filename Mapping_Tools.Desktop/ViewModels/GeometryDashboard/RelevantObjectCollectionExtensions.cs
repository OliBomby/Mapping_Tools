using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection;

namespace Mapping_Tools.Desktop.ViewModels.GeometryDashboard;

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
