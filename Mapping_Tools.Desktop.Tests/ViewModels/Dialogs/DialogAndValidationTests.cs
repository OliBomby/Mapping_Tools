using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Validation;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels.Dialogs;

[TestClass]
public sealed class DialogAndValidationTests
{
    [TestMethod]
    public void Convert_DarkThemeForNullableToggle_ReturnsCheckedState()
    {
        // Arrange
        IValueConverter converter = new DarkThemeConverter();

        // Act
        object? result = converter.Convert(
            ApplicationTheme.Dark,
            typeof(bool?),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(true);
    }

    [TestMethod]
    public void ConvertBack_UncheckedThemeToggle_ReturnsLightTheme()
    {
        // Arrange
        IValueConverter converter = new DarkThemeConverter();

        // Act
        object? result = converter.ConvertBack(
            false,
            typeof(ApplicationTheme),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(ApplicationTheme.Light);
    }

    [TestMethod]
    public void ConvertBack_DoubleExpression_ReturnsEvaluatedValue()
    {
        // Arrange
        IValueConverter converter = new InvariantDoubleConverter();

        // Act
        object? result = converter.ConvertBack(
            "5 * 2.5",
            typeof(double),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(12.5);
    }

    [TestMethod]
    public void ConvertBack_CommaDecimal_ReturnsExpectedValue()
    {
        // Arrange
        IValueConverter converter = new InvariantDoubleConverter();

        // Act
        object? result = converter.ConvertBack(
            "12,5",
            typeof(double),
            null,
            CultureInfo.GetCultureInfo("nl-NL"));

        // Assert
        result.Should().Be(12.5);
    }

    [TestMethod]
    public void Convert_InvariantInt32_ReturnsEditableText()
    {
        // Arrange
        IValueConverter converter = new InvariantInt32Converter();

        // Act
        object? result = converter.Convert(
            42,
            typeof(string),
            null,
            CultureInfo.GetCultureInfo("nl-NL"));

        // Assert
        result.Should().Be("42");
    }

    [TestMethod]
    public void ConvertBack_InvariantDouble_ReturnsTypedValue()
    {
        // Arrange
        IValueConverter converter = new InvariantDoubleConverter();

        // Act
        object? result = converter.ConvertBack(
            "12.5",
            typeof(double),
            null,
            CultureInfo.GetCultureInfo("nl-NL"));

        // Assert
        result.Should().Be(12.5);
    }

    [TestMethod]
    public void ConvertBack_ConstantTimeSpan_ReturnsTypedDuration()
    {
        // Arrange
        IValueConverter converter = new ConstantTimeSpanConverter();

        // Act
        object? result = converter.ConvertBack(
            "00:15:00",
            typeof(TimeSpan),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [TestMethod]
    public void ConvertBack_MillisecondExpression_ReturnsTypedDuration()
    {
        // Arrange
        IValueConverter converter = new ConstantTimeSpanConverter();

        // Act
        object? result = converter.ConvertBack(
            "60 * 1000 * 15",
            typeof(TimeSpan),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [TestMethod]
    public void ConvertBack_Int32Expression_ReturnsEvaluatedValue()
    {
        // Arrange
        IValueConverter converter = new InvariantInt32Converter();

        // Act
        object? result = converter.ConvertBack(
            "6 * 7",
            typeof(int),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(42);
    }

    [TestMethod]
    public void MessageDialogRequest_MultipleDefaultChoices_ThrowsArgumentException()
    {
        // Arrange
        DialogChoice<bool>[] choices =
        [
            new("Yes", true, true),
            new("No", false, true),
        ];

        // Act
        Action act = () => _ = new MessageDialogRequest<bool>(
            "Confirm",
            "Continue?",
            choices,
            false);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exactly one default*");
    }

    [TestMethod]
    public void GetValidationResult_InclusiveRangeBoundary_ReturnsSuccess()
    {
        // Arrange
        ValidationAttribute validator = new InclusiveRangeAttribute<int>(1, 60)
        {
            ErrorMessage = "Use 1 through 60.",
        };

        // Act
        var result = validator.GetValidationResult(
            60,
            new ValidationContext(new object()));

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [TestMethod]
    public void MessageDialogRequest_SourceChoicesChange_PreservesImmutableSnapshot()
    {
        // Arrange
        List<DialogChoice<bool>> choices =
        [
            new("Yes", true, true),
            new("No", false, IsCancel: true),
        ];
        MessageDialogRequest<bool> request = new(
            "Confirm",
            "Continue?",
            choices,
            false);

        // Act
        choices.Clear();

        // Assert
        request.Choices.Should().HaveCount(2);
    }

    [TestMethod]
    public void ConvertBack_ParsingFailure_DisablesDialogAcceptance()
    {
        // Arrange
        int acceptCount = 0;
        var viewModel = CreateValueViewModel(
            _ => ValidationResult.Success,
            _ => acceptCount++);
        // Act
        viewModel.ValueText = "not-a-number";
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeFalse();
        acceptCount.Should().Be(0);
    }

    [TestMethod]
    public void ConvertBack_NullInt32_ReturnsDataValidationError()
    {
        // Arrange
        IValueConverter converter = new InvariantInt32Converter();

        // Act
        object? result = converter.ConvertBack(
            null,
            typeof(int),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<BindingNotification>();
        ((BindingNotification)result!).ErrorType
            .Should()
            .Be(BindingErrorType.DataValidationError);
    }

    [TestMethod]
    public void ConvertBack_ValidTextAfterParsingFailure_ReenablesDialogAcceptance()
    {
        // Arrange
        object? accepted = null;
        var viewModel = CreateValueViewModel(
            _ => ValidationResult.Success,
            value => accepted = value);
        viewModel.ValueText = "not-a-number";

        // Act
        viewModel.ValueText = "42";
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeTrue();
        accepted.Should().Be(42);
    }

    [TestMethod]
    public void AcceptCommand_ValidValue_SubmitsParsedValue()
    {
        // Arrange
        object? accepted = null;
        var viewModel = CreateValueViewModel(
            _ => ValidationResult.Success,
            value => accepted = value);

        // Act
        viewModel.ValueText = "42";
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeTrue();
        ((INotifyDataErrorInfo)viewModel).HasErrors.Should().BeFalse();
        accepted.Should().Be(42);
    }

    [TestMethod]
    public void ValueText_RejectedByCustomAttribute_DisablesAcceptanceAndExposesError()
    {
        // Arrange
        int acceptCount = 0;
        var viewModel = CreateValueViewModel(
            value => (int)value! <= 60
                ? ValidationResult.Success
                : new ValidationResult("Use 1 through 60."),
            _ => acceptCount++);

        // Act
        viewModel.ValueText = "90";
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeFalse();
        INotifyDataErrorInfo validation = viewModel;
        validation.GetErrors(nameof(ValueDialogViewModel.ValueText))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .Equal("Use 1 through 60.");
        acceptCount.Should().Be(0);
    }

    [TestMethod]
    public void CancelCommand_ValidValue_DismissesWithoutAcceptance()
    {
        // Arrange
        int acceptCount = 0;
        int cancelCount = 0;
        ValueDialogViewModel viewModel = new(
            "Type value",
            "Value",
            1,
            new InvariantInt32Converter(),
            typeof(int),
            "OK",
            "Cancel",
            _ => ValidationResult.Success,
            _ => acceptCount++,
            () => cancelCount++);

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        acceptCount.Should().Be(0);
        cancelCount.Should().Be(1);
    }

    [TestMethod]
    public void Command_MessageChoice_InvokesTypedCloseAdapter()
    {
        // Arrange
        int closeCount = 0;
        DialogChoiceViewModel choice = new(
            "Continue",
            true,
            false,
            () => closeCount++);

        // Act
        choice.Command.Execute(null);

        // Assert
        closeCount.Should().Be(1);
        choice.IsDefault.Should().BeTrue();
        choice.IsCancel.Should().BeFalse();
    }

    private static ValueDialogViewModel CreateValueViewModel(
        Func<object?, ValidationResult?> validate,
        Action<object?> accept)
    {
        return new ValueDialogViewModel(
            "Type value",
            "Value",
            0,
            new InvariantInt32Converter(),
            typeof(int),
            "OK",
            "Cancel",
            validate,
            accept,
            () => { });
    }
}
