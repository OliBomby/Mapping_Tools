using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class CurrentBeatmapMemoryReaderTests
{
    [TestMethod]
    public void CreateProcessTarget_Linux_TargetsWineWithoutWindowsBitnessCheck()
    {
        // Arrange
        const bool is_windows = false;

        // Act
        var result = CurrentBeatmapMemoryReader.CreateProcessTarget(is_windows);

        // Assert
        result.ProcessName.Should().Be("osu!.exe");
        result.Target64Bit.Should().BeNull();
    }

    [TestMethod]
    public void CreateProcessTarget_Windows_Targets32BitStable()
    {
        // Arrange
        const bool is_windows = true;

        // Act
        var result = CurrentBeatmapMemoryReader.CreateProcessTarget(is_windows);

        // Assert
        result.ProcessName.Should().Be("osu!");
        result.Target64Bit.Should().BeFalse();
    }
}
