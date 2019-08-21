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

namespace Mapping_Tools.Viewmodels
{
    public class HitsoundPreviewHelperVM : INotifyPropertyChanged
    {
        private ObservableCollection<HitsoundZone> _items;
        private bool? _isAllItemsSelected;

        public HitsoundPreviewHelperVM() {
            _items = new ObservableCollection<HitsoundZone>();

            AddCommand = new CommandImplementation(
                _ => {
                    Items.Add(new HitsoundZone());
                });
            CopyCommand = new CommandImplementation(
                _ => {
                    int initialCount = Items.Count;
                    for (int i = 0; i < initialCount; i++) {
                        if (Items[i].IsSelected) {
                            Items.Add(Items[i].Copy());
                        }
                    }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    Items.RemoveAll(o => o.IsSelected);
                });

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

        public CommandImplementation AddCommand { get; }
        public CommandImplementation CopyCommand { get; }
        public CommandImplementation RemoveCommand { get; }

        public IEnumerable<string> SampleSets {
            get {
                return Enum.GetNames(typeof(SampleSet));
            }
        }

        public IEnumerable<string> Hitsounds {
            get {
                return Enum.GetNames(typeof(Hitsound));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
