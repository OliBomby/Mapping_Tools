using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools;
using Overlay.NET.Common;
using Overlay.NET.Wpf;
using Process.NET.Windows;
using System;
using OverlayWindow = Overlay.NET.Wpf.OverlayWindow;

namespace Mapping_Tools.Views.SnappingTools {
    public class SnappingToolsOverlay : WpfOverlayPlugin {
        // Used to limit update rates via timestamps 
        // This way we can avoid thread issues with wanting to delay updates
        private readonly TickEngine _tickEngine = new TickEngine();
        private bool _isDisposed;

        public CoordinateConverter Converter;

        public override void Initialize(IWindow targetWindow) {
            // Set target window by calling the base method
            base.Initialize(targetWindow);

            OverlayWindow = new OverlayWindow(targetWindow);
#if DEBUG
            OverlayWindow.BorderBrush = Brushes.Blue;
            OverlayWindow.BorderThickness = new Thickness(3);
#endif
            
            _tickEngine.Interval = TimeSpan.FromMilliseconds(1000 / 60f);
            _tickEngine.PreTick += OnPreTick;
            _tickEngine.Tick += OnTick;
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
            if (OverlayWindow.IsVisible) {
                OverlayWindow.Update();

                Converter.OsuWindowPosition = new Vector2(OverlayWindow.Left, OverlayWindow.Top);
            }
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

            if (IsEnabled) {
                Disable();
            }

            OverlayWindow?.Hide();
            OverlayWindow?.Close();
            OverlayWindow = null;
            _tickEngine.Stop();

            base.Dispose();
            _isDisposed = true;
        }

        ~SnappingToolsOverlay() {
            Dispose();
        }
    }
}