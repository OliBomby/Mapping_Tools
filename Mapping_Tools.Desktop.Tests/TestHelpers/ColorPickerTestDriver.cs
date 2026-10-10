using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Tests.TestHelpers;

/// <summary>Drives the real color picker popup for view behavior tests.</summary>
internal static class ColorPickerTestDriver
{
    internal static void SetHexColor(HeadlessViewHost host, ColorPicker picker, string hexColor)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(picker);
        ArgumentException.ThrowIfNullOrWhiteSpace(hexColor);
        Color expectedColor;
        try
        {
            expectedColor = Color.Parse(hexColor);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(ApplicationText.Format(DesktopStrings.Test_ColorPickerInvalidColor, hexColor), nameof(hexColor), exception);
        }

        host.Click(picker, new Point(picker.Bounds.Width - 8, picker.Bounds.Height / 2));
        picker.ApplyTemplate();
        DropDownButton button = picker.GetVisualDescendants().OfType<DropDownButton>().Single();
        Flyout flyout = (Flyout)button.Flyout!;
        if (!flyout.IsOpen)
            host.Click(picker, new Point(picker.Bounds.Width - 8, picker.Bounds.Height / 2));
        if (!flyout.IsOpen)
            throw new InvalidOperationException("The color picker popup did not open after its dropdown was clicked.");

        Control content = (Control)flyout.Content!;
        content.ApplyTemplate();
        foreach (Control control in content.GetVisualDescendants().OfType<Control>())
            control.ApplyTemplate();
        HeadlessViewHost.RunDispatcherJobs();
        TopLevel popup = TopLevel.GetTopLevel(content)
                         ?? throw new InvalidOperationException("Color picker popup has no input root.");
        TabItem[] tabs = content.GetVisualDescendants().OfType<TabItem>()
            .Where(tab => tab.Bounds.Width > 0 && tab.Bounds.Height > 0).ToArray();
        TextBox? hexTextBox = null;
        foreach (TabItem tab in tabs)
        {
            Point tabPoint = tab.TranslatePoint(new Point(tab.Bounds.Width / 2, tab.Bounds.Height / 2), popup)
                             ?? throw new InvalidOperationException("Could not locate a color picker tab in its popup.");
            popup.MouseMove(tabPoint);
            popup.MouseDown(tabPoint, MouseButton.Left, RawInputModifiers.LeftMouseButton);
            popup.MouseUp(tabPoint, MouseButton.Left);
            HeadlessViewHost.RunDispatcherJobs();
            foreach (Control control in content.GetVisualDescendants().OfType<Control>())
                control.ApplyTemplate();
            HeadlessViewHost.RunDispatcherJobs();
            hexTextBox = content.GetVisualDescendants().OfType<TextBox>()
                .SingleOrDefault(textBox => textBox.Name?.Contains("Hex", StringComparison.OrdinalIgnoreCase) == true);
            if (hexTextBox is not null) break;
        }

        if (hexTextBox is null)
            throw new InvalidOperationException(
                $"The color picker popup has no hex color input after visiting {tabs.Length} tabs. Visuals: {string.Join(", ", content.GetVisualDescendants().Select(control => $"{control.GetType().Name}:{control.Name}"))}");
        Point point = hexTextBox.TranslatePoint(new Point(hexTextBox.Bounds.Width / 2, hexTextBox.Bounds.Height / 2), popup)
                      ?? throw new InvalidOperationException("Could not locate the hex color input in its popup.");
        popup.MouseMove(point);
        popup.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        popup.MouseUp(point, MouseButton.Left);
        HeadlessViewHost.RunDispatcherJobs();
        popup.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        popup.KeyTextInput(hexColor);
        HeadlessViewHost.RunDispatcherJobs();
        host.Click(picker, new Point(8, picker.Bounds.Height / 2));

        if (picker.Color != expectedColor)
            throw new InvalidOperationException(
                $"The color picker hex input produced {picker.Color} instead of {expectedColor} (text: '{hexTextBox.Text}').");
    }
}
