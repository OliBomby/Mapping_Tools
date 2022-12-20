using System;
using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;
using System.Globalization;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternDetailsVm : BindableBase {


        #region Fields

        private string name;

        [DisplayName("Name")]
        [Description("The name of the pattern.")]
        public string Name {
            get => name;
            set => Set(ref name, value);
        }

        [DisplayName("Creation time")]
        public DateTime CreationTime { get; }

        [DisplayName("Time of last use")]
        public DateTime LastUsedTime { get; }

        [DisplayName("Usage count")]
        [InvariantCulture]
        public int UseCount { get; }

        [DisplayName("File name")]
        [InvariantCulture]
        public string FileName { get; }

        [DisplayName("Object count")]
        [InvariantCulture]
        public int ObjectCount { get; }

        [DisplayName("Duration")]
        [InvariantCulture]
        public TimeSpan Duration { get; }

        [DisplayName("Beat length")]
        [InvariantCulture]
        public double BeatLength { get; }

        #endregion

        public OsuPatternDetailsVm(OsuPattern pattern) {
            Name = pattern.Name;
            CreationTime = pattern.CreationTime;
            LastUsedTime = pattern.LastUsedTime;
            UseCount = pattern.UseCount;
            FileName = pattern.FileName;
            ObjectCount = pattern.ObjectCount;
            Duration = pattern.Duration;
            BeatLength = pattern.BeatLength;
        }
    }
}