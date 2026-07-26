using System.ComponentModel;
using System.Globalization;
using Avalonia.Data;
using Mapping_Tools.ApplicationServices.Interactions;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class DialogAndValidationTests
{
    [TestMethod]
    public void TryConvert_InvariantDoubleText_ReturnsExpectedValue()
    {
        // Arrange
        ITextValueConverter<double> converter = TextValueConverters.InvariantDouble;

        // Act
        bool converted = converter.TryConvert("12.5", out double value, out string? error);

        // Assert
        converted.Should().BeTrue();
        value.Should().Be(12.5);
        error.Should().BeNull();
    }

    [TestMethod]
    public void TryConvert_CommaDecimal_ReturnsActionableError()
    {
        // Arrange
        ITextValueConverter<double> converter = TextValueConverters.InvariantDouble;

        // Act
        bool converted = converter.TryConvert("12,5", out _, out string? error);

        // Assert
        converted.Should().BeFalse();
        error.Should().Contain("period");
    }

    [TestMethod]
    public void Convert_InvariantInt32_ReturnsEditableText()
    {
        // Arrange
        TextValueConverter<int> converter = new(
            TextValueConverters.InvariantInt32);

        // Act
        object result = converter.Convert(
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
        TextValueConverter<double> converter = new(
            TextValueConverters.InvariantDouble);

        // Act
        object result = converter.ConvertBack(
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
        TextValueConverter<TimeSpan> converter = new(
            TextValueConverters.ConstantTimeSpan);

        // Act
        object result = converter.ConvertBack(
            "00:15:00",
            typeof(TimeSpan),
            null,
            CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [TestMethod]
    public void MessageDialogRequest_MultipleDefaultChoices_ThrowsArgumentException()
    {
        // Arrange
        DialogChoice<bool>[] choices =
        [
            new("Yes", true, IsDefault: true),
            new("No", false, IsDefault: true)
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
    public void Validate_RequiredWhitespace_ReturnsFieldRequiredError()
    {
        // Arrange
        IValueValidator<string> validator = ValueValidators.RequiredText();

        // Act
        ValidationOutcome outcome = validator.Validate("   ");

        // Assert
        outcome.IsValid.Should().BeFalse();
        outcome.ErrorMessage.Should().Be("Field is required.");
    }

    [TestMethod]
    public void Validate_InclusiveRangeBoundary_ReturnsSuccess()
    {
        // Arrange
        IValueValidator<int> validator = ValueValidators.InclusiveRange(
            1,
            60,
            "Use 1 through 60.");

        // Act
        ValidationOutcome outcome = validator.Validate(60);

        // Assert
        outcome.Should().Be(ValidationOutcome.Success);
    }

    [TestMethod]
    public void Evaluate_ParsedValueOutsideRange_ReturnsFirstValidationError()
    {
        // Arrange
        ValueDialogRequest<int> request = new(
            "Backup interval",
            "Minutes",
            5,
            TextValueConverters.InvariantInt32,
            [ValueValidators.InclusiveRange(1, 60, "Use 1 through 60.")]);

        // Act
        ValueEvaluation<int> evaluation = request.Evaluate("90");

        // Assert
        evaluation.IsValid.Should().BeFalse();
        evaluation.Value.Should().Be(0);
        evaluation.ErrorMessage.Should().Be("Use 1 through 60.");
    }

    [TestMethod]
    public void Evaluate_ParsedValueWithinRange_ReturnsTypedValue()
    {
        // Arrange
        ValueDialogRequest<int> request = new(
            "Backup interval",
            "Minutes",
            5,
            TextValueConverters.InvariantInt32,
            [ValueValidators.InclusiveRange(1, 60, "Use 1 through 60.")]);

        // Act
        ValueEvaluation<int> evaluation = request.Evaluate("15");

        // Assert
        evaluation.IsValid.Should().BeTrue();
        evaluation.Value.Should().Be(15);
        evaluation.ErrorMessage.Should().BeNull();
    }

    [TestMethod]
    public void MessageDialogRequest_SourceChoicesChange_PreservesImmutableSnapshot()
    {
        // Arrange
        List<DialogChoice<bool>> choices =
        [
            new("Yes", true, IsDefault: true),
            new("No", false, IsCancel: true)
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
        TextConversionState conversionState = new();
        TextValueConverter<int> converter = new(
            TextValueConverters.InvariantInt32,
            conversionState);
        ValueDialogViewModel viewModel = CreateValueViewModel(
            _ => ValidationOutcome.Success,
            conversionState,
            _ => acceptCount++);

        // Act
        object result = converter.ConvertBack(
            "not-a-number",
            typeof(object),
            null,
            CultureInfo.InvariantCulture);
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeOfType<BindingNotification>();
        ((BindingNotification)result).ErrorType
            .Should()
            .Be(BindingErrorType.DataValidationError);
        viewModel.IsValid.Should().BeFalse();
        acceptCount.Should().Be(0);
    }

    [TestMethod]
    public void AcceptCommand_ValidValue_SubmitsParsedValue()
    {
        // Arrange
        object? accepted = null;
        TextConversionState conversionState = new();
        ValueDialogViewModel viewModel = CreateValueViewModel(
            _ => ValidationOutcome.Success,
            conversionState,
            value => accepted = value);

        // Act
        viewModel.Value = 42;
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeTrue();
        ((INotifyDataErrorInfo)viewModel).HasErrors.Should().BeFalse();
        accepted.Should().Be(42);
    }

    [TestMethod]
    public void Value_RejectedByCustomAttribute_DisablesAcceptanceAndExposesError()
    {
        // Arrange
        int acceptCount = 0;
        TextConversionState conversionState = new();
        ValueDialogViewModel viewModel = CreateValueViewModel(
            value => (int)value! <= 60
                ? ValidationOutcome.Success
                : ValidationOutcome.Failure("Use 1 through 60."),
            conversionState,
            _ => acceptCount++);

        // Act
        viewModel.Value = 90;
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeFalse();
        INotifyDataErrorInfo validation = viewModel;
        validation.GetErrors(nameof(ValueDialogViewModel.Value))
            .Cast<string>()
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
        TextConversionState conversionState = new();
        ValueDialogViewModel viewModel = new(
            "Type value",
            "Value",
            1,
            "OK",
            "Cancel",
            _ => ValidationOutcome.Success,
            conversionState,
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
            isDefault: true,
            isCancel: false,
            () => closeCount++);

        // Act
        choice.Command.Execute(null);

        // Assert
        closeCount.Should().Be(1);
        choice.IsDefault.Should().BeTrue();
        choice.IsCancel.Should().BeFalse();
    }

    private static ValueDialogViewModel CreateValueViewModel(
        Func<object?, ValidationOutcome> validate,
        TextConversionState conversionState,
        Action<object?> accept)
    {
        return new ValueDialogViewModel(
            "Type value",
            "Value",
            0,
            "OK",
            "Cancel",
            validate,
            conversionState,
            accept,
            () => { });
    }
}
