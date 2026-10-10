using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views.Dialogs;

[TestClass]
public sealed class GraphEditorDialogTests
{
    [TestMethod]
    public void GraphEdit_IsCopiedBackToEditorViewModelOnAccept()
    {
        // Arrange
        GraphEditorViewModel viewModel = new(GraphState.CreateDefault());
        GraphEditorDialog dialog = new(viewModel);

        // Act
        dialog.GraphControl.MoveAnchor(0, new Vector2(0, 0.25));
        viewModel.AcceptCommand.Execute(null);

        // Assert
        viewModel.GraphState.Anchors[0].Pos.Y.Should().Be(0.25f);
    }
}
