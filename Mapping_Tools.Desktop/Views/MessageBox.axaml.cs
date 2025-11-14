using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Views;

public partial class MessageBox : Window {
    public MessageBox(string message, string title = "Message")
    {
        InitializeComponent();
        MessageText.Text = message;
        Title = title;
    }

    private void OkClicked(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    public static async Task<bool?> Show(Window owner, string message, string title = "Message")
    {
        var box = new MessageBox(message, title);
        return await box.ShowDialog<bool?>(owner);
    }
}