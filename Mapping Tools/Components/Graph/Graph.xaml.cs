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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Graph : UserControl {
        Point? dragStart = null;
        bool Draggin = false;
        private bool _isRectDragInProg;

        public Graph() {
            InitializeComponent();

            void mouseDown(object sender, MouseButtonEventArgs args) {
                var element = (UIElement)sender;
                //dragStart = args.GetPosition(element);
                Console.WriteLine("mousedown");
                Console.WriteLine(dragStart);
                Draggin = true;
                element.CaptureMouse();
            }
            void mouseUp(object sender, MouseButtonEventArgs args) {
                var element = (UIElement)sender;
                //dragStart = null;
                Draggin = false;
                Console.WriteLine("mouseup");
                element.ReleaseMouseCapture();
            }
            void mouseMove(object sender, MouseEventArgs args) {
                //Console.WriteLine("mousemove");
                if (Draggin && args.LeftButton == MouseButtonState.Pressed) {
                    var element = (UIElement)sender;
                    var p2 = args.MouseDevice.GetPosition(mainCanvas);
                    Console.WriteLine(p2);
                    Canvas.SetLeft(element, p2.X - 10);
                    Canvas.SetTop(element, p2.Y - 10);
                }
            }
            void enableDrag(UIElement element) {
                element.MouseDown += mouseDown;
                element.MouseMove += mouseMove;
                element.MouseUp += mouseUp;
            }

            var shapes = new UIElement[] {
            new Ellipse() { Fill = Brushes.DarkKhaki, Width = 100, Height = 100 },
            new Rectangle() { Fill = Brushes.LawnGreen, Width = 200, Height = 100 },
            };


            foreach (var shape in shapes) {
                enableDrag(shape);
                //mainCanvas.Children.Add(shape);
            }
            mainCanvas.MouseMove += mouseMove;
        }

        private void rect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            _isRectDragInProg = true;
            rect.CaptureMouse();
        }

        private void rect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            _isRectDragInProg = false;
            rect.ReleaseMouseCapture();
        }

        private void rect_MouseMove(object sender, MouseEventArgs e) {
            if (!_isRectDragInProg) return;

            // get the position of the mouse relative to the Canvas
            var mousePos = e.GetPosition(mainCanvas);

            // center the rect on the mouse
            double left = mousePos.X - (rect.ActualWidth / 2);
            double top = mousePos.Y - (rect.ActualHeight / 2);
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
        }
    }
}
