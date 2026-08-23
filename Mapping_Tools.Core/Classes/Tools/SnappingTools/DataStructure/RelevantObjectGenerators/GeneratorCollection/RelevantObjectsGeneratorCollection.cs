namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

/// <summary>Ordered generator catalog used by the layer calculation engine.</summary>
public sealed class RelevantObjectsGeneratorCollection : List<RelevantObjectsGenerator>
{
    /// <summary>Creates a catalog retaining the supplied generator order.</summary>
    /// <param name="collection">The generators to copy into the catalog.</param>
    public RelevantObjectsGeneratorCollection(IEnumerable<RelevantObjectsGenerator> collection) : base(collection)
    {
    }

    /// <summary>Gets generators whose settings currently mark them active.</summary>
    /// <returns>The active generators in catalog order.</returns>
    public IEnumerable<RelevantObjectsGenerator> GetActiveGenerators()
    {
        return this.Where(o => o.Settings.IsActive);
    }
}
