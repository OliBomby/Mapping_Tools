using System;
using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;
using Mapping_Tools.Annotations;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternDetailsVm : BindableBase {

        #region Fields

        private string name;

        [UsedImplicitly]
        [DisplayName("Name")]
        [Description("The name of the pattern.")]
        public string Name {
            get => name;
            set => Set(ref name, value);
        }

        [UsedImplicitly]
        [DisplayName("Creation time")]
        public DateTime CreationTime { get; }

        [UsedImplicitly]
        [DisplayName("Time of last use")]
        public DateTime LastUsedTime { get; }

        [UsedImplicitly]
        [DisplayName("Usage count")]
        [InvariantCulture]
        public int UseCount { get; }

        [UsedImplicitly]
        [DisplayName("Object count")]
        [InvariantCulture]
        public int ObjectCount { get; }

        [UsedImplicitly]
        [DisplayName("Duration")]
        [InvariantCulture]
        public TimeSpan Duration { get; }

        [UsedImplicitly]
        [DisplayName("Beat length")]
        [InvariantCulture]
        public double BeatLength { get; }

        [UsedImplicitly]
        [DisplayName("File name")]
        [InvariantCulture]
        public string FileName { get; }

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