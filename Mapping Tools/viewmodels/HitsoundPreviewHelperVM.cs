using Mapping_Tools.Classes;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mapping_Tools.Views.RhythmGuide;

namespace Mapping_Tools.Viewmodels
{
    public class HitsoundPreviewHelperVM : INotifyPropertyChanged
    {
        private ObservableCollection<HitsoundZone> _items;
        private bool? _isAllItemsSelected;
        private RhythmGuideWindow _rhythmGuideWindow;

        public HitsoundPreviewHelperVM() {
            _items = new ObservableCollection<HitsoundZone>();

            RhythmGuideCommand = new CommandImplementation(
                _ => {
                    try {
                        if (_rhythmGuideWindow == null) {
                            _rhythmGuideWindow = new RhythmGuideWindow();
                            _rhythmGuideWindow.Closed += RhythmGuideWindowOnClosed;
                            _rhythmGuideWindow.Show();
                        } else {
                            _rhythmGuideWindow.Focus();
                        }
                    } catch (Exception ex) { MessageBox.Show(ex.Message); }
                });
            AddCommand = new CommandImplementation(
                _ => {
                    try {
                        Items.Add(new HitsoundZone());
                    } catch (Exception ex) { MessageBox.Show(ex.Message); }
                });
            CopyCommand = new CommandImplementation(
                _ => {
                    try {
                        int initialCount = Items.Count;
                        for (int i = 0; i < initialCount; i++) {
                            if (Items[i].IsSelected) {
                                Items.Add(Items[i].Copy());
                            }
                        }
                    } catch (Exception ex) { MessageBox.Show(ex.Message); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
                        Items.RemoveAll(o => o.IsSelected);
                    } catch (Exception ex) { MessageBox.Show(ex.Message); }
                });
        }

        private void RhythmGuideWindowOnClosed(object sender, EventArgs e) {
            _rhythmGuideWindow = null;
        }

        public ObservableCollection<HitsoundZone> Items {
            get { return _items; }
            set {
                if (_items == value) return;
                _items = value;
                OnPropertyChanged();
            }
        }

        public bool? IsAllItemsSelected {
            get { return _isAllItemsSelected; }
            set {
                if (_isAllItemsSelected == value) return;

                _isAllItemsSelected = value;

                if (_isAllItemsSelected.HasValue)
                    SelectAll(_isAllItemsSelected.Value, Items);

                OnPropertyChanged();
            }
        }

        private static void SelectAll(bool select, IEnumerable<HitsoundZone> models) {
            foreach (var model in models) {
                model.IsSelected = select;
            }
        }

        public CommandImplementation RhythmGuideCommand { get; }
        public CommandImplementation AddCommand { get; }
        public CommandImplementation CopyCommand { get; }
        public CommandImplementation RemoveCommand { get; }

        public IEnumerable<string> SampleSets => Enum.GetNames(typeof(SampleSet));

        public IEnumerable<string> Hitsounds => Enum.GetNames(typeof(Hitsound));

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
