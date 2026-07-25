using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Holds bindable filters and sample-generation settings for one hitsound import layer.
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
            Offset = 0;
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
            Offset = 0;
        }

        private ImportType importType;
        /// <summary>
        /// Gets or sets how the source is interpreted and updates mode-dependent UI visibility.
        /// </summary>
        public ImportType ImportType {
            get { return importType; }
            set {
                if (importType != value) {
                    importType = value;
                    NotifyPropertyChanged("ImportType");
                    NotifyPropertyChanged(nameof(CoordinateVisibility));
                    NotifyPropertyChanged(nameof(KeysoundVisibility));
                }
            }
        }

        private string path;
        /// <summary>
        /// Gets or sets the imported beatmap, stack, MIDI, or hitsound source path.
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
        /// Indicates that stack-coordinate filters apply to the selected import mode.
        /// </summary>
        public bool CoordinateVisibility => ImportType == ImportType.Stack;

        /// <summary>
        /// Indicates that SoundFont/MIDI note filters apply to the selected import mode.
        /// </summary>
        public bool KeysoundVisibility => ImportType == ImportType.MIDI;

        /// <summary>
        /// Indicates that a concrete import mode has been selected.
        /// </summary>
        public bool CanImport => ImportType != ImportType.None;

        private double x;
        /// <summary>
        /// Gets or sets the stack X filter, or -1 to accept any X coordinate.
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
        /// Gets or sets the stack Y filter, or -1 to accept any Y coordinate.
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
        /// Gets or sets the audio file or SoundFont used to render imported events.
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
        /// <summary>
        /// Gets or sets linear sample gain and updates the derived MIDI <see cref="Velocity"/>.
        /// </summary>
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
        /// <summary>
        /// Controls whether otherwise matching hitsound imports remain separate when their volumes differ.
        /// </summary>
        public bool DiscriminateVolumes {
            get => discriminateVolumes;
            set {
                if (discriminateVolumes == value) return;
                discriminateVolumes = value;
                NotifyPropertyChanged("DiscriminateVolumes");
            }
        }

        private bool detectDuplicateSamples;
        /// <summary>
        /// Controls whether imported audio content is compared to detect duplicate files.
        /// </summary>
        public bool DetectDuplicateSamples {
            get => detectDuplicateSamples;
            set {
                if (detectDuplicateSamples == value) return;
                detectDuplicateSamples = value;
                NotifyPropertyChanged("DetectDuplicateSamples");
            }
        }

        private bool removeDuplicates;
        /// <summary>
        /// Controls whether detected duplicate import events are removed.
        /// </summary>
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
        /// Gets or sets the SoundFont bank, or -1 to accept any bank.
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
        /// Gets or sets the SoundFont patch, or -1 to accept any patch.
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
        /// <summary>
        /// Gets or sets the MIDI key, or -1 to accept any note.
        /// </summary>
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
        /// Gets or sets the MIDI note length, or -1 to accept any length.
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
        /// Gets or sets the tolerance used when grouping MIDI note lengths.
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
        /// Gets or sets MIDI velocity through the linear <see cref="Volume"/> scale.
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
        /// Gets or sets the tolerance used when grouping MIDI velocities.
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

        private double offset;
        /// <summary>
        /// Gets or sets the millisecond offset applied to imported events.
        /// </summary>
        public double Offset {
            get => offset;
            set {
                if (offset != value) {
                    offset = value;
                    NotifyPropertyChanged("Offset");
                }
            }
        }


        /// <summary>
        /// Notifies binding clients after an import option changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for a named import option.
        /// </summary>
        /// <param name="propName"></param>
        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// Captures the subset of settings that controls source-data cache invalidation.
        /// </summary>
        /// <returns></returns>
        public ImportReloadingArgs GetImportReloadingArgs() {
            return new ImportReloadingArgs(ImportType, Path, X, Y, LengthRoughness, VelocityRoughness, DiscriminateVolumes, DetectDuplicateSamples, RemoveDuplicates, Offset);
        }

        /// <summary>
        /// Determines whether cached imported data can be reused for another layer configuration, honoring wildcard filters.
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
                    return Path == o.Path && SamplePath == o.SamplePath && (!discriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DoubleEpsilon);
                case ImportType.MIDI:
                    return Path == o.Path && (Bank == -1 || Bank == o.Bank) && (Patch == -1 || Patch == o.Patch) && (Key == -1 || Key == o.Key)
                                          && (Length == -1 || Length == o.Length) && (Velocity == -1 || Velocity == o.Velocity);
                case ImportType.Storyboard:
                    return Path == o.Path && SamplePath == o.SamplePath && (!discriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DoubleEpsilon);
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
                DetectDuplicateSamples == other.DetectDuplicateSamples &&
                Offset == other.Offset;
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
            hashCode = hashCode * -1521134295 + Offset.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Applies the == operator.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true"/> when all import and sample-generation settings match.</returns>
        public static bool operator ==(LayerImportArgs left, object right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Applies the != operator.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true"/> when any import or sample-generation setting differs.</returns>
        public static bool operator !=(LayerImportArgs left, object right) {
            return !left.Equals(right);
        }
    }
}
