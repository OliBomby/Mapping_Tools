using System.ComponentModel;
using Mapping_Tools.ApplicationServices.Interactions;
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
    public void Text_ParsingFailure_DisablesAcceptanceAndShowsFormatError()
    {
        // Arrange
        int acceptCount = 0;
        ValueDialogViewModel viewModel = CreateValueViewModel(
            _ => new ValueInputEvaluation(false, null, "Enter a whole number."),
            _ => acceptCount++);

        // Act
        viewModel.Text = "not-a-number";
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Enter a whole number.");
        INotifyDataErrorInfo validation = viewModel;
        validation.HasErrors.Should().BeTrue();
        validation.GetErrors(nameof(ValueDialogViewModel.Text))
            .Cast<string>()
            .Should()
            .Equal("Enter a whole number.");
        acceptCount.Should().Be(0);
    }

    [TestMethod]
    public void AcceptCommand_ValidValue_SubmitsParsedValue()
    {
        // Arrange
        object? accepted = null;
        ValueDialogViewModel viewModel = CreateValueViewModel(
            text => new ValueInputEvaluation(true, int.Parse(text!), null),
            value => accepted = value);

        // Act
        viewModel.Text = "42";
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.IsValid.Should().BeTrue();
        viewModel.ErrorMessage.Should().BeNull();
        accepted.Should().Be(42);
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
            "1",
            "OK",
            "Cancel",
            text => new ValueInputEvaluation(true, int.Parse(text!), null),
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
        Func<string?, ValueInputEvaluation> evaluate,
        Action<object?> accept)
    {
        return new ValueDialogViewModel(
            "Type value",
            "Value",
            "0",
            "OK",
            "Cancel",
            evaluate,
            accept,
            () => { });
    }
}
