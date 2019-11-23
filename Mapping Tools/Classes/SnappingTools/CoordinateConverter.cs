using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SnappingTools {

    public class CoordinateConverter {

        private double FilebarHeight {
            get {
                var mult = GetDpiMultiplier();
                return 24 * mult.Y;
            }
        }

        private double WindowChromeHeight {
            get {
                var mult = GetDpiMultiplier();
                return 24 * mult.Y;
            }
        }

        public Box2 EditorBoxOffset = new Box2(0, 1, 0, 1);  // Extra offset applied to the bounds of the editor box
        public Vector2 PositionSnapOffset => new Vector2(0.5, 0.5);  // An offset to fix a random instance of 1 pixel off snapping
        private string[] _configLines;

        public Vector2 OsuWindowPosition = Vector2.Zero;
        public Vector2 OsuResolution;
        public bool Fullscreen;
        public bool Letterboxing;
        public Vector2 LetterboxingPosition;

        // Dimensions of the editor screen without the menu bar
        public Vector2 EditorResolution => OsuResolution - new Vector2(0, FilebarHeight);

        // Dimensions of the editor grid in osu! pixels
        public readonly Vector2 EditorGridResolution = new Vector2(512, 384);

        public CoordinateConverter() {
            Fullscreen = true;
            OsuResolution = new Vector2(1920, 1080);
            Letterboxing = true;
            LetterboxingPosition = new Vector2(0.5, 0.5);
            Initialize();
        }

        public void Initialize() {
            ReadConfig();
        }

        /// <summary>
        /// Reads the osu! user config to get the variables: resolution, letterboxing & letterboxing position
        /// </summary>
        public void ReadConfig() {
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
            } catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        private string FindConfigValue(string key) {
            foreach (var line in _configLines) {
                var split = line.Split('=');
                if (split[0].Trim() == key) {
                    return split[1].Trim();
                }
            }
            throw new Exception($"Can't find the key {key} in osu! user config.");
        }

        // Converts pixel coordinate to DPI coordinate
        public Vector2 ToDpi(Vector2 coord) {
            var source = PresentationSource.FromVisual(MainWindow.AppWindow);
            if (source == null) return coord;
            if (source.CompositionTarget == null) return coord;
            var dpiX = source.CompositionTarget.TransformToDevice.M11;
            var dpiY = source.CompositionTarget.TransformToDevice.M22;
            return new Vector2(coord.X / dpiX, coord.Y / dpiY) + new Vector2(0.1, 0.1);
        }

        public Vector2 GetDpiMultiplier() {
            var source = PresentationSource.FromVisual(MainWindow.AppWindow);
            if (source?.CompositionTarget != null)
                return new Vector2(source.CompositionTarget.TransformToDevice.M11,
                                   source.CompositionTarget.TransformToDevice.M22);
            return Vector2.One;
        }

        public static Box2 GetScreenBox() {
            var screenBounds = Screen.PrimaryScreen.Bounds;
            return new Box2(screenBounds.Left, screenBounds.Top, screenBounds.Right, screenBounds.Bottom);
        }

        private bool OsuFillsScreen {
            get {
                var screenBox = GetScreenBox();
                return Fullscreen || Letterboxing || OsuResolution == new Vector2(screenBox.Right, screenBox.Bottom);
            }
        }

        /// <summary>
        /// Gets the area on the screen in pixels which contains the entire osu window.
        /// </summary>
        /// <returns></returns>
        public Box2 GetOsuWindowBox() {
            var chromeAddition = OsuFillsScreen ? Vector2.Zero : new Vector2(2, 2 + WindowChromeHeight);
            return Letterboxing ? GetScreenBox() :
                OsuFillsScreen ? new Box2(Vector2.Zero, OsuResolution) :
                new Box2(OsuWindowPosition, OsuWindowPosition + OsuResolution + chromeAddition);
        }

        /// <summary>
        /// Gets the area on the screen in pixels which contains the entire osu window without added window chrome border.
        /// </summary>
        /// <returns></returns>
        public Box2 GetOsuWindowBoxWithoutChrome() {
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
        public Box2 GetEditorBox() {
            var osuWindow = GetOsuWindowBoxWithoutChrome();
            osuWindow.Top += FilebarHeight;
            if (!Letterboxing) {
                return AddBox2(osuWindow, EditorBoxOffset);
            }

            var letterboxMultiplier = LetterboxingPosition / 200 + new Vector2(0.5, 0.5);  // range: 0-1
            var blackSpaceSize = new Vector2(osuWindow.Width, osuWindow.Height) - EditorResolution;
            var letterboxOffset = letterboxMultiplier * blackSpaceSize;
            var letterboxOffset2 = (Vector2.One - letterboxMultiplier) * blackSpaceSize;

            osuWindow.Left += letterboxOffset.X;
            osuWindow.Top += letterboxOffset.Y;
            osuWindow.Right -= letterboxOffset2.X;
            osuWindow.Bottom -= letterboxOffset2.Y;

            return AddBox2(osuWindow, EditorBoxOffset);
        }

        private static Box2 AddBox2(Box2 thisBox2, Box2 otherBox2) {
            return new Box2(
                thisBox2.Left + otherBox2.Left,
            thisBox2.Top + otherBox2.Top,
            thisBox2.Right + otherBox2.Right,
            thisBox2.Bottom + otherBox2.Bottom);
        }

        /// <summary>
        /// Gets the area on the screen in pixels which is the editor space going from (0, 0) to (512, 384) in osu pixels.
        /// </summary>
        /// <returns></returns>
        public Box2 GetEditorGridBox() {
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

        /// <summary>
        /// Converts a coordinate on the screen to a coordinate in the editor
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public Vector2 ScreenToEditorCoordinate(Vector2 coord) {
            var editorGridBox = GetEditorGridBox();
            var ratioX = editorGridBox.Width / EditorGridResolution.X;
            var ratioY = editorGridBox.Height / EditorGridResolution.Y;

            return new Vector2((coord.X - PositionSnapOffset.X - editorGridBox.Left) / ratioX, (coord.Y - PositionSnapOffset.Y - editorGridBox.Top) / ratioY);
        }

        public Vector2 EditorToScreenCoordinate(Vector2 coord) {
            var editorGridBox = GetEditorGridBox();
            var ratioX = editorGridBox.Width / EditorGridResolution.X;
            var ratioY = editorGridBox.Height / EditorGridResolution.Y;

            return new Vector2(coord.X * ratioX + editorGridBox.Left, coord.Y * ratioY + editorGridBox.Top) + PositionSnapOffset;
        }

        public Vector2 EditorToRelativeCoordinate(Vector2 coord) {
            var editor = GetEditorBox();

            // Screen pixels per osu pixel
            var ratio = editor.Height / 480;

            var gridDimensions = new Vector2(512, 384) * ratio;
            var emptySpace = new Vector2(editor.Width, editor.Height) - gridDimensions;
            var gridOffset = new Vector2(emptySpace.X / 2, emptySpace.Y / 4 * 3);

            var editorGridBox = GetEditorGridBox();
            var ratioX = editorGridBox.Width / EditorGridResolution.X;
            var ratioY = editorGridBox.Height / EditorGridResolution.Y;

            return new Vector2(coord.X * ratioX, coord.Y * ratioY) + gridOffset;
        }

        // Scales editor size to screen size
        public Vector2 ScaleByRatio(Vector2 thing) {
            var editorGridBox = GetEditorGridBox();
            var ratioX = editorGridBox.Width / EditorGridResolution.X;
            var ratioY = editorGridBox.Height / EditorGridResolution.Y;

            return new Vector2(thing.X * ratioX, thing.Y * ratioY);
        }

        public override string ToString() {
            return $"{GetScreenBox()}, {OsuWindowPosition}, {OsuResolution}, {Fullscreen}, {Letterboxing}, {LetterboxingPosition}";
        }
    }
}