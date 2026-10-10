using System.Text;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Editor.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor.Memory;

[TestClass]
public sealed class StableEditorMemoryReaderTests
{
    [TestMethod]
    public void ReadSnapshot_WithLiveMetadataTimingAndSelection_PreservesValues()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Path.Should().Be(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "123 Artist - Title", "map.osu")));
        snapshot.EditorTime.Should().Be(2222);
        snapshot.ApproachRate.Should().Be(9);
        snapshot.CircleSize.Should().Be(4);
        snapshot.SliderMultiplier.Should().Be(1.4);
        snapshot.SliderTickRate.Should().Be(2);
        snapshot.PreviewTime.Should().Be(12345);
        snapshot.Bookmarks.Should().Equal(250d, 500d);
        var timingPoint = snapshot.TimingPoints.Should().ContainSingle().Which;
        timingPoint.Offset.Should().Be(100);
        timingPoint.MpB.Should().Be(500);
        timingPoint.Kiai.Should().BeTrue();
        timingPoint.OmitFirstBarLine.Should().BeTrue();
        snapshot.SelectedHitObjects.Should().ContainSingle().Which.Should().BeSameAs(snapshot.HitObjects[0]);
        snapshot.HitObjects[0].ObjectType.Should().Be(1 | (3 << 4));
        snapshot.HitObjects[0].Filename.Should().Be("custom.wav");
    }

    [TestMethod]
    public void ReadSnapshot_WithStackedSlider_DestacksAnchorsAndPreservesEdges()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.AddSlider();
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        var slider = snapshot!.HitObjects.Single();
        slider.Pos.Should().Be(new Vector2(100, 100));
        slider.ControlPoints.Select(point => point.Position).Should().Equal(
            Vector2.Zero, new Vector2(50, 25), new Vector2(100, 50));
        slider.ControlPoints[0].Type.Should().Be(PathType.Catmull);
        slider.Repeat.Should().Be(2);
        slider.EndTime.Should().Be(2000);
        slider.TemporalLength.Should().Be(500);
        slider.EdgeHitsounds.Should().Equal(2, 0, 0);
        slider.EdgeSampleSets.Should().Equal(SampleSet.Soft, SampleSet.None, SampleSet.None);
        slider.EdgeAdditionSets.Should().Equal(SampleSet.Drum, SampleSet.None, SampleSet.None);
    }

    [TestMethod]
    public void ReadSnapshot_WithUnifiedSliderSamples_PadsEdgesWithoutReadingMissingLists()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.AddSlider();
        memory.SetByte(memory.HitObjectAddress + 286, 1);
        memory.SetInt(memory.HitObjectAddress + 224, 0);
        memory.SetInt(memory.HitObjectAddress + 228, 0);
        memory.SetInt(memory.HitObjectAddress + 232, 0);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot!.HitObjects.Single().EdgeHitsounds.Should().Equal(0, 0, 0);
    }

    [TestMethod]
    public void ReadSnapshot_WithValuesAboveLegacyValidationLimits_PreservesValues()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.AddSlider();
        memory.SetInt(memory.HitObjectAddress + 32, 9001);
        memory.SetInt(memory.HitObjectAddress + 108, 1001);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot!.HitObjects.Single().Repeat.Should().Be(9001);
        snapshot.HitObjects.Single().SampleVolume.Should().Be(1001);
    }

    [TestMethod]
    public void ReadSnapshot_WithHigh32BitAddresses_DecodesUnsignedPointers()
    {
        // Arrange
        var memory = new EditorMemoryFixture(0xF0000000);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot!.HitObjects.Should().ContainSingle();
        snapshot.TimingPoints.Should().ContainSingle();
    }

    [TestMethod]
    public void ReadSnapshot_WithSignatureAcrossScanChunks_FindsEditor()
    {
        // Arrange
        var memory = new EditorMemoryFixture(editorOffset: 64 * 1024 - 180);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot.Should().NotBeNull();
    }

    [TestMethod]
    public void ReadSnapshot_WithClosedEditor_ReturnsNoSnapshot()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.SetByte(memory.EditorAddress + 209, 1);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot.Should().BeNull();
    }

    [TestMethod]
    public void ReadSnapshot_WithEditorClosedAfterFirstSnapshot_DiscardsCachedAddress()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        var sut = new StableEditorMemoryReader(memory);
        sut.ReadSnapshot(Path.GetTempPath()).Should().NotBeNull();
        memory.SetByte(memory.EditorAddress + 209, 1);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot.Should().BeNull();
    }

    [TestMethod]
    public void ReadSnapshot_WithEmptyObjectList_ReturnsEmptyBeatmap()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.SetInt(memory.ObjectListAddress + 12, 0);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot!.HitObjects.Should().BeEmpty();
        snapshot.SelectedHitObjects.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void ReadSnapshot_WithInvalidListSize_ThrowsInvalidDataException(int count)
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.SetInt(memory.ObjectListAddress + 12, count);
        var sut = new StableEditorMemoryReader(memory);

        // Act
        Action act = () => sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    public void ReadSnapshot_WithIncompleteObjectRead_ThrowsInsteadOfReturningPartialState()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.ReadAllowed = (address, _) => address != memory.HitObjectAddress;
        var sut = new StableEditorMemoryReader(memory);

        // Act
        Action act = () => sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    public void ReadSnapshot_WithOneCollectionChange_RetriesWholeSnapshot()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        int headerReads = 0;
        memory.ReadAllowed = (address, length) =>
        {
            if (address == memory.ObjectListAddress && length == 20 && ++headerReads == 2)
                memory.SetInt(address + 16, 1);
            return true;
        };
        var sut = new StableEditorMemoryReader(memory);

        // Act
        var snapshot = sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        snapshot!.HitObjects.Should().ContainSingle();
        headerReads.Should().Be(4);
    }

    [TestMethod]
    public void ReadSnapshot_WithContinuouslyChangingCollection_RejectsBothAttempts()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        int headerReads = 0;
        memory.ReadAllowed = (address, length) =>
        {
            if (address == memory.ObjectListAddress && length == 20 && ++headerReads % 2 == 0)
                memory.SetInt(address + 16, headerReads);
            return true;
        };
        var sut = new StableEditorMemoryReader(memory);

        // Act
        Action act = () => sut.ReadSnapshot(Path.GetTempPath());

        // Assert
        act.Should().Throw<InvalidDataException>();
        headerReads.Should().Be(4);
    }

    [TestMethod]
    public void ReadSnapshot_WithCancelledToken_StopsBeforeReadingMemory()
    {
        // Arrange
        var memory = new EditorMemoryFixture();
        memory.ReadAllowed = (_, _) => throw new InvalidOperationException("Memory should not be read.");
        var sut = new StableEditorMemoryReader(memory);

        // Act
        Action act = () => sut.ReadSnapshot(Path.GetTempPath(), new CancellationToken(true));

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    private sealed class EditorMemoryFixture : IEditorProcessMemory
    {
        private readonly byte[] data = new byte[128 * 1024];
        private readonly long baseAddress;
        private int nextOffset = 80 * 1024;
        internal nint EditorAddress { get; }
        internal nint HitObjectAddress { get; }
        internal nint ObjectListAddress { get; }
        internal Func<nint, int, bool>? ReadAllowed { get; set; }

        internal EditorMemoryFixture(long baseAddress = 0x10000000, int editorOffset = 256)
        {
            this.baseAddress = baseAddress;
            EditorAddress = (nint)(baseAddress + editorOffset);
            SetInt(EditorAddress + 160, 35);
            SetInt(EditorAddress + 164, 20);
            SetInt(EditorAddress + 168, 25);
            SetInt(EditorAddress + 192, 12);
            SetInt(EditorAddress + 184, 2220);
            SetInt(EditorAddress + 188, 2224);

            nint managerAddress = Allocate(80);
            nint beatmapAddress = Allocate(320);
            SetPointer(EditorAddress + 28, managerAddress);
            SetPointer(managerAddress + 48, beatmapAddress);
            SetDouble(beatmapAddress + 8, 1.4);
            SetDouble(beatmapAddress + 16, 2);
            SetFloat(beatmapAddress + 44, 9);
            SetFloat(beatmapAddress + 48, 4);
            SetPointer(beatmapAddress + 120, CreateString("123 Artist - Title"));
            SetPointer(beatmapAddress + 144, CreateString("map.osu"));
            SetInt(beatmapAddress + 288, 12345);

            nint timingPoint = Allocate(48);
            SetDouble(timingPoint + 4, 500);
            SetDouble(timingPoint + 12, 100);
            SetInt(timingPoint + 24, 1);
            SetInt(timingPoint + 28, 4);
            SetInt(timingPoint + 32, 70);
            SetInt(timingPoint + 36, 9);
            SetByte(timingPoint + 40, 1);
            SetPointer(beatmapAddress + 176, CreateList(BitConverter.GetBytes((uint)timingPoint), 4));

            HitObjectAddress = Allocate(336);
            SetInt(HitObjectAddress + 16, 1000);
            SetInt(HitObjectAddress + 20, 1000);
            SetInt(HitObjectAddress + 24, 1);
            SetFloat(HitObjectAddress + 56, 105);
            SetFloat(HitObjectAddress + 60, 105);
            SetPointer(HitObjectAddress + 84, CreateString("custom.wav"));
            SetInt(HitObjectAddress + 96, 3);
            SetByte(HitObjectAddress + 133, 1);
            SetFloat(HitObjectAddress + 140, 100);
            SetFloat(HitObjectAddress + 144, 100);
            ObjectListAddress = CreateList(BitConverter.GetBytes((uint)HitObjectAddress), 4);
            SetPointer(managerAddress + 72, ObjectListAddress);
            SetPointer(managerAddress + 56, CreateList([.. BitConverter.GetBytes(250), .. BitConverter.GetBytes(500)], 4));
        }

        internal void AddSlider()
        {
            SetInt(HitObjectAddress + 24, 2);
            SetInt(HitObjectAddress + 32, 2);
            SetInt(HitObjectAddress + 20, 2000);
            SetDouble(HitObjectAddress + 8, 200);
            byte[] anchors = new byte[24];
            Buffer.BlockCopy(new float[] { 105, 105, 155, 130, 205, 155 }, 0, anchors, 0, anchors.Length);
            SetPointer(HitObjectAddress + 196, CreateList(anchors, 8));
            SetPointer(HitObjectAddress + 224, CreateList(BitConverter.GetBytes(2), 4));
            SetPointer(HitObjectAddress + 228, CreateList(BitConverter.GetBytes(2), 4));
            SetPointer(HitObjectAddress + 232, CreateList(BitConverter.GetBytes(3), 4));
        }

        internal void SetInt(nint address, int value) => Write(address, BitConverter.GetBytes(value));
        internal void SetByte(nint address, byte value) => Write(address, [value]);
        private void SetPointer(nint address, nint value) => Write(address, BitConverter.GetBytes((uint)value));
        private void SetFloat(nint address, float value) => Write(address, BitConverter.GetBytes(value));
        private void SetDouble(nint address, double value) => Write(address, BitConverter.GetBytes(value));

        private void Write(nint address, byte[] value) => value.CopyTo(data, checked((int)(address - baseAddress)));

        private nint Allocate(int length)
        {
            nint result = (nint)(baseAddress + nextOffset);
            nextOffset += (length + 3) & ~3;
            return result;
        }

        private nint CreateString(string value)
        {
            nint address = Allocate(8 + value.Length * 2);
            SetInt(address + 4, value.Length);
            Write(address + 8, Encoding.Unicode.GetBytes(value));
            return address;
        }

        private nint CreateList(byte[] contents, int elementSize)
        {
            nint listAddress = Allocate(20);
            nint arrayAddress = Allocate(8 + contents.Length);
            SetPointer(listAddress + 4, arrayAddress);
            SetInt(listAddress + 12, contents.Length / elementSize);
            SetInt(arrayAddress + 4, contents.Length / elementSize);
            Write(arrayAddress + 8, contents);
            return listAddress;
        }

        public bool TryRead(nint address, Span<byte> destination)
        {
            if (ReadAllowed?.Invoke(address, destination.Length) == false) return false;
            long offset = address - baseAddress;
            if (offset < 0 || offset + destination.Length > data.Length) return false;
            data.AsSpan((int)offset, destination.Length).CopyTo(destination);
            return true;
        }

        public IEnumerable<EditorMemoryRegion> EnumerateWritableRegions() => [new((nint)baseAddress, data.Length)];
    }
}

