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

        private DateTime _saveDateTime;
        public DateTime SaveDateTime {
            get => _saveDateTime;
            set => Set(ref _saveDateTime, value);
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

        #endregion

        public Beatmap GetPatternBeatmap(OsuPatternFileHandler fileHandler) {
            return new BeatmapEditor(fileHandler.GetPatternPath(FileName)).Beatmap;
        }
    }
}