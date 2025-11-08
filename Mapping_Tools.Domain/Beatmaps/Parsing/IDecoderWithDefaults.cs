namespace Mapping_Tools.Domain.Beatmaps.Parsing;

public interface IDecoderWithDefaults<out T> : IDecoder<T> {
    /// <summary>
    /// Parses the code to create a new object.
    /// </summary>
    /// <param name="code">The string to parse</param>
    /// <param name="defaultValues"> The default values to use when parsing</param>
    /// <returns>The new parsed object</returns>
    T Decode(string code, IDictionary<string, object> defaultValues);

    T IDecoder<T>.Decode(string code) {
        return Decode(code, new Dictionary<string, object>());
    }
}