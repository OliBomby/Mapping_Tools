namespace Mapping_Tools_Core.BeatmapHelper.Decoding {
    public interface IDecoder<T> {
        /// <summary>
        /// Parses the code into the object.
        /// </summary>
        /// <param name="obj">The object to add parsed info to</param>
        /// <param name="code">The string to parse</param>
        void Decode(T obj, string code);

        /// <summary>
        /// Parses the code to create a new object.
        /// </summary>
        /// <param name="code">The string to parse</param>
        /// <returns>The new parsed object</returns>
        T DecodeNew(string code);
    }
}