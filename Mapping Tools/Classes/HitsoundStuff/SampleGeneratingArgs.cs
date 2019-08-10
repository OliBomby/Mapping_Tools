using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SampleGeneratingArgs : INotifyPropertyChanged, IEquatable<SampleGeneratingArgs> {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public SampleGeneratingArgs() {
            Path = "";
            volume = 1;
        }

        public SampleGeneratingArgs(string path) {
            Path = path;
            volume = 1;
        }

        public SampleGeneratingArgs(string path, double volume, int bank, int patch, int instrument, int key, double length) {
            this.path = path;
            this.volume = volume;
            this.bank = bank;
            this.patch = patch;
            this.instrument = instrument;
            this.key = key;
            this.length = length;
        }

        public SampleGeneratingArgs(string path, int bank, int patch, int instrument, int key, double length, int velocity) {
            Path = path;
            volume = 1;
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Length = length;
            Velocity = velocity;
        }

        public bool UsesSoundFont { get { return System.IO.Path.GetExtension(Path) == ".sf2"; } }

        private string path;
        public string Path {
            get { return path; }
            set {
                if (path != value) {
                    path = value;
                    NotifyPropertyChanged("Path");
                }
            }
        }

        private double volume;
        public double Volume {
            get { return volume; }
            set {
                if (volume != value) {
                    volume = value;
                    NotifyPropertyChanged("Volume");
                    NotifyPropertyChanged("Velocity");
                }
            }
        }

        private int bank;
        public int Bank {
            get { return bank; }
            set {
                if (bank != value) {
                    bank = value;
                    NotifyPropertyChanged("Bank");
                }
            }
        }

        private int patch;
        public int Patch {
            get { return patch; }
            set {
                if (patch != value) {
                    patch = value;
                    NotifyPropertyChanged("Patch");
                }
            }
        }

        private int instrument;
        public int Instrument {
            get { return instrument; }
            set {
                if (instrument != value) {
                    instrument = value;
                    NotifyPropertyChanged("Instrument");
                }
            }
        }

        private int key;
        public int Key {
            get { return key; }
            set {
                if (key != value) {
                    key = value;
                    NotifyPropertyChanged("Key");
                }
            }
        }

        private double length;
        public double Length {
            get { return length; }
            set {
                if (length != value) {
                    length = value;
                    NotifyPropertyChanged("Length");
                }
            }
        }

        public int Velocity {
            get { return (int)Math.Round(Volume * 127); }
            set {
                if (Velocity != value) {
                    Volume = value / 127d;
                    NotifyPropertyChanged("Velocity");
                }
            }
        }

        public override string ToString() {
            if (System.IO.Path.GetExtension(Path) == ".sf2") {
                return string.Format("{0} {1},{2},{3},{4},{5},{6}", Path, Bank, Patch, Instrument, Key, Length, Velocity);
            } else {
                return string.Format("{0} {1}%", Path, Volume * 100);
            }
        }

        public SampleGeneratingArgs Copy() {
            return new SampleGeneratingArgs(Path, Volume, Bank, Patch, Instrument, Key, Length);
        }

        public bool Equals(SampleGeneratingArgs other) {
            // Equality method can ignore bank, patch etc when path is not a soundfont because then those variables have no effect on how a sample gets generated
            if (System.IO.Path.GetExtension(Path) == ".sf2" && System.IO.Path.GetExtension(other.Path) == ".sf2") {
                return Path == other.Path &&
                Bank == other.Bank &&
                Patch == other.Patch &&
                Instrument == other.Instrument &&
                Key == other.Key &&
                Length == other.Length;
            } else {
                return Path == other.Path &&
                Volume == other.Volume;
            }
        }

        public override bool Equals(object obj) {
            if (!(obj is SampleGeneratingArgs)) {
                return false;
            }

            return Equals((SampleGeneratingArgs)obj);
        }

        public bool ExactlyEquals(SampleGeneratingArgs other) {
            return Path == other.Path &&
            Volume == other.Volume &&
            Bank == other.Bank &&
            Patch == other.Patch &&
            Instrument == other.Instrument &&
            Key == other.Key &&
            Length == other.Length;
        }

        public override int GetHashCode() {
            var hashCode = 881410169;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + Volume.GetHashCode();
            if (System.IO.Path.GetExtension(Path) == ".sf2") {
                hashCode = hashCode * -1521134295 + Bank.GetHashCode();
                hashCode = hashCode * -1521134295 + Patch.GetHashCode();
                hashCode = hashCode * -1521134295 + Instrument.GetHashCode();
                hashCode = hashCode * -1521134295 + Key.GetHashCode();
                hashCode = hashCode * -1521134295 + Length.GetHashCode();
            }
            return hashCode;
        }

        public static bool operator ==(SampleGeneratingArgs left, object right) {
            return left.Equals(right);
        }

        public static bool operator !=(SampleGeneratingArgs left, object right) {
            return !left.Equals(right);
        }
    }
}
