using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class LayerImportArgs : INotifyPropertyChanged, IEquatable<LayerImportArgs> {
        public LayerImportArgs() {
            ImportType = ImportType.None;
            Path = "";
            X = -1;
            Y = -1;
            SamplePath = "";
            Bank = -1;
            Patch = -1;
            Key = -1;
            Length = -1;
            LengthRoughness = 1;
            Velocity = -1;
            VelocityRoughness = 1;
        }

        public LayerImportArgs(ImportType importType) {
            ImportType = importType;
            Path = "";
            X = -1;
            Y = -1;
            SamplePath = "";
            Bank = -1;
            Patch = -1;
            Key = -1;
            Length = -1;
            LengthRoughness = 1;
            Velocity = -1;
            VelocityRoughness = 1;
        }

        private ImportType importType;
        public ImportType ImportType {
            get { return importType; }
            set {
                if (importType != value) {
                    importType = value;
                    NotifyPropertyChanged("ImportType");
                    NotifyPropertyChanged("CoordinateVisibility");
                    NotifyPropertyChanged("KeysoundVisibility");
                }
            }
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

        public Visibility CoordinateVisibility { get { if (ImportType == ImportType.Stack) { return Visibility.Visible; } else { return Visibility.Collapsed; } } }

        public Visibility KeysoundVisibility { get { if (ImportType == ImportType.MIDI) { return Visibility.Visible; } else { return Visibility.Collapsed; } } }

        public bool CanImport { get { return ImportType != ImportType.None; } }

        private double x;
        public double X {
            get { return x; }
            set {
                if (x != value) {
                    x = value;
                    NotifyPropertyChanged("X");
                }
            }
        }

        private double y;
        public double Y {
            get { return y; }
            set {
                if (y != value) {
                    y = value;
                    NotifyPropertyChanged("Y");
                }
            }
        }

        private string samplePath;
        public string SamplePath {
            get { return path; }
            set {
                if (samplePath != value) {
                    samplePath = value;
                    NotifyPropertyChanged("SamplePath");
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

        private double lengthRoughness;
        public double LengthRoughness {
            get { return lengthRoughness; }
            set {
                if (lengthRoughness != value) {
                    lengthRoughness = value;
                    NotifyPropertyChanged("LengthRoughness");
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

        private int velocityRoughness;
        public int VelocityRoughness {
            get { return velocityRoughness; }
            set {
                if (velocityRoughness != value) {
                    velocityRoughness = value;
                    NotifyPropertyChanged("VelocityRoughness");
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public bool ExactlyEquals(LayerImportArgs other) {
            return Path == other.Path &&
                ImportType == other.ImportType &&
                X == other.X &&
                Y == other.Y &&
                SamplePath == other.SamplePath &&
                Bank == other.Bank &&
                Patch == other.Patch &&
                Key == other.Key &&
                Length == other.Length &&
                LengthRoughness == other.LengthRoughness &&
                Velocity == other.Velocity &&
                VelocityRoughness == other.VelocityRoughness;
        }

        public bool Equals(LayerImportArgs other) {
            // Functionally equals
            if (ImportType != other.ImportType)
                return false;
            if (ImportType == ImportType.Stack) {
                return Path == other.Path &&
                    X == other.X &&
                    Y == other.Y;
            } else if (ImportType == ImportType.Hitsounds) {
                return Path == other.path &&
                    SamplePath == other.SamplePath;
            } else if (ImportType == ImportType.MIDI) {
                return Path == other.Path &&
                    Bank == other.Bank &&
                    Patch == other.Patch &&
                    Key == other.Key &&
                    Length == other.Length &&
                    LengthRoughness == other.LengthRoughness &&
                    Velocity == other.Velocity &&
                    VelocityRoughness == other.VelocityRoughness;
            } else if (ImportType == ImportType.None) {
                return true;
            } else {
                return ExactlyEquals(other);
            }
        }

        public override bool Equals(object obj) {
            if (!(obj is LayerImportArgs)) {
                return false;
            }

            return Equals((LayerImportArgs)obj);
        }

        public override int GetHashCode() {
            var hashCode = -421944398;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + ImportType.GetHashCode();
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SamplePath);
            hashCode = hashCode * -1521134295 + Bank.GetHashCode();
            hashCode = hashCode * -1521134295 + Patch.GetHashCode();
            hashCode = hashCode * -1521134295 + Key.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + LengthRoughness.GetHashCode();
            hashCode = hashCode * -1521134295 + Velocity.GetHashCode();
            hashCode = hashCode * -1521134295 + VelocityRoughness.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(LayerImportArgs left, object right) {
            return left.Equals(right);
        }

        public static bool operator !=(LayerImportArgs left, object right) {
            return !left.Equals(right);
        }
    }
}
