using Mapping_Tools.Classes.MathUtil;
using Overlay.NET.Common;
using Overlay.NET.Wpf;
using Process.NET.Windows;
using System;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.Tools.SnappingTools;
using OverlayWindow = Overlay.NET.Wpf.OverlayWindow;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// 
    /// </summary>
    public class SnappingToolsOverlay : WpfOverlayPlugin {
        // Used to limit update rates via timestamps 
        // This way we can avoid thread issues with wanting to delay updates
        private readonly TickEngine _tickEngine = new TickEngine();
        private bool _isDisposed;

        public CoordinateConverter Converter;

        public override void Initialize(IWindow targetWindow) {
            // Set target window by calling the base method
            base.Initialize(targetWindow);

            OverlayWindow = new OverlayWindow(targetWindow) {ShowInTaskbar = false};

            _tickEngine.Interval = TimeSpan.FromMilliseconds(1000 / 60f);
            _tickEngine.PreTick += OnPreTick;
            _tickEngine.Tick += OnTick;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        public void SetBorder(bool enabled) {
            if (_isDisposed) return;

            if (enabled) {
                OverlayWindow.BorderBrush = Brushes.GreenYellow;
                OverlayWindow.BorderThickness = new Thickness(3);
            } else {
                OverlayWindow.BorderBrush = Brushes.Transparent;
                OverlayWindow.BorderThickness = new Thickness(0);
            }
        }

        public override void Enable() {
            _tickEngine.IsTicking = true;
            base.Enable();
        }

        public override void Disable() {
            _tickEngine.IsTicking = false;
            base.Disable();
        }

        private void OnTick(object sender, EventArgs eventArgs) {
            // This will only be true if the target window is active
            // (or very recently has been, depends on your update rate)
            if (!OverlayWindow.IsVisible) return;

            var bounds = Converter.GetEditorBox();

            var topLeft = Converter.ToDpi(new Vector2(bounds.Left, bounds.Top));
            var bottomRight = Converter.ToDpi(new Vector2(bounds.Right, bounds.Bottom));
            OverlayWindow.Left = topLeft.X;
            OverlayWindow.Top = topLeft.Y;
            OverlayWindow.Width = Math.Abs(bottomRight.X - topLeft.X);
            OverlayWindow.Height = Math.Abs(bottomRight.Y - topLeft.Y);
        }

        private void OnPreTick(object sender, EventArgs eventArgs) {
            var activated = TargetWindow.IsActivated;
            var visible = OverlayWindow.IsVisible;

            // Ensure window is shown or hidden correctly prior to updating
            if (!activated && visible) {
                OverlayWindow.Hide();
            } else if (activated && !visible) {
                OverlayWindow.Show();
            }
        }

        public override void Update() => _tickEngine.Pulse();

        // Clear objects
        public override void Dispose() {
            if (_isDisposed) {
                return;
            }

            try {
                if (IsEnabled) {
                    Disable();
                }

                OverlayWindow?.Hide();
                OverlayWindow?.Close();
                OverlayWindow = null;
                _tickEngine.Stop();

                base.Dispose();
                _isDisposed = true;
            } catch {
                // ignored
            }
        }

        /// <summary>Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.</summary>
        ~SnappingToolsOverlay() => Dispose();
    }
}
