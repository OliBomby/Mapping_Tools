using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.IO;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SnappingTools {
    public class CoordinateConverter
    {
        private const int FilebarHeight = 24;
        private readonly Vector2 ExtraOffset = new Vector2(0.5, 0.5);
        private string[] _configLines;

        public Vector2 ScreenResolution;
        public Vector2 OsuWindowPosition = Vector2.Zero;
        public Vector2 OsuWindowDimensions;
        public bool Fullscreen;
        public bool Letterboxing;
        public Vector2 LetterboxingPosition;

        public CoordinateConverter()
        {
            Initialize();
        }

        public void Initialize()
        {
            var screenBounds = Screen.PrimaryScreen.Bounds;
            ScreenResolution = new Vector2(screenBounds.Width, screenBounds.Height);

            ReadConfig();
        }

        /// <summary>
        /// Reads the osu! user config to get the variables: resolution, letterboxing & letterboxing position
        /// </summary>
        public void ReadConfig()
        {
            try {
                // Try reading the user config for drawing data
                _configLines = File.ReadAllLines(SettingsManager.Settings.OsuConfigPath);

                Fullscreen = FindConfigValue("Fullscreen") == "1";
                if (Fullscreen) {
                    OsuWindowDimensions = new Vector2(double.Parse(FindConfigValue("WidthFullscreen")),
                        double.Parse(FindConfigValue("HeightFullscreen")));
                } else {
                    OsuWindowDimensions = new Vector2(double.Parse(FindConfigValue("Width")),
                        double.Parse(FindConfigValue("Height")));
                }

                Letterboxing = FindConfigValue("Letterboxing") == "1";
                LetterboxingPosition = new Vector2(double.Parse(FindConfigValue("LetterboxPositionX")),
                    double.Parse(FindConfigValue("LetterboxPositionY")));

            } catch (Exception ex) { Console.WriteLine(ex.Message + @" while reading osu! user config."); }
        }

        private string FindConfigValue(string key)
        {
            foreach (var line in _configLines)
            {
                var split = line.Split('=');
                if (split[0].Trim() == key)
                {
                    return split[1].Trim();
                }
            }
            throw new Exception($"Can't find the key {key} in osu! user config.");
        }

        public Vector2 ScreenToEditorCoordinate(Vector2 coord)
        {
            // In letterbox mode the osu window is always fullscreen sized and not upscaled
            // Letterboxing works in both fullscreen and windowed mode
            // The filebar is always at the topmost and not connected to the osu window, but it does reduce the window height by 24 every time

            var windowDimensions = OsuWindowDimensions - new Vector2(0, FilebarHeight);
            var letterboxOffset = Letterboxing
                ? (LetterboxingPosition / 200 + new Vector2(0.5, 0.5)) * (ScreenResolution - OsuWindowDimensions)
                : Vector2.Zero;  
            var windowOffset = OsuWindowPosition + letterboxOffset + new Vector2(0, FilebarHeight);
            if (!Fullscreen && !Letterboxing && OsuWindowDimensions != ScreenResolution) {
                windowOffset += new Vector2(0, 24);
            }

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;

            var gridOffset = windowDimensions - new Vector2(512, 384) * ratio;

            return (coord - ExtraOffset - windowOffset - new Vector2(gridOffset.X / 2, gridOffset.Y / 4 * 3)) / ratio;
        }

        public Vector2 EditorToScreenCoordinate(Vector2 coord)
        {
            var windowDimensions = OsuWindowDimensions - new Vector2(0, FilebarHeight);
            var letterboxOffset = Letterboxing
                ? (LetterboxingPosition / 200 + new Vector2(0.5, 0.5)) * (ScreenResolution - OsuWindowDimensions)
                : Vector2.Zero;
            var windowOffset = OsuWindowPosition + letterboxOffset + new Vector2(0, FilebarHeight);
            if (!Fullscreen && !Letterboxing && OsuWindowDimensions != ScreenResolution) {
                windowOffset += new Vector2(0, 24);
            }

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;

            var gridOffset = windowDimensions - new Vector2(512, 384) * ratio;

            return (coord * ratio + windowOffset + new Vector2(gridOffset.X / 2, gridOffset.Y / 4 * 3) + ExtraOffset).Rounded();
        }

        public Vector2 EditorToRelativeCoordinate(Vector2 coord) {
            var windowDimensions = OsuWindowDimensions - new Vector2(0, FilebarHeight);
            var letterboxOffset = Letterboxing
                ? (LetterboxingPosition / 200 + new Vector2(0.5, 0.5)) * (ScreenResolution - OsuWindowDimensions)
                : Vector2.Zero;
            var windowOffset = letterboxOffset + new Vector2(0, FilebarHeight);
            if (!Fullscreen && !Letterboxing && OsuWindowDimensions != ScreenResolution) {
                windowOffset += new Vector2(0, 24);
            }

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;

            var gridOffset = windowDimensions - new Vector2(512, 384) * ratio;

            return (coord * ratio + windowOffset + new Vector2(gridOffset.X / 2, gridOffset.Y / 4 * 3) + ExtraOffset).Rounded();
        }

        public double EditorToScreenSize(double d)
        {
            var windowDimensions = OsuWindowDimensions - new Vector2(0, FilebarHeight);

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;
            return d * ratio;
        }

        public override string ToString() {
            return $"{ScreenResolution}, {OsuWindowPosition}, {OsuWindowDimensions}, {Fullscreen}, {Letterboxing}, {LetterboxingPosition}";
        }
    }
}
