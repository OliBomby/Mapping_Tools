using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

internal sealed class MtipcClient
{
    private const int HandshakeMagic = 1337;
    private const int ConnectTimeoutMilliseconds = 1000;
    private static readonly MtipcSession defaultSession = new(Connect);
    private readonly MtipcSession session;

    public MtipcClient() : this(defaultSession)
    {
    }

    internal MtipcClient(Func<CancellationToken, MtipcConnection> connectionProvider) : this(new MtipcSession(connectionProvider))
    {
    }

    internal MtipcClient(MtipcSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

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
        MtipcMessageType.EditorTime, static reader => (double)reader.ReadInt32(), cancellationToken);

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
        // Protocol version 1 does not include a custom sample set field.
        const int customSampleSet = 0;
        bool isSelected = reader.ReadBoolean();
        _ = reader.ReadSingle();
        _ = reader.ReadSingle();

        int curveType = 0;
        Vector2 endPosition = position;
        IReadOnlyList<Vector2> curvePoints = [];
        IReadOnlyList<int> soundTypeList = [];
        IReadOnlyList<int> sampleSetList = [];
        IReadOnlyList<int> sampleSetAdditionsList = [];

        if ((type & 2) != 0)
        {
            bool unifiedSoundAddition = reader.ReadBoolean();
            _ = reader.ReadDouble();
            curveType = reader.ReadInt32();
            endPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            curvePoints = ReadPoints(reader);
            if (!unifiedSoundAddition)
            {
                soundTypeList = ReadInts(reader);
                sampleSetList = ReadInts(reader);
                sampleSetAdditionsList = ReadInts(reader);
            }
        }

        return new MtipcHitObjectData(spatialLength, startTime, endTime, type, soundType, segmentCount, position,
            endPosition, sampleFile, sampleVolume, sampleSet, sampleSetAdditions, customSampleSet, isSelected,
            curveType, curvePoints, soundTypeList, sampleSetList, sampleSetAdditionsList);
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

    private T Send<T>(MtipcMessageType messageType, Func<BinaryReader, T> readResponse, CancellationToken cancellationToken) =>
        session.Send(messageType, readResponse, cancellationToken);

    internal sealed class MtipcSession
    {
        private readonly object synchronization = new();
        private readonly Func<CancellationToken, MtipcConnection> connectionProvider;
        private MtipcConnection? connection;

        public MtipcSession(Func<CancellationToken, MtipcConnection> connectionProvider)
        {
            this.connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        public T Send<T>(MtipcMessageType messageType, Func<BinaryReader, T> readResponse, CancellationToken cancellationToken)
        {
            lock (synchronization)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MtipcConnection activeConnection = GetConnection(cancellationToken);
                try
                {
                    activeConnection.WriteMessage((int)messageType);
                    cancellationToken.ThrowIfCancellationRequested();
                    using BinaryReader response = activeConnection.ReadMessage();
                    return readResponse(response);
                }
                catch
                {
                    Disconnect();
                    throw;
                }
            }
        }

        private MtipcConnection GetConnection(CancellationToken cancellationToken)
        {
            if (connection is not null) return connection;

            MtipcConnection newConnection = connectionProvider(cancellationToken);
            try
            {
                newConnection.SetTimeout(ConnectTimeoutMilliseconds);
                newConnection.WriteMessage((int)MtipcMessageType.Hello);
                using BinaryReader handshake = newConnection.ReadMessage();
                if (handshake.ReadInt32() != HandshakeMagic)
                    throw new InvalidDataException("MTIPC returned an invalid handshake response.");

                connection = newConnection;
                return newConnection;
            }
            catch
            {
                newConnection.Dispose();
                throw;
            }
        }

        private void Disconnect()
        {
            connection?.Dispose();
            connection = null;
        }
    }

    private static MtipcConnection Connect(CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var pipe = new NamedPipeClientStream(".", "mtipc", PipeDirection.InOut, PipeOptions.WriteThrough);
                pipe.Connect(ConnectTimeoutMilliseconds);
                pipe.ReadMode = PipeTransmissionMode.Message;
                return new MtipcConnection(pipe, isPipe: true);
            }
            catch (Exception exception) when (exception is IOException or TimeoutException or UnauthorizedAccessException)
            {
                lastException = exception;
            }
        }
        else
        {
            try { return new MtipcConnection(ConnectSocket(new UnixDomainSocketEndPoint("/tmp/mtipc.sock"), cancellationToken), isPipe: false); }
            catch (Exception exception) when (exception is SocketException or IOException or TimeoutException)
            {
                lastException = exception;
            }
        }

        try { return new MtipcConnection(ConnectSocket(new IPEndPoint(IPAddress.Loopback, 41337), cancellationToken), isPipe: false); }
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
