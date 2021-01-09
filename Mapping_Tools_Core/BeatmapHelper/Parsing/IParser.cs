using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Parsing {
    public interface IParser<T> {
        /// <summary>
        /// Parses the lines of text into the object.
        /// </summary>
        /// <param name="obj">The object to add parsed info to</param>
        /// <param name="lines">The lines of text to parse</param>
        void Parse(T obj, IReadOnlyCollection<string> lines);

        /// <summary>
        /// Parses the lines to create a new object.
        /// </summary>
        /// <param name="lines">The lines of text to parse</param>
        /// <returns>The new parsed object</returns>
        T ParseNew(IReadOnlyCollection<string> lines);

        /// <summary>
        /// Serializes an object to lines of text.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The lines of serialized text</returns>
        IEnumerable<string> Serialize(T obj);
    }
}