using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Infrastructure.Tools.SliderPicturator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.SliderPicturator;

[TestClass]
public sealed class SliderPicturatorFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task PicturateAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("slider-picturator", fixtureName);
        SliderPicturatorServiceOptions project = fixture.ReadProject<SliderPicturatorServiceOptions>();
        SliderPicturatorService service = new(
            fixture.Gateway,
            new SkiaSharpImageFileService(),
            new ApplicationSettings());

        // Act
        await service.PicturateAsync(
            fixture.TargetPath,
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Slider Picturator");
        fixture.AssertTextOutput(actual);
    }
}
