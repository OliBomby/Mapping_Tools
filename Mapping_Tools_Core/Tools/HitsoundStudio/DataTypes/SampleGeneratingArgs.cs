using Mapping_Tools_Core.Audio.SampleImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes {
    public class SampleGeneratingArgs : ISampleGeneratingArgs {
        public ISampleImportArgs ImportArgs { get; }

        public double Volume { get; }

        public SampleGeneratingArgs() { }

        public SampleGeneratingArgs(ISampleImportArgs importArgs, double volume) {
            ImportArgs = importArgs;
            Volume = volume;
        }

        public bool Equals(ISampleGeneratingArgs other) {
            if (other == null) return false;
            if (ImportArgs == null) {
                if (other.ImportArgs != null) return false;
            } else {
                if (!ImportArgs.Equals(other.ImportArgs)) return false;
            }
            return Volume.Equals(other.Volume);
        }

        public object Clone() {
            return new SampleGeneratingArgs(ImportArgs?.Clone() as ISampleImportArgs, Volume);
        }
    }
}