namespace Mapping_Tools.Components.Dialogs.SampleDialog {
    /// <summary>
    /// Interaction logic for SampleDialog.xaml
    /// </summary>
    public partial class SampleDialog {
        public SampleDialogViewModel ViewModel => (SampleDialogViewModel) DataContext;

        public SampleDialog() {
            InitializeComponent();
            DataContext = new SampleDialogViewModel();
        }
    }
}
