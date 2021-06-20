using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image = System.Windows.Controls.Image;

namespace Mapping_Tools.Components {
    public class GIFImageControl : Image {
        public static readonly DependencyProperty AllowClickToPauseProperty =
            DependencyProperty.Register("AllowClickToPause", typeof(bool), typeof(GIFImageControl),
                new UIPropertyMetadata(true));


        public static readonly DependencyProperty GIFSourceProperty =
            DependencyProperty.Register("GIFSource", typeof(string), typeof(GIFImageControl),
                new UIPropertyMetadata("", GIFSource_Changed));


        public static readonly DependencyProperty PlayAnimationProperty =
            DependencyProperty.Register("PlayAnimation", typeof(bool), typeof(GIFImageControl),
                new UIPropertyMetadata(true, PlayAnimation_Changed));


        private Bitmap _Bitmap;


        private bool _mouseClickStarted;


        public GIFImageControl() {
            MouseLeftButtonDown += GIFImageControl_MouseLeftButtonDown;

            MouseLeftButtonUp += GIFImageControl_MouseLeftButtonUp;

            MouseLeave += GIFImageControl_MouseLeave;

            Click += GIFImageControl_Click;

            //TODO:Future feature: Add a Play/Pause graphic on mouse over, and possibly a context menu
        }


        public bool AllowClickToPause {
            get => (bool) GetValue(AllowClickToPauseProperty);

            set => SetValue(AllowClickToPauseProperty, value);
        }


        public bool PlayAnimation {
            get => (bool) GetValue(PlayAnimationProperty);

            set => SetValue(PlayAnimationProperty, value);
        }


        public string GIFSource {
            get => (string) GetValue(GIFSourceProperty);

            set => SetValue(GIFSourceProperty, value);
        }


        private void GIFImageControl_Click(object sender, RoutedEventArgs e) {
            if (AllowClickToPause)

                PlayAnimation = !PlayAnimation;
        }


        private void GIFImageControl_MouseLeave(object sender, MouseEventArgs e) {
            _mouseClickStarted = false;
        }


        private void GIFImageControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (_mouseClickStarted)

                FireClickEvent(sender, e);

            _mouseClickStarted = false;
        }


        private void GIFImageControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            _mouseClickStarted = true;
        }


        private void FireClickEvent(object sender, RoutedEventArgs e) {
            if (null != Click)

                Click(sender, e);
        }


        public event RoutedEventHandler Click;


        private static void PlayAnimation_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var gic = (GIFImageControl) d;

            if ((bool) e.NewValue) {
                //StartAnimation if GIFSource is properly set

                if (null != gic._Bitmap)

                    ImageAnimator.Animate(gic._Bitmap, gic.OnFrameChanged);
            }
            else

                //Pause Animation

            {
                ImageAnimator.StopAnimate(gic._Bitmap, gic.OnFrameChanged);
            }
        }


        private void SetImageGIFSource() {
            if (null != _Bitmap) {
                ImageAnimator.StopAnimate(_Bitmap, OnFrameChanged);

                _Bitmap = null;
            }

            if (string.IsNullOrEmpty(GIFSource)) {
                //Turn off if GIF set to null or empty

                Source = null;

                InvalidateVisual();

                return;
            }

            if (File.Exists(GIFSource)) {
                _Bitmap = (Bitmap) System.Drawing.Image.FromFile(GIFSource);
            }

            else {
                //Support looking for embedded resources

                var assemblyToSearch = Assembly.GetAssembly(GetType());

                _Bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);

                if (null == _Bitmap) {
                    assemblyToSearch = Assembly.GetCallingAssembly();

                    _Bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);

                    if (null == _Bitmap) {
                        assemblyToSearch = Assembly.GetEntryAssembly();

                        _Bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);

                        if (null == _Bitmap)

                            throw new FileNotFoundException("GIF Source was not found.", GIFSource);
                    }
                }
            }

            if (PlayAnimation)
                ImageAnimator.Animate(_Bitmap, OnFrameChanged);
        }


        private Bitmap GetBitmapResourceFromAssembly(Assembly assemblyToSearch) {
            var resourselist = assemblyToSearch.GetManifestResourceNames().ToList();

            if (null != assemblyToSearch.FullName) {
                var searchName = $"Mapping_Tools.{GIFSource}";

                if (resourselist.Contains(searchName)) {
                    var bitmapStream = assemblyToSearch.GetManifestResourceStream(searchName);

                    if (null != bitmapStream)

                        return (Bitmap) System.Drawing.Image.FromStream(bitmapStream);
                }
            }

            return null;
        }


        private static void GIFSource_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((GIFImageControl) d).SetImageGIFSource();
        }


        private void OnFrameChanged(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new OnFrameChangedDelegate(OnFrameChangedInMainThread));
        }


        private void OnFrameChangedInMainThread() {
            if (PlayAnimation) {
                ImageAnimator.UpdateFrames(_Bitmap);

                Source = GetBitmapSource(_Bitmap);

                InvalidateVisual();
            }
        }


        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        public static extern IntPtr DeleteObject(IntPtr hDc);


        private static BitmapSource GetBitmapSource(Bitmap gdiBitmap) {
            var hBitmap = gdiBitmap.GetHbitmap();

            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(hBitmap);

            return bitmapSource;
        }


        private delegate void OnFrameChangedDelegate();
    }
}