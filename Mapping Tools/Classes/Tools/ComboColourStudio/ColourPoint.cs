using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.ComboColourStudio {
    public class ColourPoint : BindableBase, ICloneable {
        private double _time;
        private ObservableCollection<SpecialColour> _colourSequence; 
        private ColourPointMode _mode;
        private bool _isSelected;
        private ComboColourProject _parentProject;

        public ColourPoint() : this(0, new ObservableCollection<SpecialColour>(), ColourPointMode.Normal, null) {}

        public ColourPoint(double time, IEnumerable<SpecialColour> colourSequence, ColourPointMode mode, ComboColourProject parentProject) {
            Time = time;
            ColourSequence = new ObservableCollection<SpecialColour>(colourSequence);
            Mode = mode;
            ParentProject = parentProject;

            
            AddCommand = new CommandImplementation(sender => {
                var cm = GetContextMenu(ParentProject);
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
            });

            RemoveCommand = new CommandImplementation(item => {
                if (ColourSequence.Count == 0) return;
                if (item == null) {
                    ColourSequence.RemoveAt(ColourSequence.Count - 1);
                } else {
                    ColourSequence.Remove(item as SpecialColour);
                }
            });
        }

        private ContextMenu GetContextMenu(ComboColourProject colourSource) {
            var cm = new ContextMenu();

            if (colourSource.ComboColours.Count == 0) {
                cm.Items.Add(new MenuItem
                    {Header = "Add at least one combo colour before adding colours to this sequence."});
            } else {
                foreach (var comboColour in colourSource.ComboColours) {
                    cm.Items.Add(new MenuItem {
                        Header = comboColour.Name,
                        Icon = new PackIcon
                            {Kind = PackIconKind.Circle, Foreground = new SolidColorBrush(comboColour.Color)},
                        Command = new CommandImplementation(_ => {
                            ColourSequence.Add(comboColour);
                        }),
                        Tag = comboColour
                    });
                }
            }

            return cm;
        }

        public double Time {
            get => _time;
            set => Set(ref _time, value);
        }

        public ObservableCollection<SpecialColour> ColourSequence {
            get => _colourSequence;
            set => Set(ref _colourSequence, value);
        }

        public ColourPointMode Mode {
            get => _mode;
            set => Set(ref _mode, value);
        }

        public bool IsSelected {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        [CanBeNull]
        [JsonIgnore]
        public ComboColourProject ParentProject {
            get => _parentProject;
            set => Set(ref _parentProject, value);
        }
        
        public IEnumerable<ColourPointMode> ColourPointModes => Enum.GetValues(typeof(ColourPointMode)).Cast<ColourPointMode>();
        public CommandImplementation AddCommand { get; }
        public CommandImplementation RemoveCommand { get; }

        public object Clone() {
            var colours = new SpecialColour[ColourSequence.Count];
            for (int i = 0; i < ColourSequence.Count; i++) {
                colours[i] = (SpecialColour)ColourSequence[i].Clone();
            }
            return new ColourPoint(Time, colours, Mode, ParentProject);
        }
    }
}