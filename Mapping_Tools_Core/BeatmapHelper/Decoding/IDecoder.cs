using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding {
    public interface IDecoder<T> {
        /// <summary>
        /// Parses the lines of text into the object.
        /// </summary>
        /// <param name="obj">The object to add parsed info to</param>
        /// <param name="lines">The lines of text to parse</param>
        void Decode(T obj, IReadOnlyCollection<string> lines);

        /// <summary>
        /// Parses the lines to create a new object.
        /// </summary>
        /// <param name="lines">The lines of text to parse</param>
        /// <returns>The new parsed object</returns>
        T DecodeNew(IReadOnlyCollection<string> lines);
    }
}