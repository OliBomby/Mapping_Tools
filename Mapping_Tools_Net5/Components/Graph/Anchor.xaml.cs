using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Components.Dialogs;
using Newtonsoft.Json;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using MenuItem = System.Windows.Controls.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Anchor.xaml
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public partial class Anchor : IGraphAnchor {
        protected override double DefaultSize { get; } = 12;

        public event DependencyPropertyChangedEventHandler GraphStateChangedEvent;

        public static readonly DependencyProperty PosProperty =
            DependencyProperty.Register(nameof(Pos),
                typeof(Vector2), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(Vector2.Zero, FrameworkPropertyMetadataOptions.None,
                    OnPosChanged));

        /// <summary>
        /// Ranges from (0,0) bottom left to (1,1) top right
        /// </summary>
        [JsonProperty]
        public sealed override Vector2 Pos {
            get => (Vector2) GetValue(PosProperty);
            set => SetValue(PosProperty, value);
        }

        private static void OnPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null) return;
            var a = (Anchor) d;
            a.GraphStateChangedEvent?.Invoke(d, e);
        }
        
        public static readonly DependencyProperty TensionAnchorProperty =
            DependencyProperty.Register(nameof(TensionAnchor),
                typeof(TensionAnchor), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));
        
        [NotNull]
        [JsonProperty]
        public TensionAnchor TensionAnchor {
            get => (TensionAnchor) GetValue(TensionAnchorProperty);
            set => SetValue(TensionAnchorProperty, value);
        }
        
        public static readonly DependencyProperty InterpolatorProperty =
            DependencyProperty.Register(nameof(Interpolator),
                typeof(IGraphInterpolator), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnInterpolatorChanged));
        
        [NotNull]
        [JsonProperty]
        public IGraphInterpolator Interpolator {
            get => (IGraphInterpolator) GetValue(InterpolatorProperty);
            set => SetValue(InterpolatorProperty, value);
        }

        private static void OnInterpolatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null) return;
            if (e.NewValue == null) return;
            var a = (Anchor) d;
            a.UpdateInterpolatorStuff();
            a.GraphStateChangedEvent?.Invoke(d, e);
        }
        
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke),
                typeof(Brush), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnStrokeChanged));
        
        [JsonProperty]
        public sealed override Brush Stroke {
            get => (Brush) GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null) return;
            var a = (Anchor) d;
            a.MainShape.Stroke = (Brush) e.NewValue;
        }
        
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill),
                typeof(Brush), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnFillChanged));
        
        [JsonProperty]
        public sealed override Brush Fill {
            get => (Brush) GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null) return;
            var a = (Anchor) d;
            a.MainShape.Fill = (Brush) e.NewValue;
        }
        
        public static readonly DependencyProperty TensionProperty =
            DependencyProperty.Register(nameof(Tension),
                typeof(double), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None,
                    OnTensionChanged));
        
        [JsonProperty]
        public sealed override double Tension {
            get => (double) GetValue(TensionProperty);
            set => SetValue(TensionProperty, value);
        }

        private static void OnTensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null) return;
            var a = (Anchor) d;
            a.TensionAnchor.Tension = (double) e.NewValue;
            a.Interpolator.P = (double) e.NewValue;
            a.GraphStateChangedEvent?.Invoke(d, e);
        }
        
        public static readonly DependencyProperty PreviousAnchorProperty =
            DependencyProperty.Register(nameof(PreviousAnchor),
                typeof(Anchor), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));
        
        [CanBeNull]
        public Anchor PreviousAnchor {
            get => (Anchor) GetValue(PreviousAnchorProperty);
            set => SetValue(PreviousAnchorProperty, value);
        }
        
        public static readonly DependencyProperty NextAnchorProperty =
            DependencyProperty.Register(nameof(NextAnchor),
                typeof(Anchor), 
                typeof(Anchor), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));
        
        [CanBeNull]
        public Anchor NextAnchor {
            get => (Anchor) GetValue(NextAnchorProperty);
            set => SetValue(NextAnchorProperty, value);
        }

        public Anchor(Graph parent, Vector2 pos) : this(parent, pos, null) { }

        public Anchor(Graph parent, Vector2 pos, IGraphInterpolator interpolator) : this(parent, pos, interpolator, null) { }

        [JsonConstructor]
        public Anchor(Graph parent, Vector2 pos, IGraphInterpolator interpolator, TensionAnchor tensionAnchor) : base(parent) {
            InitializeComponent();
            SetCursor();
            PopulateContextMenu();
            Pos = pos;
            TensionAnchor = tensionAnchor ?? new TensionAnchor(Graph, pos, this);
            TensionAnchor.ParentAnchor = this;
            TensionAnchor.Graph = Graph;
            Interpolator = interpolator ?? new SingleCurveInterpolator();
            if (Interpolator is CustomInterpolator c) {
                Tension = c.P;
            }
            Stroke = parent?.AnchorStroke;
            Fill = parent?.AnchorFill;
        }

        public AnchorState GetAnchorState() {
            return new AnchorState {Interpolator = Interpolator, Pos = Pos, Tension = Tension};
        }

        public void SetAnchorState(AnchorState anchorState) {
            Pos = anchorState.Pos;
            Interpolator = anchorState.Interpolator;
            Tension = anchorState.Tension;
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

            var movement = Graph.GetValueRelative(new Point(drag.X, drag.Y));
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
                var menuItem = new MenuItem {Header = name, Icon = new PackIcon {Kind = PackIconKind.RadioboxBlank}, Tag = interpolator};
                menuItem.Click += MenuItem_OnClick;
                cm.Items.Add(menuItem);
            }

            cm.Items.Add(new Separator());
            cm.Items.Add(GetTypeInMenuItem());
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menu && menu.Tag is Type interpolator) {
                SetInterpolator(interpolator);
            }
        }

        private void SetInterpolator(Type type) {
            Interpolator = InterpolatorHelper.GetInterpolator(type);
        }

        private void UpdateInterpolatorStuff() {
            var type = Interpolator.GetType();

            var cm = GetContextMenu();
            var items = cm.Items;
            foreach (var item in items) {
                if (item is MenuItem mi && mi.Icon != null && mi.Tag is Type interpolator) {
                    mi.Icon = interpolator == type ? 
                        new PackIcon {Kind = PackIconKind.RadioboxMarked} : 
                        new PackIcon {Kind = PackIconKind.RadioboxBlank};
                }
            }

            if (Graph == null) return;
            Graph.LastInterpolationSet = type;
            Graph.UpdateVisual();
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e) {
            Graph.RemoveAnchor(GetContextMenu().PlacementTarget as Anchor);
        }

        private async void TypeInMenuItem_OnClick(object sender, RoutedEventArgs e) {
            var dialog = new TypeValueDialog(Pos.Y);
            var result = await Graph.GraphDialogHost.ShowDialog(dialog);

            if (!(bool) result) return;
        
            if (TypeConverters.TryParseDouble(dialog.ValueBox.Text, out double value)) {
                Pos = new Vector2(Pos.X, value);
            }
            Graph.UpdateVisual();
        }
    }
}
