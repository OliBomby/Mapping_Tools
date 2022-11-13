using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SlideratorStuff;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels
{
    public class SliderPicturatorVm : BindableBase
    {
        #region Properties

        private CancellationTokenSource previewTokenSource;
        private readonly object previewTokenLock = new();

        private bool _isProcessingPreview;
        [JsonIgnore]
        public bool IsProcessingPreview
        {
            get => _isProcessingPreview;
            set => Set(ref _isProcessingPreview, value);
        }

        [JsonIgnore]
        public string[] Paths { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        private long _viewportSize;
        public long ViewportSize
        {
            get => _viewportSize;
            set => Set(ref _viewportSize, value);
        }

        [JsonIgnore]
        public IEnumerable<long> ViewportSizes => new List<long> { 16384, 32768 };

        private double _yResolution;
        public double YResolution
        {
            get => _yResolution;
            set => Set(ref _yResolution, value);
        }

        private double _sliderStartX;
        public double SliderStartX
        {
            get => _sliderStartX;
            set => Set(ref _sliderStartX, value);
        }

        private double _sliderStartY;
        public double SliderStartY
        {
            get => _sliderStartY;
            set => Set(ref _sliderStartY, value);
        }

        private double _imageStartX;
        public double ImageStartX
        {
            get => _imageStartX;
            set => Set(ref _imageStartX, value);
        }

        private double _imageStartY;
        public double ImageStartY
        {
            get => _imageStartY;
            set => Set(ref _imageStartY, value);
        }

        [JsonIgnore]
        public IEnumerable<Color> AvailableColors {
            get
            {
                try {
                    var path = MainWindow.AppWindow.GetCurrentMaps()[0];
                    var beatmap = new BeatmapEditor(IOHelper.GetCurrentBeatmapOrCurrentBeatmap()).Beatmap;
                    var comboColors = beatmap.ComboColours;

                    if (comboColors.Count == 0) {
                        comboColors = ComboColour.GetDefaultComboColours().ToList();
                    }

                    var availableColors = comboColors.Select(comboColor => Color.FromArgb(comboColor.Color.R, comboColor.Color.G, comboColor.Color.B));
                    if (beatmap.SpecialColours.ContainsKey("SliderTrackOverride")) {
                        var tempColor = beatmap.SpecialColours["SliderTrackOverride"].Color;
                        availableColors = availableColors.Append(Color.FromArgb(tempColor.R, tempColor.G, tempColor.B));
                    }

                    return availableColors;
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                    return Enumerable.Empty<Color>();
                }
            }
        }


        private bool _useMapComboColors;

        public bool UseMapComboColors
        {
            get => _useMapComboColors;
            set
            {
                if (Set(ref _useMapComboColors, value)) {
                    if (value) {
                        CurrentTrackColor = ComboColor;
                    } else {
                        CurrentTrackColor = Color.FromArgb(TrackColorPickerColor.R, TrackColorPickerColor.G, TrackColorPickerColor.B); ;
                    }
                    RaisePropertyChanged(nameof(ShouldShowCCPicker));
                    RaisePropertyChanged(nameof(ShouldShowPalette));
                }
            }
        }

        private Color _currentTrackColor;
        public Color CurrentTrackColor
        {
            get => _currentTrackColor;
            set
            {
                if (Set(ref _currentTrackColor, value)) {
                    RegeneratePreview();
                }
            }
        }

        private Color _comboColor;
        public Color ComboColor
        {
            get => _comboColor;
            set
            {
                if (Set(ref _comboColor, value) && UseMapComboColors) {
                    CurrentTrackColor = value;
                    RaisePropertyChanged(nameof(PickedComboColor));
                }
            }
        }

        [JsonIgnore]
        public string PickedComboColor => ColorTranslator.ToHtml(ComboColor);

        private System.Windows.Media.Color _trackColorPickerColor;

        public System.Windows.Media.Color TrackColorPickerColor
        {
            get => _trackColorPickerColor;
            set {
                if(Set(ref _trackColorPickerColor, value) && !UseMapComboColors) {
                    CurrentTrackColor = Color.FromArgb(value.R, value.G, value.B);
                    RegeneratePreview();
                }
            }
        }

        private System.Windows.Media.Color _borderColor;

        public System.Windows.Media.Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (Set(ref _borderColor, value)) {
                    RegeneratePreview();
                }
            }
        }

        [JsonIgnore]
        public Visibility ShouldShowCCPicker => UseMapComboColors ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility ShouldShowPalette => UseMapComboColors ? Visibility.Collapsed : Visibility.Visible;

        private int _timeCode;
        public int TimeCode
        {
            get => _timeCode;
            set => Set(ref _timeCode, value);
        }

        private double _duration;
        public double Duration
        {
            get => _duration;
            set => Set(ref _duration, value);
        }

        [JsonIgnore]
        public CommandImplementation UploadFileCommand { get; }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private Bitmap _bm;
        [JsonIgnore]
        public Bitmap BM
        {
            get => _bm;
            set => Set(ref _bm, value);
        }

        private InteropBitmap _bmImage;
        [JsonIgnore]
        public InteropBitmap BMImage
        {
            get => _bmImage;
            set => Set(ref _bmImage, value);
        }

        private string _pictureFile;
        public string PictureFile
        {
            get => _pictureFile;
            set {
                Set(ref _pictureFile, value);
                BM = new Bitmap(value);
                RaisePropertyChanged(nameof(BM));
                RegeneratePreview();
            }
        }

        private bool _blackOn;
        public bool BlackOn
        {
            get => _blackOn;
            set {
                if (Set(ref _blackOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool _borderOn;
        public bool BorderOn
        {
            get => _borderOn;
            set {
                if (Set(ref _borderOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool _redOn;
        public bool RedOn
        {
            get => _redOn;
            set {
                if (Set(ref _redOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool _greenOn;
        public bool GreenOn
        {
            get => _greenOn;
            set {
                if (Set(ref _greenOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool _blueOn;
        public bool BlueOn
        {
            get => _blueOn;
            set {
                if (Set(ref _blueOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool _alphaOn;
        public bool AlphaOn
        {
            get => _alphaOn;
            set {
                if (Set(ref _alphaOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        #endregion

        public void RegeneratePreview() {
            if (BM == null) {
                return;
            }
            CancellationToken ct;
            lock (previewTokenLock) {
                if (previewTokenSource is not null) {
                    previewTokenSource.Cancel();
                    previewTokenSource.Dispose();
                }
                previewTokenSource = new CancellationTokenSource();
                ct = previewTokenSource.Token;
            }

            // Raise property changed for the load indicator in the preview
            IsProcessingPreview = true;
            Bitmap bm = (Bitmap)BM.Clone();
            Color ctc = Color.FromArgb(CurrentTrackColor.ToArgb());
            Color bc = Color.FromArgb(BorderColor.R, BorderColor.G, BorderColor.B);
            Task.Run(() => {
                Bitmap newBM = SliderPicturator.Recolor(bm, ctc, bc, Color.FromArgb(0, 0, 0), !BlackOn, !BorderOn, !AlphaOn, RedOn, GreenOn, BlueOn);
                // Send the new preview to the main thread
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    IntPtr hBitmap = newBM.GetHbitmap();
                    InteropBitmap retval;

                    try {
                        retval = (InteropBitmap)Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                    finally {
                        DeleteObject(hBitmap);
                    }
                    BMImage = retval;
                    RaisePropertyChanged(nameof(BMImage));
                });

            }, ct).ContinueWith(task => {
                // Show the error if one occured while generating preview
                if (task.IsFaulted) {
                    task.Exception.Show();
                }
                // Stop the processing indicator
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    IsProcessingPreview = false;
                });
            }, ct);
        }

        private void HandleCurrentBeatmapUpdate(object sender, string currentBeatmaps)
        {
            RaisePropertyChanged(nameof(AvailableColors));
        }

        public SliderPicturatorVm()
        {
            MainWindow.AppWindow.OnUpdateCurrentBeatmap += HandleCurrentBeatmapUpdate;
            ViewportSize = 16384;
            TimeCode = 0;
            Duration = 1;
            YResolution = 1080;
            SliderStartX = 256;
            SliderStartY = 192;
            ImageStartX = 0;
            ImageStartY = 0;
            BlackOn = true;
            BorderOn = true;
            RedOn = true;
            GreenOn = true;
            BlueOn = true;
            AlphaOn = true;
            UseMapComboColors = false;
            ComboColor = Color.FromArgb(0, 0, 0);
            TrackColorPickerColor = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
            BorderColor = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
            BMImage = null;
            BM = null;

            UploadFileCommand = new CommandImplementation(_ => SetFile());
        }

        private void SetFile()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".png"; // Required file extension
            fileDialog.Filter = @"All Image Files|*.BMP;*.bmp;*.JPG;*.JPEG*.jpg;*.jpeg;*.PNG;*.png;*.GIF;*.gif;*.tif;*.tiff;*.ico;*.ICO|PNG|*.PNG;*.png|JPEG|*.JPG;*.JPEG*.jpg;*.jpeg|Bitmap(.BMP,.bmp)|*.BMP;*.bmp|GIF|*.GIF;*.gif|TIF|*.tif;*.tiff|ICO|*.ico;*.ICO";// Optional file extensions

            if (fileDialog.ShowDialog() == DialogResult.OK) {
                PictureFile = fileDialog.FileName;
            }
        }
    }
    
}