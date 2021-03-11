using System;

namespace Mapping_Tools.Classes.Tools.HitsoundStudio {
    /// <summary>
    /// 
    /// </summary>
    public class UISampleGeneratingArgs : BindableBase {
        public UISampleGeneratingArgs() {
            Path = "";
            Volume = 1;
            Bank = -1;
            Patch = -1;
            Instrument = -1;
            Key = -1;
            Length = -1;
            Velocity = 127;
        }

        public UISampleGeneratingArgs(string path) {
            Path = path;
            Volume = 1;
            Bank = -1;
            Patch = -1;
            Instrument = -1;
            Key = -1;
            Length = -1;
            Velocity = 127;
        }

        public UISampleGeneratingArgs(string path, double volume, int bank, int patch, int instrument, int key, double length) {
            Path = path;
            Volume = volume;
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Length = length;
            Velocity = 127;
        }

        public UISampleGeneratingArgs(string path, int bank, int patch, int instrument, int key, double length, int velocity) {
            Path = path;
            Volume = 1;
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Length = length;
            Velocity = velocity;
        }

        /// <summary>
        /// Checks if the specified path is a cafewalk soundfont file.
        /// </summary>
        public bool UsesSoundFont => GetExtension() == ".sf2";

        private string path;
        public string Path {
            get => path;
            set => Set(ref path, value);
        }

        private double volume;
        public double Volume {
            get => volume;
            set => Set(ref volume, value);
        }

        private int bank;
        public int Bank {
            get => bank;
            set => Set(ref bank, value);
        }

        private int patch;
        public int Patch {
            get => patch;
            set => Set(ref patch, value);
        }

        private int instrument;
        public int Instrument {
            get => instrument;
            set => Set(ref instrument, value);
        }

        private int key;
        public int Key {
            get => key;
            set => Set(ref key, value);
        }

        private double length;
        public double Length {
            get => length;
            set => Set(ref length, value);
        }

        private int velocity;
        public int Velocity {
            get => velocity;
            set => Set(ref velocity, value);
        }

        /// <summary>Returns a string that represents the current object and can be used as a filename.</summary>
        public string GetFilename() {
            var filename = System.IO.Path.GetFileNameWithoutExtension(Path);
            return GetExtension() == ".sf2" ? 
                $"{filename}-{Bank}-{Patch}-{Instrument}-{Key}-{(int)Length}-{Velocity}" : 
                Math.Abs(Volume - 1) < Precision.DOUBLE_EPSILON ?
                   filename :
                $"{filename}-{(int)(Volume * 100)}";
        }

        /// <summary>
        /// Gets the extension of the file in <see cref="Path"/>
        /// </summary>
        /// <returns></returns>
        public string GetExtension() {
            return System.IO.Path.GetExtension(Path);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return GetExtension() == ".sf2" ? 
                $"{Path} {Bank},{Patch},{Instrument},{Key},{Length},{Velocity}" : 
                $"{Path} {Volume * 100}%";
        }

        public UISampleGeneratingArgs Copy() {
            return (UISampleGeneratingArgs) MemberwiseClone();
        }
    }
}
