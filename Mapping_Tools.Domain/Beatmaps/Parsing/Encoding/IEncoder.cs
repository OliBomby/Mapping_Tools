namespace Mapping_Tools.Domain.Beatmaps.Parsing.Encoding;

public interface IEncoder<in T> {
    /// <summary>
    /// Serializes an object to a code string.
    /// </summary>
    /// <param name="obj">The object to encode</param>
    /// <returns>The code</returns>
    string Encode(T obj);
}