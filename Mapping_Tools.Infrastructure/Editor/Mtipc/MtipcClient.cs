using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

internal sealed class MtipcClient
{
    private const int HandshakeMagic = 1337;
    private const int ConnectTimeoutMilliseconds = 1000;

    public MtipcBeatmapData ReadBeatmap(CancellationToken cancellationToken) => Send(
        MtipcMessageType.ReadBeatmap,
        reader =>
        {
            double sliderMultiplier = reader.ReadDouble();
            double sliderTickRate = reader.ReadDouble();
            double approachRate = reader.ReadSingle();
            double circleSize = reader.ReadSingle();
            _ = reader.ReadSingle();
            _ = reader.ReadSingle();
            string containingFolder = reader.ReadString();
            string filename = reader.ReadString();
            int previewTime = reader.ReadInt32();
            _ = reader.ReadSingle();
            _ = reader.ReadSingle();
            return new MtipcBeatmapData(
                sliderMultiplier,
                sliderTickRate,
                approachRate,
                circleSize,
                previewTime,
                containingFolder,
                filename);
        }, cancellationToken);

    public IReadOnlyList<double> ReadBookmarks(CancellationToken cancellationToken) => Send(
        MtipcMessageType.ReadBookmarks,
        reader => ReadCounted(reader, static value => (double)value.ReadInt32()), cancellationToken);

    public IReadOnlyList<MtipcControlPointData> ReadControlPoints(CancellationToken cancellationToken) => Send(
        MtipcMessageType.ReadControlPoints,
        reader => ReadCounted(reader, static value => new MtipcControlPointData(value.ReadDouble(), value.ReadDouble(),
            value.ReadInt32(), value.ReadInt32(), value.ReadInt32(), value.ReadInt32(), value.ReadInt32(), value.ReadBoolean())), cancellationToken);

    public IReadOnlyList<MtipcHitObjectData> ReadObjects(CancellationToken cancellationToken) => Send(
        MtipcMessageType.ReadObjects, reader => ReadCounted(reader, ReadObject), cancellationToken);

    public double ReadEditorTime(CancellationToken cancellationToken) => Send(
        MtipcMessageType.EditorTime, static reader => reader.ReadDouble(), cancellationToken);

    public void ReloadEditor(CancellationToken cancellationToken) => Send(
        MtipcMessageType.ReloadEditor, static _ => true, cancellationToken);

    private static MtipcHitObjectData ReadObject(BinaryReader reader)
    {
        double spatialLength = reader.ReadDouble();
        int startTime = reader.ReadInt32();
        int endTime = reader.ReadInt32();
        int type = reader.ReadInt32();
        int soundType = reader.ReadInt32();
        int segmentCount = reader.ReadInt32();
        var position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        string? sampleFile = reader.ReadString();
        int sampleVolume = reader.ReadInt32();
        int sampleSet = reader.ReadInt32();
        int sampleSetAdditions = reader.ReadInt32();
        int customSampleSet = reader.ReadInt32();
        bool isSelected = reader.ReadBoolean();
        position = new Vector2(reader.ReadSingle(), reader.ReadSingle());

        int curveType = 0;
        IReadOnlyList<Vector2> curvePoints = [];
        IReadOnlyList<int> soundTypeList = [];
        IReadOnlyList<int> sampleSetList = [];
        IReadOnlyList<int> sampleSetAdditionsList = [];

        if ((type & 2) != 0)
        {
            bool unifiedSoundAddition = reader.ReadBoolean();
            _ = reader.ReadDouble();
            curveType = reader.ReadInt32();
            _ = reader.ReadSingle();
            _ = reader.ReadSingle();
            curvePoints = ReadPoints(reader);
            if (!unifiedSoundAddition)
            {
                soundTypeList = ReadInts(reader);
                sampleSetList = ReadInts(reader);
                sampleSetAdditionsList = ReadInts(reader);
            }
        }

        return new MtipcHitObjectData(spatialLength, startTime, endTime, type, soundType, segmentCount, position,
            sampleFile, sampleVolume, sampleSet, sampleSetAdditions, customSampleSet, isSelected, curveType,
            curvePoints, soundTypeList, sampleSetList, sampleSetAdditionsList);
    }

    private static Vector2[] ReadPoints(BinaryReader reader)
    {
        int count = ReadCount(reader);
        var values = new Vector2[count];
        for (int index = 0; index < count; index++) values[index] = new(reader.ReadSingle(), reader.ReadSingle());
        return values;
    }

    private static int[] ReadInts(BinaryReader reader)
    {
        int count = ReadCount(reader);
        var values = new int[count];
        for (int index = 0; index < count; index++) values[index] = reader.ReadInt32();
        return values;
    }

    private static T[] ReadCounted<T>(BinaryReader reader, Func<BinaryReader, T> read)
    {
        int count = ReadCount(reader);
        var values = new T[count];
        for (int index = 0; index < count; index++) values[index] = read(reader);
        return values;
    }

    private static int ReadCount(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count is < 0 or > 1_000_000) throw new InvalidDataException("MTIPC returned an invalid collection length.");
        return count;
    }

    private T Send<T>(MtipcMessageType messageType, Func<BinaryReader, T> readResponse, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using Stream stream = Connect(cancellationToken);
        if (stream.CanTimeout)
        {
            stream.ReadTimeout = ConnectTimeoutMilliseconds;
            stream.WriteTimeout = ConnectTimeoutMilliseconds;
        }
        using var reader = new BinaryReader(stream);
        using var writer = new BinaryWriter(stream);
        writer.Write(HandshakeMagic);
        writer.Flush();
        _ = reader.ReadInt32();
        writer.Write((int)messageType);
        writer.Flush();
        cancellationToken.ThrowIfCancellationRequested();
        return readResponse(reader);
    }

    private static Stream Connect(CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var pipe = new NamedPipeClientStream(".", "mtipc", PipeDirection.InOut, PipeOptions.WriteThrough);
                pipe.Connect(ConnectTimeoutMilliseconds);
                return pipe;
            }
            catch (Exception exception) when (exception is IOException or TimeoutException or UnauthorizedAccessException)
            {
                lastException = exception;
            }
        }
        else
        {
            try { return ConnectSocket(new UnixDomainSocketEndPoint("/tmp/mtipc.sock"), cancellationToken); }
            catch (Exception exception) when (exception is SocketException or IOException or TimeoutException)
            {
                lastException = exception;
            }
        }

        try { return ConnectSocket(new IPEndPoint(IPAddress.Loopback, 41337), cancellationToken); }
        catch (Exception exception) when (exception is SocketException or IOException or TimeoutException)
        {
            throw new IOException("MTIPC server is unavailable.", exception ?? lastException);
        }
    }

    private static NetworkStream ConnectSocket(EndPoint endpoint, CancellationToken cancellationToken)
    {
        var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            if (!socket.ConnectAsync(endpoint, cancellationToken).AsTask().Wait(ConnectTimeoutMilliseconds, cancellationToken))
                throw new TimeoutException("MTIPC connection timed out.");

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
