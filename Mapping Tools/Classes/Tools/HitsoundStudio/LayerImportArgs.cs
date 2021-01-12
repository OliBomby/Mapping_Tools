using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;

namespace Mapping_Tools.Classes.Tools.HitsoundStudio {
    /// <summary>
    /// Specific to a single <see cref="HitsoundLayer"/>.
    /// Describes the relation between the <see cref="HitsoundLayer"/> and its source material.
    /// </summary>
    public class LayerImportArgs : BindableBase {
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
            get => importType;
            set {
                if (Set(ref importType, value)) {
                    RaisePropertyChanged(nameof(CoordinateVisibility));
                    RaisePropertyChanged(nameof(KeysoundVisibility));
                    RaisePropertyChanged(nameof(CanImport));
                }
            }
        }

        private string path;
        /// <summary>
        /// 
        /// </summary>
        public string Path {
            get => path;
            set => Set(ref path, value);
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
            set => Set(ref x, value);
        }

        private double y;
        /// <summary>
        /// 
        /// </summary>
        public double Y {
            get => y;
            set => Set(ref y, value);
        }

        private string samplePath;
        /// <summary>
        /// 
        /// </summary>
        public string SamplePath {
            get => samplePath;
            set => Set(ref samplePath, value);
        }

        private double volume;
        public double Volume {
            get => volume;
            set => Set(ref volume, value);
        }

        private bool discriminateVolumes;
        public bool DiscriminateVolumes {
            get => discriminateVolumes;
            set => Set(ref discriminateVolumes, value);
        }

        private bool detectDuplicateSamples;
        public bool DetectDuplicateSamples {
            get => detectDuplicateSamples;
            set => Set(ref detectDuplicateSamples, value);
        }

        private int bank;
        /// <summary>
        /// 
        /// </summary>
        public int Bank {
            get => bank;
            set => Set(ref bank, value);
        }

        private int patch;
        /// <summary>
        /// 
        /// </summary>
        public int Patch {
            get => patch;
            set => Set(ref patch, value);
        }

        private int key;
        public int Key {
            get => key;
            set => Set(ref key, value);
        }

        private double length;
        /// <summary>
        /// 
        /// </summary>
        public double Length {
            get => length;
            set => Set(ref length, value);
        }

        private double lengthRoughness;
        /// <summary>
        /// 
        /// </summary>
        public double LengthRoughness {
            get => lengthRoughness;
            set => Set(ref lengthRoughness, value);
        }

        private int velocity;
        /// <summary>
        /// 
        /// </summary>
        public int Velocity {
            get => velocity;
            set => Set(ref velocity, value);
        }

        private double velocityRoughness;
        /// <summary>
        /// 
        /// </summary>
        public double VelocityRoughness {
            get => velocityRoughness;
            set => Set(ref velocityRoughness, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ILayerSourceRef GetLayerSourceRef() {
            switch (ImportType) {
                case ImportType.Stack:
                    throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
    }
}
