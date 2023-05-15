﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class SampleGeneratingArgs : BindableBase, IEquatable<SampleGeneratingArgs> {
        public SampleGeneratingArgs() {
            Path = "";
            Volume = 1;
            Panning = 0;
            PitchShift = 0;
            Bank = -1;
            Patch = -1;
            Instrument = -1;
            Key = -1;
            Length = -1;
        }

        public SampleGeneratingArgs(string path) {
            Path = path;
            Volume = 1;
            Panning = 0;
            PitchShift = 0;
            Bank = -1;
            Patch = -1;
            Instrument = -1;
            Key = -1;
            Length = -1;
        }

        public SampleGeneratingArgs(string path, double volume, double panning, double pitchShift, int bank, int patch, int instrument, int key, double length) {
            Path = path;
            Volume = volume;
            Panning = panning;
            PitchShift = pitchShift;
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Length = length;
        }

        public SampleGeneratingArgs(string path, int bank, int patch, int instrument, int key, double length, int velocity) {
            Path = path;
            Panning = 0;
            PitchShift = 0;
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
        public bool UsesSoundFont => GetExtension().ToLower() == ".sf2";

        /// <summary>
        /// Means you can export this sample by simply copy pasting the source file in <see cref="Path"/>.
        /// </summary>
        public bool CanCopyPaste => !string.IsNullOrEmpty(GetExtension()) &&
                                    !UsesSoundFont &&
                                    Math.Abs(Volume - 1) < Precision.DoubleEpsilon &&
                                    Math.Abs(Panning) < Precision.DoubleEpsilon &&
                                    Math.Abs(PitchShift) < Precision.DoubleEpsilon;

        private string path;
        public string Path {
            get => path;
            set => Set(ref path, value);
        }

        private double volume;
        public double Volume {
            get => volume;
            set {
                if (Set(ref volume, value)) {
                    RaisePropertyChanged(nameof(Velocity));
                }
            }
        }

        private double panning;
        public double Panning {
            get => panning;
            set => Set(ref panning, value);
        }

        private double pitchShift;
        public double PitchShift {
            get => pitchShift;
            set => Set(ref pitchShift, value);
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

        [JsonIgnore]
        public int Velocity {
            get => (int) Math.Round(Volume * 127);
            set => Volume = value / 127d;
        }

        /// <summary>Returns a string that represents the current object and can be used as a filename.</summary>
        public string GetFilename() {
            var filename = System.IO.Path.GetFileNameWithoutExtension(Path);
            return GetExtension().ToLower() == ".sf2" ?
                Math.Abs(Panning) < Precision.DoubleEpsilon &&
                Math.Abs(PitchShift) < Precision.DoubleEpsilon ?
                    $"{filename}-{Bank}-{Patch}-{Instrument}-{Key}-{(int)Length}-{Velocity}" :
                $"{filename}-{(int)(Panning * 100)}-{(int)(PitchShift * 100)}-{Bank}-{Patch}-{Instrument}-{Key}-{(int)Length}-{Velocity}" :
                Math.Abs(Volume - 1) < Precision.DoubleEpsilon &&
                Math.Abs(Panning) < Precision.DoubleEpsilon &&
                Math.Abs(PitchShift) < Precision.DoubleEpsilon ?
                   filename :
                $"{filename}-{(int)(Volume * 100)}-{(int)(Panning * 100)}-{(int)(PitchShift * 100)}";
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
            return GetExtension().ToLower() == ".sf2" ? 
                $"{Path} p{Panning:N1} s{PitchShift:N3} {Bank},{Patch},{Instrument},{Key},{Length},{Velocity}" :
                $"{Path} {Volume * 100}% p{Panning:N1} s{PitchShift:N2}";
        }

        public SampleGeneratingArgs Copy() {
            return new SampleGeneratingArgs(Path, Volume, Panning, PitchShift, Bank, Patch, Instrument, Key, Length);
        }

        public bool Equals(SampleGeneratingArgs other) {
            if (other is null) return false;

            return Path == other.Path &&
                   Math.Abs(Volume - other.Volume) < Precision.DoubleEpsilon &&
                   Math.Abs(Panning - other.Panning) < Precision.DoubleEpsilon &&
                   Math.Abs(PitchShift - other.PitchShift) < Precision.DoubleEpsilon &&
                   Bank == other.Bank &&
                   Patch == other.Patch &&
                   Instrument == other.Instrument &&
                   Key == other.Key &&
                   Math.Abs(Length - other.Length) < Precision.DoubleEpsilon;
        }

        public override bool Equals(object obj) {
            if (!(obj is SampleGeneratingArgs)) {
                return false;
            }

            return Equals((SampleGeneratingArgs)obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() {
            var hashCode = 0x34894079;
            hashCode = hashCode * -0x5AAAAAD7 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -0x5AAAAAD7 + Volume.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Panning.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + PitchShift.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Bank.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Patch.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Instrument.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Key.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + Length.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(SampleGeneratingArgs left, object right) {
            return !(left is null) && left.Equals(right);
        }

        public static bool operator !=(SampleGeneratingArgs left, object right) {
            return left is null || !left.Equals(right);
        }
    }
}
