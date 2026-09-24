using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc2;

/// <summary>Maintains a serialized MTIPC2 connection to osu!.</summary>
public sealed class Mtipc2Client
{
    private readonly object gate = new();
    private Stream? stream;
    private bool isPipe;

    /// <summary>Creates a client that connects on its first request.</summary>
    public Mtipc2Client() { }

    internal Mtipc2CurrentBeatmap CurrentBeatmap()
    {
        JsonElement value = Send(new { op = "currentBeatmap" });
        return new Mtipc2CurrentBeatmap(
            value.GetProperty("fileId").ValueKind == JsonValueKind.Null ? null : value.GetProperty("fileId").GetString(),
            value.GetProperty("context").GetString()!);
    }

    internal Mtipc2EditorState? EditorState()
    {
        JsonElement value = Send(new { op = "editorState" });
        return value.ValueKind == JsonValueKind.Null ? null : new Mtipc2EditorState(
            value.GetProperty("fileId").GetString()!,
            value.GetProperty("timeMs").GetInt32(),
            value.GetProperty("selectedIndices").EnumerateArray().Select(item => item.GetInt32()).ToArray());
    }

    internal IReadOnlyList<Mtipc2Mapset> ListMapsets() => Send(new { op = "listMapsets" })
        .EnumerateArray().Select(item => new Mtipc2Mapset(item.GetProperty("id").GetString()!, item.GetProperty("name").GetString()!)).ToArray();

    internal IReadOnlyList<Mtipc2File> ListFiles(string mapsetId) => Send(new { op = "listFiles", mapsetId })
        .EnumerateArray().Select(item => new Mtipc2File(item.GetProperty("id").GetString()!, item.GetProperty("name").GetString()!,
            item.GetProperty("kind").GetString()!)).ToArray();

    internal byte[] ReadFile(string fileId) => Convert.FromBase64String(
        Send(new { op = "readFile", fileId }).GetProperty("contentBase64").GetString()!);

    internal IReadOnlyList<Mtipc2CommitFile> Commit(params Mtipc2Change[] changes)
    {
        JsonElement value = Send(new
        {
            op = "commit",
            changes = changes.Select(change => new
            {
                kind = change.Kind,
                fileId = change.FileId,
                contentBase64 = change.Content is null ? null : Convert.ToBase64String(change.Content),
            }).ToArray(),
        });
        return value.GetProperty("files").EnumerateArray()
            .Select(item => new Mtipc2CommitFile(item.GetProperty("requestedId").GetString()!, item.GetProperty("fileId").GetString()!))
            .ToArray();
    }

    private JsonElement Send(object request)
    {
        lock (gate)
        {
            try
            {
                if (stream is null)
                {
                    Connect();
                    JsonElement hello = Exchange(new { op = "hello" });
                    if (hello.GetProperty("protocol").GetString() != "MTIPC2" || hello.GetProperty("version").GetInt32() != 1)
                        throw new InvalidDataException("The server is not MTIPC2 version 1.");
                }
                return Exchange(request);
            }
            catch
            {
                stream?.Dispose();
                stream = null;
                throw;
            }
        }
    }

    private JsonElement Exchange(object request)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(request);
        if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("MTIPC2 request exceeds 64 MiB.");
        if (!isPipe) stream!.Write(BitConverter.GetBytes(bytes.Length));
        stream!.Write(bytes);
        stream.Flush();

        byte[] response = isPipe ? ReadPipeMessage() : ReadSocketMessage();
        using JsonDocument document = JsonDocument.Parse(response);
        JsonElement root = document.RootElement;
        if (!root.GetProperty("ok").GetBoolean())
            throw new IOException(root.GetProperty("error").GetString());
        return root.GetProperty("value").Clone();
    }

    private void Connect()
    {
        if (OperatingSystem.IsWindows())
        {
            var pipe = new NamedPipeClientStream(".", "mtipc2", PipeDirection.InOut);
            try
            {
                pipe.Connect(1000);
                pipe.ReadMode = PipeTransmissionMode.Message;
                stream = pipe;
                isPipe = true;
                return;
            }
            catch (Exception error) when (error is IOException or TimeoutException or UnauthorizedAccessException)
            {
                pipe.Dispose();
            }
        }
        else
        {
            try
            {
                stream = ConnectSocket(new UnixDomainSocketEndPoint("/tmp/mtipc2.sock"));
                isPipe = false;
                return;
            }
            catch (Exception error) when (error is SocketException or IOException or TimeoutException) { }
        }

        stream = ConnectSocket(new IPEndPoint(IPAddress.Loopback, 41338));
        isPipe = false;
    }

    private static NetworkStream ConnectSocket(EndPoint endpoint)
    {
        var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            ReceiveTimeout = 30000,
            SendTimeout = 30000,
        };
        try
        {
            socket.Connect(endpoint);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private byte[] ReadPipeMessage()
    {
        var buffer = new byte[8192];
        using var data = new MemoryStream();
        do
        {
            int count = stream!.Read(buffer);
            if (count == 0) throw new EndOfStreamException("MTIPC2 pipe closed.");
            data.Write(buffer, 0, count);
            if (data.Length > 64 * 1024 * 1024) throw new InvalidDataException("MTIPC2 response exceeds 64 MiB.");
        } while (stream is NamedPipeClientStream pipe && !pipe.IsMessageComplete);
        return data.ToArray();
    }

    private byte[] ReadSocketMessage()
    {
        int length = BitConverter.ToInt32(ReadExact(4));
        if (length is < 0 or > 64 * 1024 * 1024) throw new InvalidDataException("Invalid MTIPC2 response length.");
        return ReadExact(length);
    }

    private byte[] ReadExact(int count)
    {
        var bytes = new byte[count];
        int offset = 0;
        while (offset < bytes.Length)
        {
            int read = stream!.Read(bytes, offset, bytes.Length - offset);
            if (read == 0) throw new EndOfStreamException("MTIPC2 socket closed.");
            offset += read;
        }
        return bytes;
    }
}

internal sealed record Mtipc2CurrentBeatmap(string? FileId, string Context);
internal sealed record Mtipc2EditorState(string FileId, int TimeMs, IReadOnlyList<int> SelectedIndices);
internal sealed record Mtipc2Mapset(string Id, string Name);
internal sealed record Mtipc2File(string Id, string Name, string Kind);
internal sealed record Mtipc2Change(string Kind, string FileId, byte[]? Content = null);
internal sealed record Mtipc2CommitFile(string RequestedId, string FileId);
