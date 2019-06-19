using System;
using System.ComponentModel;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SampleGeneratingArgs : INotifyPropertyChanged, IEquatable<SampleGeneratingArgs> {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public SampleGeneratingArgs() {
            Path = "";
        }

        public SampleGeneratingArgs(string path) {
            Path = path;
        }

        public SampleGeneratingArgs(string path, int bank, int patch, int instrument, int key, int length, int velocity) {
            Path = path;
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Length = length;
            Velocity = velocity;
        }

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

        private int length;
        public int Length {
            get { return length; }
            set {
                if (length != value) {
                    length = value;
                    NotifyPropertyChanged("Length");
                }
            }
        }

        private int velocity;
        public int Velocity {
            get { return velocity; }
            set {
                if (velocity != value) {
                    velocity = value;
                    NotifyPropertyChanged("Velocity");
                }
            }
        }

        public override bool Equals(object obj) {
            if (!(obj is SampleGeneratingArgs)) {
                return false;
            }

            return Equals((SampleGeneratingArgs)obj);
        }

        public bool Equals(SampleGeneratingArgs other) {
            // Equality method can ignore bank, patch etc when path is not a soundfont because then those variables have no effect on how a sample gets generated
            if (System.IO.Path.GetExtension(Path) == ".sf2" && System.IO.Path.GetExtension(other.Path) == ".sf2") {
                return Path == other.Path &&
                Bank == other.Bank &&
                Patch == other.Patch &&
                Instrument == other.Instrument &&
                Key == other.Key &&
                Length == other.Length &&
                Velocity == other.Velocity;
            } else {
                return Path == other.Path;
            }
        }

        public static bool operator ==(SampleGeneratingArgs left, object right) {
            return left.Equals(right);
        }

        public static bool operator !=(SampleGeneratingArgs left, object right) {
            return !left.Equals(right);
        }
    }
}
