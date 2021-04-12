using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public interface IEnumeratingEncoder<in T> : IEncoder<T> {
        /// <summary>
        /// Serializes an object to lines of text.
        /// </summary>
        /// <param name="obj">The object to encode</param>
        /// <returns>The lines of encoded text</returns>
        IEnumerable<string> EncodeEnumerable(T obj);
    }
}