namespace Mapping_Tools.Components.Dialogs {
    public partial class MessageDialog {
        public MessageDialog(string message) {
            InitializeComponent();
            MessageTextBlock.Text = message;
        }
    }
}
