namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public interface IEncoder<in T> {
        /// <summary>
        /// Serializes an object to a code string.
        /// </summary>
        /// <param name="obj">The object to encode</param>
        /// <returns>The code</returns>
        string Encode(T obj);
    }
}