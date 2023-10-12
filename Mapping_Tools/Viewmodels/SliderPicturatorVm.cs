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
using Editor_Reader;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.SlideratorStuff;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Viewmodels
{
    public class SliderPicturatorVm : BindableBase
    {
        #region Properties

        private CancellationTokenSource previewTokenSource;
        private readonly object previewTokenLock = new();

        private bool isProcessingPreview;
        [JsonIgnore]
        public bool IsProcessingPreview
        {
            get => isProcessingPreview;
            set => Set(ref isProcessingPreview, value);
        }

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        private long viewportSize;
        public long ViewportSize
        {
            get => viewportSize;
            set => Set(ref viewportSize, value);
        }

        private int quality;
        public int Quality
        {
            get => quality;
            set
            {
                if (Set(ref quality, value)) {
                    RegeneratePreview();
                }
            }
        }

        private long segmentCount;
        public long SegmentCount
        {
            get => segmentCount;
            set => Set(ref segmentCount, value);
        }

        [JsonIgnore]
        public IEnumerable<long> ViewportSizes => new List<long> { 16384, 32768 };

        private double yResolution;
        public double YResolution
        {
            get => yResolution;
            set => Set(ref yResolution, value);
        }

        private double sliderStartX;
        public double SliderStartX
        {
            get => sliderStartX;
            set => Set(ref sliderStartX, value);
        }

        private double sliderStartY;
        public double SliderStartY
        {
            get => sliderStartY;
            set => Set(ref sliderStartY, value);
        }

        private double imageStartX;
        public double ImageStartX
        {
            get => imageStartX;
            set => Set(ref imageStartX, value);
        }

        private double imageStartY;
        public double ImageStartY
        {
            get => imageStartY;
            set => Set(ref imageStartY, value);
        }

        [JsonIgnore]
        public IEnumerable<Color> AvailableColors {
            get
            {
                try {
                    var path = MainWindow.AppWindow.GetCurrentMaps()[0];
                    var beatmap = new BeatmapEditor(path).Beatmap;
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


        private bool useMapComboColors;

        public bool UseMapComboColors
        {
            get => useMapComboColors;
            set
            {
                if (Set(ref useMapComboColors, value)) {
                    CurrentTrackColor = value ? ComboColor : Color.FromArgb(TrackColorPickerColor.R, TrackColorPickerColor.G, TrackColorPickerColor.B);
                    RaisePropertyChanged(nameof(ShouldShowCcPicker));
                    RaisePropertyChanged(nameof(ShouldShowPalette));
                }
            }
        }

        private Color currentTrackColor;
        public Color CurrentTrackColor
        {
            get => currentTrackColor;
            set
            {
                if (Set(ref currentTrackColor, value)) {
                    RegeneratePreview();
                }
            }
        }

        private Color comboColor;
        public Color ComboColor
        {
            get => comboColor;
            set
            {
                if (Set(ref comboColor, value) && UseMapComboColors) {
                    CurrentTrackColor = value;
                    RaisePropertyChanged(nameof(PickedComboColor));
                }
            }
        }

        [JsonIgnore]
        public string PickedComboColor => ColorTranslator.ToHtml(ComboColor);

        private System.Windows.Media.Color trackColorPickerColor;

        public System.Windows.Media.Color TrackColorPickerColor
        {
            get => trackColorPickerColor;
            set {
                if(Set(ref trackColorPickerColor, value) && !UseMapComboColors) {
                    CurrentTrackColor = Color.FromArgb(value.R, value.G, value.B);
                    RegeneratePreview();
                }
            }
        }

        private System.Windows.Media.Color borderColor;

        public System.Windows.Media.Color BorderColor
        {
            get => borderColor;
            set
            {
                if (Set(ref borderColor, value)) {
                    RegeneratePreview();
                }
            }
        }

        [JsonIgnore]
        public Visibility ShouldShowCcPicker => UseMapComboColors ? Visibility.Visible : Visibility.Collapsed;

        [JsonIgnore]
        public Visibility ShouldShowPalette => UseMapComboColors ? Visibility.Collapsed : Visibility.Visible;

        private double timeCode;
        public double TimeCode
        {
            get => timeCode;
            set => Set(ref timeCode, value);
        }

        private double duration;
        public double Duration
        {
            get => duration;
            set => Set(ref duration, value);
        }

        [JsonIgnore]
        public CommandImplementation UploadFileCommand { get; }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private Bitmap bm;
        [JsonIgnore]
        public Bitmap Bm
        {
            get => bm;
            set => Set(ref bm, value);
        }

        private InteropBitmap bmImage;
        [JsonIgnore]
        public InteropBitmap BmImage
        {
            get => bmImage;
            set => Set(ref bmImage, value);
        }

        private string pictureFile;
        public string PictureFile
        {
            get => pictureFile;
            set {
                Set(ref pictureFile, value);
                Bm = new Bitmap(value);
                RaisePropertyChanged(nameof(Bm));
                RegeneratePreview();
            }
        }

        private bool blackOn;
        public bool BlackOn
        {
            get => blackOn;
            set {
                if (Set(ref blackOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool borderOn;
        public bool BorderOn
        {
            get => borderOn;
            set {
                if (Set(ref borderOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool redOn;
        public bool RedOn
        {
            get => redOn;
            set {
                if (Set(ref redOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool greenOn;
        public bool GreenOn
        {
            get => greenOn;
            set {
                if (Set(ref greenOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool blueOn;
        public bool BlueOn
        {
            get => blueOn;
            set {
                if (Set(ref blueOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool alphaOn;
        public bool AlphaOn
        {
            get => alphaOn;
            set {
                if (Set(ref alphaOn, value)) {
                    RegeneratePreview();
                }
            }
        }

        private bool setBeatmapColors;
        public bool SetBeatmapColors
        {
            get => setBeatmapColors;
            set => Set(ref setBeatmapColors, value);
        }

        private HitObject selectedSlider;
        public HitObject SelectedSlider
        {
            get => selectedSlider;
            set {
                if (Set(ref selectedSlider, value)) {
                    RegeneratePreview();
                }
            }
        }

        [JsonIgnore]
        public CommandImplementation ImportCommand
        {
            get;
        }

        [JsonIgnore]
        public CommandImplementation RemoveCommand
        {
            get;
        }

        #endregion

        public void RegeneratePreview() {
            if (Bm == null) {
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
            Bitmap bm = (Bitmap)Bm.Clone();
            Color ctc = Color.FromArgb(CurrentTrackColor.ToArgb());
            Color bc = Color.FromArgb(BorderColor.R, BorderColor.G, BorderColor.B);
            HitObject ss = null;
            if (SelectedSlider != null) {
                ss = SelectedSlider.DeepCopy();
            }
            Task.Run(() => {
                (Bitmap newBM, long segmentCount) = SliderPicturator.Recolor(bm, ctc, bc, Color.FromArgb(0, 0, 0), ss, !BlackOn, !BorderOn, !AlphaOn, RedOn, GreenOn, BlueOn, Quality);
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
                    BmImage = retval;
                    SegmentCount = segmentCount;
                    RaisePropertyChanged(nameof(BmImage));
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
            ViewportSize = 32768;
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
            SetBeatmapColors = true;
            UseMapComboColors = false;
            ComboColor = Color.FromArgb(0, 0, 0);
            SegmentCount = 0;
            Quality = 1;
            TrackColorPickerColor = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
            BorderColor = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
            BmImage = null;
            Bm = null;
            SelectedSlider = null;

            UploadFileCommand = new CommandImplementation(_ => SetFile());
            ImportCommand = new CommandImplementation(_ => Import(
                IOHelper.GetCurrentBeatmapOrCurrentBeatmap(false)
            ));
            RemoveCommand = new CommandImplementation(_ => SelectedSlider = null);
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

        public void Import(string path)
        {
            try {
                EditorReader reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

                if (editorReaderException1 != null) {
                    throw new Exception("Could not fetch selected hit object.", editorReaderException1);
                }

                BeatmapEditor editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);
                List<HitObject> markedObjects = selected;

                if (editorReaderException2 != null) {
                    throw new Exception("Could not fetch selected hit object.", editorReaderException2);
                }

                if (markedObjects == null || markedObjects.Count(o => o.IsSlider) == 0) return;

                SelectedSlider = markedObjects.Find(s => s.IsSlider);
            } catch (Exception ex) {
                ex.Show();
            }
        }
    }
    
}