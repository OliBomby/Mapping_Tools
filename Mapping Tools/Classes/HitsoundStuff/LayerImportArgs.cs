using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class LayerImportArgs : INotifyPropertyChanged, IEquatable<LayerImportArgs> {
        /// <inheritdoc />
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
            discriminateVolumes = false;
            DetectDuplicateSamples = false;
            RemoveDuplicates = false;
        }

        /// <inheritdoc />
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
            discriminateVolumes = false;
            DetectDuplicateSamples = false;
            RemoveDuplicates = false;
        }

        private ImportType importType;
        /// <summary>
        /// 
        /// </summary>
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
        /// <summary>
        /// 
        /// </summary>
        public string Path {
            get => path;
            set {
                if (path != value) {
                    path = value;
                    NotifyPropertyChanged("Path");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Visibility CoordinateVisibility =>
            ImportType == ImportType.Stack ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 
        /// </summary>
        public Visibility KeysoundVisibility =>
            ImportType == ImportType.MIDI ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// 
        /// </summary>
        public bool CanImport => ImportType != ImportType.None;

        private double x;
        /// <summary>
        /// 
        /// </summary>
        public double X {
            get => x;
            set {
                if (x != value) {
                    x = value;
                    NotifyPropertyChanged("X");
                }
            }
        }

        private double y;
        /// <summary>
        /// 
        /// </summary>
        public double Y {
            get => y;
            set {
                if (y != value) {
                    y = value;
                    NotifyPropertyChanged("Y");
                }
            }
        }

        private string samplePath;
        /// <summary>
        /// 
        /// </summary>
        public string SamplePath {
            get => samplePath;
            set {
                if (samplePath != value) {
                    samplePath = value;
                    NotifyPropertyChanged("SamplePath");
                }
            }
        }

        private double volume;
        public double Volume {
            get => volume;
            set {
                if (volume == value) return;
                volume = value;
                NotifyPropertyChanged("Volume");
                NotifyPropertyChanged("Velocity");
            }
        }

        private bool discriminateVolumes;
        public bool DiscriminateVolumes {
            get => discriminateVolumes;
            set {
                if (discriminateVolumes == value) return;
                discriminateVolumes = value;
                NotifyPropertyChanged("DiscriminateVolumes");
            }
        }

        private bool detectDuplicateSamples;
        public bool DetectDuplicateSamples {
            get => detectDuplicateSamples;
            set {
                if (detectDuplicateSamples == value) return;
                detectDuplicateSamples = value;
                NotifyPropertyChanged("DetectDuplicateSamples");
            }
        }

        private bool removeDuplicates;
        public bool RemoveDuplicates {
            get => removeDuplicates;
            set {
                if (removeDuplicates == value) return;
                removeDuplicates = value;
                NotifyPropertyChanged("RemoveDuplicates");
            }
        }

        private int bank;
        /// <summary>
        /// 
        /// </summary>
        public int Bank {
            get => bank;
            set {
                if (bank != value) {
                    bank = value;
                    NotifyPropertyChanged("Bank");
                }
            }
        }

        private int patch;
        /// <summary>
        /// 
        /// </summary>
        public int Patch {
            get => patch;
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
        /// <summary>
        /// 
        /// </summary>
        public double Length {
            get => length;
            set {
                if (length != value) {
                    length = value;
                    NotifyPropertyChanged("Length");
                }
            }
        }

        private double lengthRoughness;
        /// <summary>
        /// 
        /// </summary>
        public double LengthRoughness {
            get => lengthRoughness;
            set {
                if (lengthRoughness != value) {
                    lengthRoughness = value;
                    NotifyPropertyChanged("LengthRoughness");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Velocity {
            get => (int)Math.Round(Volume * 127);
            set {
                if (Velocity == value) return;
                Volume = value / 127d;
                NotifyPropertyChanged("Velocity");
            }
        }

        private double velocityRoughness;
        /// <summary>
        /// 
        /// </summary>
        public double VelocityRoughness {
            get => velocityRoughness;
            set {
                if (velocityRoughness != value) {
                    velocityRoughness = value;
                    NotifyPropertyChanged("VelocityRoughness");
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propName"></param>
        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ImportReloadingArgs GetImportReloadingArgs() {
            return new ImportReloadingArgs(ImportType, Path, X, Y, LengthRoughness, VelocityRoughness, DiscriminateVolumes, DetectDuplicateSamples, RemoveDuplicates);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool ReloadCompatible(LayerImportArgs o) {
            if (ImportType != o.ImportType)
                return false;

            switch (ImportType) {
                case ImportType.Stack:
                    return Path == o.Path && (X == -1 || X == o.X) && (Y == -1 || Y == o.Y);
                case ImportType.Hitsounds:
                    return Path == o.Path && SamplePath == o.SamplePath && (!discriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DOUBLE_EPSILON);
                case ImportType.MIDI:
                    return Path == o.Path && (Bank == -1 || Bank == o.Bank) && (Patch == -1 || Patch == o.Patch) && (Key == -1 || Key == o.Key)
                                          && (Length == -1 || Length == o.Length) && (Velocity == -1 || Velocity == o.Velocity);
                case ImportType.Storyboard:
                    return Path == o.Path && SamplePath == o.SamplePath && (!discriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DOUBLE_EPSILON);
                case ImportType.None:
                    return true;
                default:
                    return Equals(o);
            }
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(LayerImportArgs other) {
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
                VelocityRoughness == other.VelocityRoughness &&
                RemoveDuplicates == other.RemoveDuplicates &&
                DiscriminateVolumes == other.DiscriminateVolumes &&
                DetectDuplicateSamples == other.DetectDuplicateSamples;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (!(obj is LayerImportArgs)) {
                return false;
            }

            return Equals((LayerImportArgs)obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
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
            hashCode = hashCode * -1521134295 + RemoveDuplicates.GetHashCode();
            hashCode = hashCode * -1521134295 + DiscriminateVolumes.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectDuplicateSamples.GetHashCode();
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
