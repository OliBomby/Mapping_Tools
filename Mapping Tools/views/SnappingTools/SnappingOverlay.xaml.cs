using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Mapping_Tools.Classes.MathUtil;
using System.Threading;

namespace Mapping_Tools.Views.SnappingTools
{
    /// <summary>
    /// Interaction logic for SnappingOverlay.xaml
    /// </summary>
    public partial class SnappingOverlay : Window
    {
        public SnappingOverlay()
        {
            InitializeComponent();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        public static void CreateOverlayPoint(Vector2 position, Brush color, double radius)
        {
            OverlayPoint p = new OverlayPoint();
            p.size = radius * 2;
            p.child = new Ellipse
            {
                Width = p.size,
                Height = p.size,
                Fill = color,
                Opacity = 0.4
            };
            Canvas.SetLeft(p.child, position[0]);
            Canvas.SetTop(p.child, position[1]);
            SnappingToolsView.overlay.MainCanvas.Children.Add(p.child);
        }

        public static void CreateOverlayPoint(Vector2 position, Brush color)
        {
            OverlayPoint p = new OverlayPoint();
            p.size = 20;
            p.child = new Ellipse
            {
                Width = p.size,
                Height = p.size,
                Fill = color,
                Opacity = 0.4
            };
            Canvas.SetLeft(p.child, position[0]);
            Canvas.SetTop(p.child, position[1]);
            SnappingToolsView.overlay.MainCanvas.Children.Add(p.child);
        }

        public static void CreateOverlayPoint(Vector2 position)
        {
            OverlayPoint p = new OverlayPoint();
            p.size = 20;
            p.child = new Ellipse
            {
                Width = p.size,
                Height = p.size,
                Fill = Brushes.Red,
                Opacity = 0.4
            };
            Canvas.SetLeft(p.child, position[0]);
            Canvas.SetTop(p.child, position[1]);
            SnappingToolsView.overlay.MainCanvas.Children.Add(p.child);
        }
    }
}

public class OverlayPoint : Mapping_Tools.Classes.SnappingTools.IOverlayElement
{
    public Ellipse child { get; set; }
    public bool highlighted { get; set; } = false;
    public double size = 0;

    public void Highlight()
    {
        if (highlighted)
            return;

        child.Opacity = 0.8;
        child.Width = size * 1.2;
        child.Height = size * 1.2;

        highlighted = true;
    }

    public void UnHighlight()
    {
        if (!highlighted)
            return;

        child.Opacity = 0.5;
        child.Width = size;
        child.Height = size;

        highlighted = false;
    }
}

