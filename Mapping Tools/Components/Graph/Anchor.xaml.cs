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
        protected override double DefaultSize { get; } = 10;

        [CanBeNull]
        public TensionAnchor TensionAnchor { get; set; }

        private Brush _stroke;
        public override Brush Stroke {
            get => _stroke;
            set { 
                _stroke = value;
                MainShape.Stroke = value;
            }
        }

        private Brush _fill;
        public override Brush Fill {
            get => _fill;
            set {
                _fill = value;
                MainShape.Fill = value;
            }
        }

        private double _tension;
        public override double Tension {
            get => _tension;
            set {
                if (Math.Abs(_tension - value) < Precision.DOUBLE_EPSILON) return;
                _tension = value;
                if (TensionAnchor != null)
                    TensionAnchor.Tension = value;
            }
        }

        public Anchor(Graph parent, Vector2 pos) : base(parent, pos) {
            InitializeComponent();
            SetCursor();
            PopulateContextMenu();
        }

        private void SetCursor() {
            Cursor = Cursors.Cross;
        }

        protected override void OnDrag(Vector2 drag, MouseEventArgs e) {
            // Shift makes it move horizontally
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                drag.Y = 0;
            }
            // Ctrl makes it move vertically
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                drag.X = 0;
            }

            var movement = new Vector2(drag.X / Graph.Width, -drag.Y / Graph.Height);
            Graph.MoveAnchorTo(this, Pos + movement);
        }

        private void Anchor_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var cm = GetContextMenu();

            var deleteMenuItem = GetDeleteMenuItem();
            if (deleteMenuItem != null) {
                GetDeleteMenuItem().IsEnabled = !Graph.IsEdgeAnchor(this);
            }

            cm.PlacementTarget = sender as Anchor;
            cm.IsOpen = true;

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
