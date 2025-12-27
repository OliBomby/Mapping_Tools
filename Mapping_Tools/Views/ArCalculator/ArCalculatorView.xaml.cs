using System.Windows.Navigation;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.ArCalculator
{
    /// <summary>
    /// Interaction logic for ArCalculatorView.xaml
    /// </summary>
    public partial class ArCalculatorView
    {
        public static readonly string ToolName = "AR Calculator";
        public static readonly string ToolDescription = "A tool to calculate the recommended AR based on the specified BPM.";

        public ArCalculatorView()
        {
            InitializeComponent();
            DataContext = new ArCalculatorVm();
        }

        private void OnOriginalToolNavigationRequested(object sender, RequestNavigateEventArgs e)
        {
            Browser.OpenLink(e.Uri);
            e.Handled = true;
        }
    }
}
