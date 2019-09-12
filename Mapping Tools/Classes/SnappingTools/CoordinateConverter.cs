using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SnappingTools {
    public class CoordinateConverter
    {
        private const int FilebarHeight = 28;
        private const int WindowChromeHeight = 32;
        private readonly Vector2 OsuWindowPositionOffset = new Vector2(2, 1);
        private readonly Vector2 ExtraOffset = new Vector2(0.5, 0.5);
        private string[] _configLines;

        public Vector2 OsuWindowPosition = Vector2.Zero;
        public Vector2 OsuResolution;
        public bool Fullscreen;
        public bool Letterboxing;
        public Vector2 LetterboxingPosition;

        public Vector2 EditorResolution => OsuResolution - new Vector2(0, FilebarHeight);

        public CoordinateConverter()
        {
            Initialize();
        }

        public void Initialize()
        {
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
                    OsuResolution = new Vector2(double.Parse(FindConfigValue("WidthFullscreen")),
                        double.Parse(FindConfigValue("HeightFullscreen")));
                } else {
                    OsuResolution = new Vector2(double.Parse(FindConfigValue("Width")),
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

        public static Box2 GetScreenBox()
        {
            var screenBounds = Screen.PrimaryScreen.Bounds;
            return new Box2(screenBounds.Left, screenBounds.Top, screenBounds.Right, screenBounds.Bottom);
        }

        private bool OsuFillsScreen
        {
            get
            {
                var screenBox = GetScreenBox();
                return Fullscreen || Letterboxing || OsuResolution == new Vector2(screenBox.Right, screenBox.Bottom);
            }
        }

        /// <summary>
        /// Gets the area on the screen in pixels which contains the entire osu window.
        /// </summary>
        /// <returns></returns>
        public Box2 GetOsuWindowBox()
        {
            var chromeAddition = OsuFillsScreen ? Vector2.Zero : new Vector2(2, 2 + WindowChromeHeight);
            return Letterboxing ? GetScreenBox() : 
                Fullscreen ? new Box2(Vector2.Zero, OsuResolution) : 
                new Box2(OsuWindowPosition + OsuWindowPositionOffset, OsuWindowPosition + OsuWindowPositionOffset + OsuResolution + chromeAddition);
        }

        /// <summary>
        /// Gets the area on the screen in pixels which contains the entire osu window without added window chrome border.
        /// </summary>
        /// <returns></returns>
        public Box2 GetOsuWindowBoxWithoutChrome()
        {
            var osuWindow = GetOsuWindowBox();
            if (OsuFillsScreen) return osuWindow;

            osuWindow.Top += 1 + WindowChromeHeight;
            osuWindow.Left += 1;
            osuWindow.Right -= 1;
            osuWindow.Bottom -= 1;
            return osuWindow;
        }

        /// <summary>
        /// Gets the area on the screen in pixels which is the editor area without menu bar and without letterboxing black space.
        /// </summary>
        /// <returns></returns>
        public Box2 GetEditorBox()
        {
            var osuWindow = GetOsuWindowBoxWithoutChrome();
            osuWindow.Top += FilebarHeight;
            if (!Letterboxing) return osuWindow;

            var letterboxMultiplier = LetterboxingPosition / 200 + new Vector2(0.5, 0.5);  // range: 0-1
            var blackSpaceSize = new Vector2(osuWindow.Width, osuWindow.Height) - EditorResolution;
            var letterboxOffset = letterboxMultiplier * blackSpaceSize;
            var letterboxOffset2 = (Vector2.One - letterboxMultiplier) * blackSpaceSize;

            osuWindow.Left += letterboxOffset.X;
            osuWindow.Top += letterboxOffset.Y;
            osuWindow.Right -= letterboxOffset2.X;
            osuWindow.Bottom -= letterboxOffset2.Y;
            return osuWindow;
        }

        /// <summary>
        /// Gets the area on the screen in pixels which is the editor space going from (0, 0) to (512, 384) in osu pixels.
        /// </summary>
        /// <returns></returns>
        public Box2 GetEditorGridBox()
        {
            var editor = GetEditorBox();

            // Screen pixels per osu pixel
            var ratio = editor.Height / 480;

            var gridDimensions = new Vector2(512, 384) * ratio;
            var emptySpace = new Vector2(editor.Width, editor.Height) - gridDimensions;
            var gridOffset = new Vector2(emptySpace.X / 2, emptySpace.Y / 4 * 3);

            editor.Left += gridOffset.X;
            editor.Top += gridOffset.Y;
            editor.Right = editor.Left + gridDimensions.X;
            editor.Bottom = editor.Top + gridDimensions.Y;

            return editor;
        }

        public Vector2 ScreenResolution = new Vector2(1920, 1080);

        public Vector2 ScreenToEditorCoordinate(Vector2 coord)
        {
            // In letterbox mode the osu window is always fullscreen sized and not upscaled
            // Letterboxing works in both fullscreen and windowed mode
            // The filebar is always at the topmost and not connected to the osu window, but it does reduce the window height by 24 every time

            var windowDimensions = OsuResolution - new Vector2(0, FilebarHeight);
            var letterboxOffset = Letterboxing
                ? (LetterboxingPosition / 200 + new Vector2(0.5, 0.5)) * (ScreenResolution - OsuResolution)
                : Vector2.Zero;  
            var windowOffset = OsuWindowPosition + letterboxOffset + new Vector2(0, FilebarHeight);
            if (!Fullscreen && !Letterboxing && OsuResolution != ScreenResolution) {
                windowOffset += new Vector2(0, 24);
            }

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;

            var gridOffset = windowDimensions - new Vector2(512, 384) * ratio;

            return (coord - ExtraOffset - windowOffset - new Vector2(gridOffset.X / 2, gridOffset.Y / 4 * 3)) / ratio;
        }

        public Vector2 EditorToScreenCoordinate(Vector2 coord)
        {
            var windowDimensions = OsuResolution - new Vector2(0, FilebarHeight);
            var letterboxOffset = Letterboxing
                ? (LetterboxingPosition / 200 + new Vector2(0.5, 0.5)) * (ScreenResolution - OsuResolution)
                : Vector2.Zero;
            var windowOffset = OsuWindowPosition + letterboxOffset + new Vector2(0, FilebarHeight);
            if (!Fullscreen && !Letterboxing && OsuResolution != ScreenResolution) {
                windowOffset += new Vector2(0, 24);
            }

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;

            var gridOffset = windowDimensions - new Vector2(512, 384) * ratio;

            return (coord * ratio + windowOffset + new Vector2(gridOffset.X / 2, gridOffset.Y / 4 * 3) + ExtraOffset).Rounded();
        }

        public Vector2 EditorToRelativeCoordinate(Vector2 coord) {
            var windowDimensions = OsuResolution - new Vector2(0, FilebarHeight);
            var letterboxOffset = Letterboxing
                ? (LetterboxingPosition / 200 + new Vector2(0.5, 0.5)) * (ScreenResolution - OsuResolution)
                : Vector2.Zero;
            var windowOffset = letterboxOffset + new Vector2(0, FilebarHeight);
            if (!Fullscreen && !Letterboxing && OsuResolution != ScreenResolution) {
                windowOffset += new Vector2(0, 24);
            }

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;

            var gridOffset = windowDimensions - new Vector2(512, 384) * ratio;

            return (coord * ratio + windowOffset + new Vector2(gridOffset.X / 2, gridOffset.Y / 4 * 3) + ExtraOffset).Rounded();
        }

        public double EditorToScreenSize(double d)
        {
            var windowDimensions = OsuResolution - new Vector2(0, FilebarHeight);

            // Screen pixels per osu pixel
            var ratio = windowDimensions.Y / 480;
            return d * ratio;
        }

        public override string ToString() {
            return $"{ScreenResolution}, {OsuWindowPosition}, {OsuResolution}, {Fullscreen}, {Letterboxing}, {LetterboxingPosition}";
        }
    }
}
