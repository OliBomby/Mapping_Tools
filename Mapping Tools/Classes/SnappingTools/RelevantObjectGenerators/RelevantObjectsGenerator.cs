using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public abstract class RelevantObjectsGenerator : INotifyPropertyChanged
    {
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public string Name {
            get; }
        public string Tooltip {
            get; }
        public GeneratorType GeneratorType { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
