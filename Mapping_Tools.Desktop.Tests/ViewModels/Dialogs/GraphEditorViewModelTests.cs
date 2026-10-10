using Mapping_Tools.Core.Graph;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels.Dialogs;

[TestClass]
public sealed class GraphEditorViewModelTests
{
    [TestMethod]
    public void GraphState_SetFromGraphControl_PreservesThePublishedSnapshot()
    {
        // Arrange
        var initial = GraphState.CreateDefault();
        GraphEditorViewModel viewModel = new(initial);
        var published = initial.Clone();

        // Act
        viewModel.GraphState = published;

        // Assert
        viewModel.GraphState.Should().BeSameAs(published);
    }
}
