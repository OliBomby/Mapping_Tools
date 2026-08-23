using System.Globalization;
using Avalonia.Data;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class ToolControlTests
{
    [TestMethod]
    public void Value_AcrossRunLifecycle_AlwaysReservesProgressSlot()
    {
        // Arrange
        ToolProgressBar progressBar = new()
        {
            Maximum = 100,
            Value = 100,
        };

        // Act
        progressBar.Value = 0;
        bool afterReset = progressBar.IsVisible;
        progressBar.Value = 25;

        // Assert
        afterReset.Should().BeTrue();
        progressBar.IsVisible.Should().BeTrue();
    }

    [TestMethod]
    public void Convert_WithValidationException_ReturnsOnlyErrorReason()
    {
        // Arrange
        ValidationErrorMessageConverter converter = new();
        BindingNotification notification = new(
            new FormatException("Enter a valid number."),
            BindingErrorType.DataValidationError);

        // Act
        object message = converter.Convert(
            notification,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        message.Should().Be("Enter a valid number.");
    }

    [TestMethod]
    public void Convert_WithWrappedConverterError_ReturnsNestedFormatReason()
    {
        // Arrange
        ValidationErrorMessageConverter converter = new();
        InvalidCastException error = new(
            "Could not convert '{DataValidationError: System.FormatException: "
            + "Beat divisor 'nope' is not a valid fraction or number., Fallback: (do nothing)}' "
            + "(Avalonia.Data.BindingNotification) to IBeatDivisor[].");

        // Act
        object message = converter.Convert(
            error,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        message.Should().Be("Beat divisor 'nope' is not a valid fraction or number.");
    }

    [TestMethod]
    public void Convert_WithWrappedConverterErrorStackTrace_ReturnsOnlyNestedFormatReason()
    {
        // Arrange
        ValidationErrorMessageConverter converter = new();
        InvalidCastException error = new(
            "Could not convert '{DataValidationError: System.FormatException: "
            + "Enter a whole number or arithmetic expression.\r\n"
            + "   at Mapping_Tools.Application.Interactions.Converters.InvariantInt32Converter.ConvertBack()\r\n"
            + "   at Mapping_Tools.Desktop.Converters.ValueConverterHelper.ConvertBack()\r\n"
            + "}' (Avalonia.Data.BindingNotification) to System.Int32.");

        // Act
        object message = converter.Convert(
            error,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        message.Should().Be("Enter a whole number or arithmetic expression.");
    }

    [TestMethod]
    public void ConvertError_WithBindingFailure_ReturnsOnlyErrorReason()
    {
        // Arrange
        BindingNotification notification = new(
            new FormatException("Enter a whole number."),
            BindingErrorType.DataValidationError);

        // Act
        object message = ValidationErrorMessageConverter.ConvertError(notification);

        // Assert
        message.Should().Be("Enter a whole number.");
    }
}
