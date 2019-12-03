using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Anchor.xaml
    /// </summary>
    public partial class Anchor {
        protected override double DefaultSize { get; } = 14;

        [CanBeNull]
        public TensionAnchor TensionAnchor { get; set; }

        [NotNull]
        public IGraphInterpolator Interpolator { get => _interpolator;
            set => SetInterpolator(value);
        }

        private IGraphInterpolator _interpolator;

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

        [CanBeNull]
        public Anchor PreviousAnchor { get; set; }

        [CanBeNull]
        public Anchor NextAnchor { get; set; }

        public Anchor(Graph parent, Vector2 pos) : this(parent, pos, InterpolatorHelper.GetName(typeof(SingleCurveInterpolator))) { }

        public Anchor(Graph parent, Vector2 pos, string interpolator) : base(parent, pos) {
            InitializeComponent();
            SetCursor();
            PopulateContextMenu();
            SetInterpolator(interpolator);
        }

        private void SetCursor() {
            Cursor = Cursors.Cross;
        }

        public override void EnableDragging() {
            base.EnableDragging();

            SizeMultiplier = 1.25;
        }

        public override void DisableDragging() {
            base.DisableDragging();

            SizeMultiplier = 1;
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

            if (PreviousAnchor == null) {
                foreach (var item in GetContextMenu().Items) {
                    if (!(item is MenuItem menuItem) || !(menuItem.Tag is string)) continue;
                    menuItem.IsEnabled = false;
                    menuItem.Icon = null;
                }
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

        private MenuItem GetTypeInMenuItem() {
            return FindResource("TypeInMenuItem") as MenuItem;
        }

        private void PopulateContextMenu() {
            var cm = GetContextMenu();
            cm.Items.Add(GetDeleteMenuItem());
            cm.Items.Add(new Separator());

            foreach (var interpolator in InterpolatorHelper.GetInterpolators()) {
                var name = InterpolatorHelper.GetName(interpolator);
                var menuItem = new MenuItem {Header = name, Icon = new PackIcon {Kind = PackIconKind.RadioboxBlank}, Tag = name};
                menuItem.Click += MenuItem_OnClick;
                cm.Items.Add(menuItem);
            }

            cm.Items.Add(new Separator());
            cm.Items.Add(GetTypeInMenuItem());
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menu && menu.Tag is string name) {
                SetInterpolator(name);
            }
        }

        private void SetInterpolator(string name) {
            SetInterpolator(InterpolatorHelper.GetInterpolator(name));
        }

        private void SetInterpolator(IGraphInterpolator p) {
            if (_interpolator != null && _interpolator == p) return;

            var name = InterpolatorHelper.GetName(p.GetType());

            Graph.LastInterpolationSet = name;

            var cm = GetContextMenu();
            var items = cm.Items;
            foreach (var item in items) {
                if (item is MenuItem mi && mi.Icon != null) {
                    mi.Icon = mi.Tag.ToString() == name ? 
                        new PackIcon {Kind = PackIconKind.RadioboxMarked} : 
                        new PackIcon {Kind = PackIconKind.RadioboxBlank};
                }
            }

            _interpolator = p;
            Graph.UpdateVisual();
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e) {
            Graph.RemoveAnchor(GetContextMenu().PlacementTarget as Anchor);
        }

        private void TypeInMenuItem_OnClick(object sender, RoutedEventArgs e) {
            Graph.OpenTypeValueDialog(this);
        }
    }
}
