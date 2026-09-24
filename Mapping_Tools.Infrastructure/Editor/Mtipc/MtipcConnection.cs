using System.IO.Pipes;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

internal sealed class MtipcConnection : IDisposable
{
    private readonly Stream stream;
    private readonly bool isPipe;

    public MtipcConnection(Stream stream, bool isPipe)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        this.isPipe = isPipe;
    }

    public void SetTimeout(int milliseconds)
    {
        if (stream.CanTimeout)
        {
            stream.ReadTimeout = milliseconds;
            stream.WriteTimeout = milliseconds;
        }
    }

    public void WriteMessage(int messageType)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (!isPipe) writer.Write(sizeof(int));
        writer.Write(messageType);
        writer.Flush();
    }

    public BinaryReader ReadMessage()
    {
        byte[] payload = isPipe ? ReadPipeMessage() : ReadSocketMessage();
        return new BinaryReader(new MemoryStream(payload, writable: false));
    }

    public void Dispose() => stream.Dispose();

    private byte[] ReadPipeMessage()
    {
        byte[] buffer = new byte[1024];
        using var payload = new MemoryStream();
        do
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0) throw new EndOfStreamException("MTIPC pipe closed while reading.");
            payload.Write(buffer, 0, bytesRead);
        }
        while (stream is NamedPipeClientStream pipe && !pipe.IsMessageComplete);

        return payload.ToArray();
    }

    private byte[] ReadSocketMessage()
    {
        int length = BitConverter.ToInt32(ReadExact(sizeof(int)), 0);
        if (length < 0) throw new InvalidDataException($"MTIPC returned an invalid message length: {length}.");
        return ReadExact(length);
    }

    private byte[] ReadExact(int length)
    {
        byte[] bytes = new byte[length];
        int offset = 0;
        while (offset < bytes.Length)
        {
            int bytesRead = stream.Read(bytes, offset, bytes.Length - offset);
            if (bytesRead == 0) throw new EndOfStreamException("MTIPC socket closed while reading.");
            offset += bytesRead;
        }

        return bytes;
    }
}
