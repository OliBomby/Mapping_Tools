using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Viewmodels;
using Mapping_Tools_Core.Tools.ComboColourStudio;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.ComboColourStudio {
    public class ColourPointBindable : BindableBase, IColourPoint, ICloneable {
        private double _time;
        private ColourPointMode _mode;
        private readonly ObservableCollection<int> _colourSequence;

        private bool _isSelected;
        private ComboColourStudioVm _parentProject;

        public double Time {
            get => _time;
            set => Set(ref _time, value);
        }

        public ColourPointMode Mode {
            get => _mode;
            set => Set(ref _mode, value);
        }

        public IReadOnlyList<int> ColourSequence => _colourSequence;

        public bool IsSelected {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        [JsonIgnore]
        [NotNull]
        public ComboColourStudioVm ParentProject {
            get => _parentProject;
            set => Set(ref _parentProject, value);
        }

        [JsonIgnore]
        public IEnumerable<NamedColour> ColourSequenceColours => ColourSequence.Select(i => ParentProject.ObservableComboColours[i]);

        [JsonIgnore]
        [UsedImplicitly]
        public IEnumerable<ColourPointMode> ColourPointModes => Enum.GetValues(typeof(ColourPointMode)).Cast<ColourPointMode>();
        [JsonIgnore]
        public CommandImplementation AddColourCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveColourCommand { get; }

        [UsedImplicitly]
        public ColourPointBindable() : this(0, ColourPointMode.Normal, new int[0], null) {}

        public ColourPointBindable(IColourPoint colourPoint, ComboColourStudioVm parentProject) :
            this(colourPoint.Time, colourPoint.Mode, colourPoint.ColourSequence, parentProject) { }

        public ColourPointBindable(double time, ColourPointMode mode, IEnumerable<int> colourSequence, ComboColourStudioVm parentProject) {
            Time = time;
            Mode = mode;
            _colourSequence = new ObservableCollection<int>(colourSequence);
            ParentProject = parentProject;

            _colourSequence.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(ColourSequenceColours));
            
            AddColourCommand = new CommandImplementation(sender => {
                var cm = GetContextMenu();
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
            });

            RemoveColourCommand = new CommandImplementation(item => {
                if (ColourSequence.Count == 0) return;
                if (item == null) {
                    _colourSequence.RemoveAt(ColourSequence.Count - 1);
                } else {
                    _colourSequence.Remove((int) item);
                }
            });
        }

        private ContextMenu GetContextMenu() {
            var cm = new ContextMenu();

            if (ParentProject.ObservableComboColours.Count == 0) {
                cm.Items.Add(new MenuItem
                    {Header = "Add at least one combo colour before adding colours to this sequence."});
            } else {
                for (int i = 0; i < ParentProject.ObservableComboColours.Count; i++) {
                    var comboColour = ParentProject.ObservableComboColours[i];
                    int index = i;  // Make seperate index variable so the Command doesn't get changed from outer scope

                    cm.Items.Add(new MenuItem {
                        Header = comboColour.Name,
                        Icon = new PackIcon { Kind = PackIconKind.Circle, Foreground = new SolidColorBrush(comboColour.Colour) },
                        Command = new CommandImplementation(_ => {
                            _colourSequence.Add(index);
                        }),
                        Tag = comboColour
                    });
                }
            }

            return cm;
        }

        public object Clone() {
            return new ColourPointBindable(Time, Mode, ColourSequence, ParentProject);
        }
    }
}