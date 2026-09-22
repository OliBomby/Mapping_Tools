using System.Text;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Editor.Mtipc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor.Mtipc;

[TestClass]
public sealed class MtipcClientTests
{
    [TestMethod]
    public void ReadBeatmap_WithFramedSocketResponses_UsesHelloAndReturnsBeatmap()
    {
        // Arrange
        var stream = new ScriptedStream(
            CreateFrame(writer => writer.Write(1337)),
            CreateFrame(writer =>
            {
                writer.Write(1.4);
                writer.Write(1.25);
                writer.Write(9f);
                writer.Write(4f);
                writer.Write(8f);
                writer.Write(6f);
                writer.Write("Test Map");
                writer.Write("test.osu");
                writer.Write(1234);
                writer.Write(0f);
                writer.Write(0f);
            }));
        var sut = new MtipcClient(_ => new MtipcConnection(stream, isPipe: false));

        // Act
        MtipcBeatmapData actual = sut.ReadBeatmap(CancellationToken.None);

        // Assert
        actual.SliderMultiplier.Should().Be(1.4);
        actual.SliderTickRate.Should().Be(1.25);
        actual.ApproachRate.Should().Be(9);
        actual.CircleSize.Should().Be(4);
        actual.PreviewTime.Should().Be(1234);
        actual.ContainingFolder.Should().Be("Test Map");
        actual.Filename.Should().Be("test.osu");
        stream.Writes.Should().Equal(
            CreateFrame(writer => writer.Write((int)MtipcMessageType.Hello))
                .Concat(CreateFrame(writer => writer.Write((int)MtipcMessageType.ReadBeatmap))));
    }

    [TestMethod]
    public void ReadObjects_WithProtocolV1SliderPayload_PreservesStartAndEndPositions()
    {
        // Arrange
        var stream = new ScriptedStream(
            CreateFrame(writer => writer.Write(1337)),
            CreateFrame(writer =>
            {
                writer.Write(1);
                writer.Write(116.999996429443);
                writer.Write(1345);
                writer.Write(1687);
                writer.Write(6);
                writer.Write(0);
                writer.Write(1);
                writer.Write(360f);
                writer.Write(55f);
                writer.Write(string.Empty);
                writer.Write(0);
                writer.Write(2);
                writer.Write(0);
                writer.Write(false);
                writer.Write(512f);
                writer.Write(384f);
                writer.Write(true);
                writer.Write(200d);
                writer.Write(3);
                writer.Write(300f);
                writer.Write(350f);
                writer.Write(1);
                writer.Write(360f);
                writer.Write(55f);
            }));
        var sut = new MtipcClient(_ => new MtipcConnection(stream, isPipe: false));

        // Act
        IReadOnlyList<MtipcHitObjectData> actual = sut.ReadObjects(CancellationToken.None);

        // Assert
        actual.Should().ContainSingle();
        actual[0].SpatialLength.Should().BeApproximately(117, 0.0001);
        actual[0].StartTime.Should().Be(1345);
        actual[0].EndTime.Should().Be(1687);
        actual[0].Type.Should().Be(6);
        actual[0].Position.Should().Be(new Vector2(360, 55));
        actual[0].EndPosition.Should().Be(new Vector2(300, 350));
        actual[0].IsSelected.Should().BeFalse();
        actual[0].CurveType.Should().Be(3);
        actual[0].CurvePoints.Should().Equal(new Vector2(360, 55));
    }

    [TestMethod]
    public void ReadEditorTime_WithProtocolV1IntegerResponse_ReturnsMilliseconds()
    {
        // Arrange
        var stream = new ScriptedStream(
            CreateFrame(writer => writer.Write(1337)),
            CreateFrame(writer => writer.Write(5716)));
        var sut = new MtipcClient(_ => new MtipcConnection(stream, isPipe: false));

        // Act
        double actual = sut.ReadEditorTime(CancellationToken.None);

        // Assert
        actual.Should().Be(5716);
    }

    [TestMethod]
    public void ReadBeatmapThenReadEditorTime_OnSeparateClients_PerformsSingleHandshake()
    {
        // Arrange
        var stream = new ScriptedStream(
            CreateFrame(writer => writer.Write(1337)),
            CreateFrame(writer =>
            {
                writer.Write(1.4);
                writer.Write(1.25);
                writer.Write(9f);
                writer.Write(4f);
                writer.Write(8f);
                writer.Write(6f);
                writer.Write("Test Map");
                writer.Write("test.osu");
                writer.Write(1234);
                writer.Write(0f);
                writer.Write(0f);
            }),
            CreateFrame(writer => writer.Write(5716)));
        var session = new MtipcClient.MtipcSession(_ => new MtipcConnection(stream, isPipe: false));
        var beatmapClient = new MtipcClient(session);
        var editorTimeClient = new MtipcClient(session);

        // Act
        _ = beatmapClient.ReadBeatmap(CancellationToken.None);
        double actualEditorTime = editorTimeClient.ReadEditorTime(CancellationToken.None);

        // Assert
        actualEditorTime.Should().Be(5716);
        stream.Writes.Should().Equal(
            CreateFrame(writer => writer.Write((int)MtipcMessageType.Hello))
                .Concat(CreateFrame(writer => writer.Write((int)MtipcMessageType.ReadBeatmap)))
                .Concat(CreateFrame(writer => writer.Write((int)MtipcMessageType.EditorTime))));
    }

    private static byte[] CreateFrame(Action<BinaryWriter> writePayload)
    {
        using var payload = new MemoryStream();
        using (var payloadWriter = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
            writePayload(payloadWriter);

        using var frame = new MemoryStream();
        using (var frameWriter = new BinaryWriter(frame, Encoding.UTF8, leaveOpen: true))
        {
            frameWriter.Write((int)payload.Length);
            frameWriter.Write(payload.ToArray());
        }

        return frame.ToArray();
    }

    private sealed class ScriptedStream : Stream
    {
        private readonly MemoryStream responses;
        private readonly MemoryStream requests = new();

        public ScriptedStream(params byte[][] responseFrames)
        {
            responses = new MemoryStream(responseFrames.SelectMany(frame => frame).ToArray());
        }

        public byte[] Writes => requests.ToArray();

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => responses.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => requests.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
        }
    }
}
