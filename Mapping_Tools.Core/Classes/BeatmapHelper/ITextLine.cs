namespace Mapping_Tools.Core.Classes.BeatmapHelper
{
#nullable disable

    /// <summary>
    /// Defines round-trip parsing and serialization for one line of osu! file text.
    /// </summary>
    public interface ITextLine
    {
        /// <summary>
        /// Replaces the object's state by parsing a complete source line.
        /// </summary>
        /// <returns></returns>
        string GetLine();
        /// <summary>
        /// Serializes the current state as one complete source line.
        /// </summary>
        /// <param name="line"></param>
        void SetLine(string line);
    }
}
