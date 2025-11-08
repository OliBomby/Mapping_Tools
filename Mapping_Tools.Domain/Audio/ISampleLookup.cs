namespace Mapping_Tools.Domain.Audio;

/// <summary>
/// A mapping from all available custom samples to their content hash. Used for determining which custom samples exist and which sounds they make.
/// </summary>
public interface ISampleLookup : IReadOnlyDictionary<string, string> {
    
}