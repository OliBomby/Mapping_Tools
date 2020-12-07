using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper
{
    /// <summary>
    /// Interface for a text file.
    /// </summary>
    public interface ITextFile
    {
        /// <summary>
        /// Returns with all lines found within text file.
        /// </summary>
        /// <returns></returns>
        List<string> GetLines();

        /// <summary>
        /// Edits the spesified text file with passed through lines.
        /// </summary>
        /// <param name="lines"></param>
        void SetLines(List<string> lines);
    }
}
