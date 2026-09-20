using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class ButtonModifierCaptureTests
{
    [TestMethod]
    public void Consume_AfterShiftPointerPressAndRelease_PreservesModifierForClick()
    {
        // Arrange
        Button button = new();
        ButtonModifierCapture capture = new(button);
        var observedModifiers = KeyModifiers.None;
        button.Click += (_, _) => observedModifiers = capture.Consume();
        Pointer pointer = new(1, PointerType.Mouse, true);
        PointerPointProperties pressedProperties = new(
            RawInputModifiers.LeftMouseButton,
            PointerUpdateKind.LeftButtonPressed);
        PointerPressedEventArgs pressed = new(
            button,
            pointer,
            button,
            new Point(0, 0),
            0,
            pressedProperties,
            KeyModifiers.Shift);
        PointerPointProperties releasedProperties = new(
            RawInputModifiers.None,
            PointerUpdateKind.LeftButtonReleased);
        PointerReleasedEventArgs released = new(
            button,
            pointer,
            button,
            new Point(0, 0),
            1,
            releasedProperties,
            KeyModifiers.Shift,
            MouseButton.Left);

        // Act
        button.RaiseEvent(pressed);
        button.RaiseEvent(released);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // Assert
        observedModifiers.Should().Be(KeyModifiers.Shift);
    }

    [TestMethod]
    public void Consume_AfterControlPointerPressAndRelease_PreservesModifierForClick()
    {
        // Arrange
        Button button = new();
        ButtonModifierCapture capture = new(button);
        var observedModifiers = KeyModifiers.None;
        button.Click += (_, _) => observedModifiers = capture.Consume();
        Pointer pointer = new(1, PointerType.Mouse, true);
        PointerPointProperties pressedProperties = new(
            RawInputModifiers.LeftMouseButton,
            PointerUpdateKind.LeftButtonPressed);
        PointerPressedEventArgs pressed = new(
            button,
            pointer,
            button,
            new Point(0, 0),
            0,
            pressedProperties,
            KeyModifiers.Control);
        PointerPointProperties releasedProperties = new(
            RawInputModifiers.None,
            PointerUpdateKind.LeftButtonReleased);
        PointerReleasedEventArgs released = new(
            button,
            pointer,
            button,
            new Point(0, 0),
            1,
            releasedProperties,
            KeyModifiers.Control,
            MouseButton.Left);

        // Act
        button.RaiseEvent(pressed);
        button.RaiseEvent(released);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // Assert
        observedModifiers.Should().Be(KeyModifiers.Control);
    }

    [TestMethod]
    public void Consume_AfterKeyboardActivation_PreservesModifiersForClick()
    {
        // Arrange
        Button button = new();
        ButtonModifierCapture capture = new(button);
        var observedModifiers = KeyModifiers.None;
        button.Click += (_, _) => observedModifiers = capture.Consume();
        KeyEventArgs keyDown = new()
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = button,
            Key = Key.Space,
            KeyModifiers = KeyModifiers.Shift | KeyModifiers.Control,
        };

        // Act
        button.RaiseEvent(keyDown);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // Assert
        observedModifiers.Should().Be(KeyModifiers.Shift | KeyModifiers.Control);
    }
}
