namespace Mapping_Tools_Core.Audio.Exporting {
    public interface IMidiSampleExporter : ISampleExporter {
        /// <summary>
        /// Adds a MIDI note to the exporter.
        /// </summary>
        /// <param name="bankNumber"></param>
        /// <param name="patchNumber"></param>
        /// <param name="noteNumber"></param>
        /// <param name="duration"></param>
        /// <param name="velocity"></param>
        void AddMidiNote(int bankNumber, int patchNumber, int noteNumber, double duration, int velocity);
    }
}