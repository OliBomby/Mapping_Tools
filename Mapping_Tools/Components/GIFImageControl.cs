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


        public static readonly DependencyProperty GifSourceProperty =
            DependencyProperty.Register("GifSource", typeof(string), typeof(GIFImageControl),
                new UIPropertyMetadata("", GIFSource_Changed));


        public static readonly DependencyProperty PlayAnimationProperty =
            DependencyProperty.Register("PlayAnimation", typeof(bool), typeof(GIFImageControl),
                new UIPropertyMetadata(true, PlayAnimation_Changed));


        private Bitmap bitmap;


        private bool mouseClickStarted;


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


        public string GifSource {
            get => (string) GetValue(GifSourceProperty);

            set => SetValue(GifSourceProperty, value);
        }


        private void GIFImageControl_Click(object sender, RoutedEventArgs e) {
            if (AllowClickToPause)

                PlayAnimation = !PlayAnimation;
        }


        private void GIFImageControl_MouseLeave(object sender, MouseEventArgs e) {
            mouseClickStarted = false;
        }


        private void GIFImageControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (mouseClickStarted)

                FireClickEvent(sender, e);

            mouseClickStarted = false;
        }


        private void GIFImageControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            mouseClickStarted = true;
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

                if (null != gic.bitmap)

                    ImageAnimator.Animate(gic.bitmap, gic.OnFrameChanged);
            }
            else

                //Pause Animation

            {
                ImageAnimator.StopAnimate(gic.bitmap, gic.OnFrameChanged);
            }
        }


        private void SetImageGifSource() {
            if (null != bitmap) {
                ImageAnimator.StopAnimate(bitmap, OnFrameChanged);

                bitmap = null;
            }

            if (string.IsNullOrEmpty(GifSource)) {
                //Turn off if GIF set to null or empty

                Source = null;

                InvalidateVisual();

                return;
            }

            if (File.Exists(GifSource)) {
                bitmap = (Bitmap) System.Drawing.Image.FromFile(GifSource);
            }

            else {
                //Support looking for embedded resources

                var assemblyToSearch = Assembly.GetAssembly(GetType());

                bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);

                if (null == bitmap) {
                    assemblyToSearch = Assembly.GetCallingAssembly();

                    bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);

                    if (null == bitmap) {
                        assemblyToSearch = Assembly.GetEntryAssembly();

                        bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);

                        if (null == bitmap)

                            throw new FileNotFoundException("GIF Source was not found.", GifSource);
                    }
                }
            }

            if (PlayAnimation)
                ImageAnimator.Animate(bitmap, OnFrameChanged);
        }


        private Bitmap GetBitmapResourceFromAssembly(Assembly assemblyToSearch) {
            var resourselist = assemblyToSearch.GetManifestResourceNames().ToList();

            if (null != assemblyToSearch.FullName) {
                var searchName = $"Mapping_Tools.{GifSource}";

                if (resourselist.Contains(searchName)) {
                    var bitmapStream = assemblyToSearch.GetManifestResourceStream(searchName);

                    if (null != bitmapStream)

                        return (Bitmap) System.Drawing.Image.FromStream(bitmapStream);
                }
            }

            return null;
        }


        private static void GIFSource_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((GIFImageControl) d).SetImageGifSource();
        }


        private void OnFrameChanged(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new OnFrameChangedDelegate(OnFrameChangedInMainThread));
        }


        private void OnFrameChangedInMainThread() {
            if (PlayAnimation) {
                ImageAnimator.UpdateFrames(bitmap);

                Source = GetBitmapSource(bitmap);

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