using Avalonia.Data;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class ToolControlTests
{
    [TestMethod]
    public void Value_AfterCompletedRun_ShowsNextRunAgain()
    {
        // Arrange
        ToolProgressBar progressBar = new()
        {
            Maximum = 100,
            Value = 100,
            IsVisible = false
        };

        // Act
        progressBar.Value = 0;
        progressBar.Value = 25;

        // Assert
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
            System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        message.Should().Be("Enter a valid number.");
    }

    [TestMethod]
    public void Convert_WithWrappedConverterError_ReturnsNestedFormatReason()
    {
        // Arrange
        ValidationErrorMessageConverter converter = new();
        InvalidCastException error = new(
            "Could not convert '{DataValidationError: System.FormatException: " +
            "Beat divisor 'nope' is not a valid fraction or number., Fallback: (do nothing)}' " +
            "(Avalonia.Data.BindingNotification) to IBeatDivisor[].");

        // Act
        object message = converter.Convert(
            error,
            typeof(string),
            null,
            System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        message.Should().Be("Beat divisor 'nope' is not a valid fraction or number.");
    }
}
