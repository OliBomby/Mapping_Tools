using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public interface IEncoder<T> {
        /// <summary>
        /// Serializes an object to lines of text.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The lines of serialized text</returns>
        IEnumerable<string> Encode(T obj);
    }
}