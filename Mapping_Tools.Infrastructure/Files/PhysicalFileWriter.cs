using System.Text;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Writes complete buffers directly to their destinations without creating
///     temporary siblings that osu!'s file watcher could observe.
/// </summary>
internal static class PhysicalFileWriter
{
    internal static Encoding Utf8WithoutBom { get; } = new UTF8Encoding(false);

    public static void WriteText(string destinationPath, string contents, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(encoding);
        byte[] bytes = Encode(contents, encoding);
        using var stream = OpenDestination(destinationPath);
        stream.Write(bytes);
        CompleteWrite(stream, bytes.Length);
    }

    public static Task WriteTextAsync(
        string destinationPath,
        string contents,
        Encoding encoding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(encoding);
        cancellationToken.ThrowIfCancellationRequested();
        return WriteBytesAsync(destinationPath, Encode(contents, encoding), cancellationToken);
    }

    public static Task WriteLinesAsync(
        string destinationPath,
        IReadOnlyList<string> lines,
        Encoding encoding,
        string newLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(newLine);
        cancellationToken.ThrowIfCancellationRequested();

        StringBuilder contents = new();
        foreach (string line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            contents.Append(line).Append(newLine);
        }

        return WriteTextAsync(destinationPath, contents.ToString(), encoding, cancellationToken);
    }

    public static async Task CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        // Finish reading before opening the destination, including when copying to itself.
        byte[] bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        await WriteBytesAsync(destinationPath, bytes, cancellationToken).ConfigureAwait(false);
    }

    private static byte[] Encode(string contents, Encoding encoding)
    {
        byte[] preamble = encoding.GetPreamble();
        byte[] bytes = new byte[preamble.Length + encoding.GetByteCount(contents)];
        preamble.CopyTo(bytes, 0);
        encoding.GetBytes(contents.AsSpan(), bytes.AsSpan(preamble.Length));
        return bytes;
    }

    private static async Task WriteBytesAsync(
        string destinationPath,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = OpenDestination(destinationPath);
        // Once the destination is open, finish the write even if cancellation arrives.
        await stream.WriteAsync(bytes, CancellationToken.None).ConfigureAwait(false);
        CompleteWrite(stream, bytes.Length);
    }

    private static FileStream OpenDestination(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        // Avoid truncating on open: write once, then trim any previous trailing bytes.
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
    }

    private static void CompleteWrite(FileStream stream, int length)
    {
        if (stream.Length != length) stream.SetLength(length);
        stream.Flush(true);
    }
}
