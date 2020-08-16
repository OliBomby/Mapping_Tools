using Mapping_Tools.Classes.SystemTools;
using System;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    /// <summary>
    /// Must store the objects, the greenlines, the timing, the global SV, the tickrate, the difficulty settings,
    /// the hitsounds, absolute times and positions, combocolour index, combo numbers, stack leniency, gamemode.
    /// Also store additional metadata such as the name, the date it was saved, use count, the map title, artist, diffname, and mapper.
    /// </summary>
    public class OsuPattern : BindableBase {
        #region Fields

        private bool _isSelected;
        public bool IsSelected {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        private string _name;
        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        private DateTime _creationTime;
        public DateTime CreationTime {
            get => _creationTime;
            set => Set(ref _creationTime, value);
        }

        private DateTime _lastUsedTime;
        public DateTime LastUsedTime {
            get => _lastUsedTime;
            set => Set(ref _lastUsedTime, value);
        }

        private int _useCount;
        public int UseCount {
            get => _useCount;
            set => Set(ref _useCount, value);
        }

        private string _fileName;
        public string FileName {
            get => _fileName;
            set => Set(ref _fileName, value);
        }

        private int _objectCount;
        public int ObjectCount {
            get => _objectCount;
            set => Set(ref _objectCount, value);
        }

        private TimeSpan _duration;
        public TimeSpan Duration {
            get => _duration;
            set => Set(ref _duration, value);
        }

        private double _beatLength;
        public double BeatLength {
            get => _beatLength;
            set => Set(ref _beatLength, value);
        }

        #endregion

        public Beatmap GetPatternBeatmap(OsuPatternFileHandler fileHandler) {
            return new BeatmapEditor(fileHandler.GetPatternPath(FileName)).Beatmap;
        }
    }
}