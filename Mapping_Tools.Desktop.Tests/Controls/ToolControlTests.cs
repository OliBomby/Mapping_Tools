using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
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
            + "   at Mapping_Tools.Desktop.Converters.InvariantInt32Converter.ConvertBack()\r\n"
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

    [TestMethod]
    public void RunButton_WithInvalidBoundText_BlocksExecutionUntilTextIsCorrected()
    {
        // Arrange
        var viewModel = new ValidationProbeViewModel(CreateExecutionService());
        TextBox input = new() { DataContext = viewModel };
        input.Bind(
            TextBox.TextProperty,
            new Binding(nameof(ValidationProbeViewModel.Number))
            {
                Mode = BindingMode.TwoWay,
                Converter = new InvariantDoubleConverter(),
            });
        ToolRunButton runButton = new() { RunCommand = viewModel.RunCommand };
        StackPanel view = new();
        view.Children.Add(input);
        view.Children.Add(runButton);
        Button button = runButton.GetVisualDescendants().OfType<Button>().Single();
        input.Text = "not a number";

        // Act
        bool blockedCanExecute = button.Command!.CanExecute(null);
        bool invalidHasErrors = DataValidationErrors.GetHasErrors(input);
        button.Command!.Execute(null);
        int blockedRunCount = viewModel.RunCount;
        input.Text = "128";
        bool correctedCanExecute = button.Command.CanExecute(null);
        button.Command.Execute(null);

        // Assert
        invalidHasErrors.Should().BeTrue();
        blockedCanExecute.Should().BeFalse();
        blockedRunCount.Should().Be(0);
        DataValidationErrors.GetHasErrors(input).Should().BeFalse();
        correctedCanExecute.Should().BeTrue();
        viewModel.RunCount.Should().Be(1);
    }

    private static ToolExecutionService CreateExecutionService()
    {
        return new ToolExecutionService(
            new UserNotificationService(),
            new RecordingEditorReloadService(),
            new ApplicationSettings(),
            TimeProvider.System);
    }

    internal sealed class ValidationProbeViewModel : SingleRunToolViewModel
    {
        public ValidationProbeViewModel(IToolExecutionService execution)
            : base(execution, ComboColourStudioToolDefinition.Definition)
        {
        }

        public double Number { get; set; } = 256;

        public int RunCount { get; private set; }

        protected override Task RunCoreAsync()
        {
            RunCount++;
            return Task.CompletedTask;
        }
    }
}
