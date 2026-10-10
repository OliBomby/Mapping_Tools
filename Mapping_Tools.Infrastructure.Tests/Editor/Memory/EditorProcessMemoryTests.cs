using Mapping_Tools.Infrastructure.Editor.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor.Memory;

[TestClass]
public sealed class EditorProcessMemoryTests
{
    [TestMethod]
    public void ParseLinuxRegions_WithWineAndHostMappings_ReturnsWritable32BitRanges()
    {
        // Arrange
        string[] maps =
        [
            "10000000-10001000 rw-p 00000000 00:00 0",
            "10001000-10002000 r--p 00000000 00:00 0",
            "10002000-10003000 --xp 00000000 00:00 0",
            "f0000000-f0002000 rw-p 00000000 00:00 0",
            "fffff000-100001000 rw-p 00000000 00:00 0",
            "7ffe00000000-7ffe00002000 rw-p 00000000 00:00 0",
        ];

        // Act
        var regions = EditorProcessMemory.ParseLinuxRegions(maps).ToArray();

        // Assert
        regions.Should().Equal(new EditorMemoryRegion(0x10000000, 4096),
            new EditorMemoryRegion(unchecked((nint)0xF0000000L), 8192),
            new EditorMemoryRegion(unchecked((nint)0xFFFFF000L), 4096));
    }
}
