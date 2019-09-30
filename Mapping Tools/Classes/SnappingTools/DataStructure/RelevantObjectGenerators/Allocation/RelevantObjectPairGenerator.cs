using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    public class RelevantObjectPairGenerator {
        public void GeneratePairs(ObjectLayer layer) {
            // Get the dependencies of all the active generators
            layer.GeneratorCollection.GetDependencies();
            // Check the preferences for generating preferences
            // Get the necessary layers
            // Take into account the situation. The changes of the layer
            // Generate pairs with a set of layers and a dependency
            // Return the new pairs
        }
    }
}