using System;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.MathUtil;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Anchor.xaml
    /// </summary>
    public partial class Anchor {
        private const double AnchorSize = 12;
        private const double TensionAnchorSize = 7;

        private bool _isDragging;
        private bool _ignoreDrag;

        public readonly bool IsTensionPoint;

        /// <summary>
        /// Ranges from (0,0) bottom left to (1,1) top right
        /// </summary>
        public Vector2 Pos { get; set; }

        public Graph Graph { get; set; }

        [CanBeNull]
        public Anchor LinkedAnchor { get; set; }

        /// <summary>
        /// Goes from -1 to 1
        /// </summary>
        public double Tension { get; set; }

        private Brush _stroke;
        public Brush Stroke {
            get => _stroke;
            set { 
                _stroke = value;
                MainShape.Stroke = value;
                if (_isDragging && IsTensionPoint) {
                    MainShape.Fill = value;
                }
            }
        }

        private Brush _fill;
        public Brush Fill {
            get => _fill;
            set {
                _fill = value;
                if (!(_isDragging && IsTensionPoint)) {
                    MainShape.Fill = value;
                }
            }
        }

        public Anchor(Graph parent, Vector2 pos, bool isTensionPoint = false) {
            InitializeComponent();
            IsTensionPoint = isTensionPoint;
            Graph = parent;
            Pos = pos;
            SetDimensions();
            SetCursor();
            PopulateContextMenu();
        }

        private void SetDimensions() {
            SetSize(IsTensionPoint ? TensionAnchorSize : AnchorSize);
        }

        private void SetSize(double size) {
            Width = size;
            Height = size;
            Graph.UpdateVisual();
        }

        private void SetCursor() {
            Cursor = IsTensionPoint ? Cursors.SizeNS : Cursors.Cross;
        }

        private void ThisLeftMouseDown(object sender, MouseButtonEventArgs e) {
            // Move the cursor to the middle of this anchor
            MoveCursorToThis(GetRelativeCursorPosition(e));

            EnableDragging();
            e.Handled = true;
        }

        public void EnableDragging() {
            if (IsTensionPoint) {
                Cursor = Cursors.None;
                MainShape.Fill = Stroke;
                Graph.UpdateVisual();
            }

            CaptureMouse();
            _isDragging = true;
        }

        public void DisableDragging() {
            if (IsTensionPoint) {
                SetCursor();
                SetDimensions();
                MainShape.Fill = Fill;
                Graph.UpdateVisual();
            }

            ReleaseMouseCapture();
            _isDragging = false;
        }

        private static void MoveCursorToThis(Vector relativeCursorPosition) {
            // Cursor position relative to center of this anchor
            var relativePos = FromDpi(relativeCursorPosition);
            // Cursor position on screen
            var cursorPos = System.Windows.Forms.Cursor.Position;
            // New cursor position on screen
            var newCursorPos = new System.Drawing.Point(cursorPos.X - (int)Math.Round(relativePos.X), cursorPos.Y - (int)Math.Round(relativePos.Y));
            // Set new cursor position
            System.Windows.Forms.Cursor.Position = newCursorPos;
        }

        private static Vector2 FromDpi(Vector vector) {
            var source = PresentationSource.FromVisual(MainWindow.AppWindow);
            if (source == null) return new Vector2(vector.X, vector.Y);
            if (source.CompositionTarget == null) return new Vector2(vector.X, vector.Y);
            var dpiX = source.CompositionTarget.TransformToDevice.M11;
            var dpiY = source.CompositionTarget.TransformToDevice.M22;
            return new Vector2(vector.X * dpiX, vector.Y * dpiY);
        }

        private Vector GetRelativeCursorPosition(MouseButtonEventArgs e) {
            return e.MouseDevice.GetPosition(this) - new Point(Width / 2, Height / 2);
        }

        private Vector GetRelativeCursorPosition(MouseEventArgs e) {
            return e.MouseDevice.GetPosition(this) - new Point(Width / 2, Height / 2);
        }

        private void ThisMouseUp(object sender, MouseButtonEventArgs e) {
            DisableDragging();
            e.Handled = true;
        }

        private void ThisMouseMove(object sender, MouseEventArgs e) {
            if (_ignoreDrag) {
                _ignoreDrag = false;
                return;
            }

            if (!_isDragging) return;

            if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) DisableDragging();

            // Get the position of the mouse relative to the Canvas
            var diff = GetRelativeCursorPosition(e);

            if (IsTensionPoint) {
                // Ctrl on tension point makes it more precise
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                    diff.Y /= 10;
                }
            } else {
                // Shift makes it move horizontally
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    diff.Y = 0;
                }
                // Ctrl makes it move vertically
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                    diff.X = 0;
                }
            }

            // Let the graph move this anchor
            Graph.MoveAnchor(this, diff);

            // If this is a tension point move the cursor to this
            if (IsTensionPoint) {
                _ignoreDrag = true;
                MoveCursorToThis(GetRelativeCursorPosition(e));
            }

            e.Handled = true;
        }

        private void Anchor_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (IsTensionPoint) {
                LinkedAnchor?.ResetTension();
                Graph.UpdateVisual();
            } else {
                var cm = GetContextMenu();
                var deleteMenuItem = GetDeleteMenuItem();
                if (deleteMenuItem != null) {
                    GetDeleteMenuItem().IsEnabled = !Graph.IsEdgeAnchor(this);
                }
                cm.PlacementTarget = sender as Anchor;
                cm.IsOpen = true;
            }

            e.Handled = true;
        }

        private ContextMenu GetContextMenu() {
            return FindResource("ContextMenu") as ContextMenu;
        }

        private MenuItem GetDeleteMenuItem() {
            return FindResource("DeleteMenuItem") as MenuItem;
        }

        private void PopulateContextMenu() {
            var cm = GetContextMenu();
            cm.Items.Add(GetDeleteMenuItem());
            cm.Items.Add(new Separator());
            for (int i = 0; i < 5; i++) {
                var menuItem = new MenuItem {Header = $"Mode {i + 1}", Icon = new PackIcon {Kind = PackIconKind.RadioboxBlank}};
                menuItem.Click += MenuItem_OnClick;
                cm.Items.Add(menuItem);
            }
        }

        public void ResetTension() {
            SetTension(0);
        }

        public void SetTension(double tension) {
            Tension = tension;
            if (LinkedAnchor != null)
                LinkedAnchor.Tension = tension;

            if (_isDragging) {
                SetSize(TensionAnchorSize * Math.Pow(1.5, Math.Min(Math.Abs(Tension) - 1, 1)));
            } else {
                Graph.UpdateVisual();
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            var cm = GetContextMenu();
            var items = cm.Items;
            foreach (var item in items) {
                if (item is MenuItem mi && mi.Icon != null) {
                    mi.Icon = new PackIcon {Kind = PackIconKind.RadioboxBlank};
                }
            }

            if (sender is MenuItem menu) menu.Icon = new PackIcon {Kind = PackIconKind.RadioboxMarked};
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e) {
            Graph.RemoveAnchor(GetContextMenu().PlacementTarget as Anchor);
        }
    }
}
