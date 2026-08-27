using System.Text;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Writes physical files through same-directory temporary files before
///     replacing the destination.
/// </summary>
internal static class PhysicalAtomicFileWriter
{
    private const int bufferSize = 81920;

    internal static Encoding Utf8WithoutBom { get; } = new UTF8Encoding(false);

    public static void WriteText(
        string destinationPath,
        string contents,
        Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(encoding);

        string fullDestinationPath = PrepareDestination(destinationPath);
        string temporaryPath = CreateTemporarySibling(fullDestinationPath);
        try
        {
            using (FileStream stream = OpenTemporaryFile(temporaryPath))
            {
                using (StreamWriter writer = new(stream, encoding, bufferSize, leaveOpen: true))
                {
                    writer.Write(contents);
                    writer.Flush();
                }

                stream.Flush(true);
            }

            File.Move(temporaryPath, fullDestinationPath, true);
        }
        finally
        {
            DeleteTemporary(temporaryPath);
        }
    }

    public static async Task WriteTextAsync(
        string destinationPath,
        string contents,
        Encoding encoding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(encoding);

        string fullDestinationPath = PrepareDestination(destinationPath);
        string temporaryPath = CreateTemporarySibling(fullDestinationPath);
        try
        {
            await using (FileStream stream = OpenTemporaryFile(temporaryPath))
            {
                await using (StreamWriter writer = new(
                                   stream,
                                   encoding,
                                   bufferSize,
                                   leaveOpen: true))
                {
                    await writer.WriteAsync(contents.AsMemory(), cancellationToken)
                        .ConfigureAwait(false);
                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullDestinationPath, true);
        }
        finally
        {
            DeleteTemporary(temporaryPath);
        }
    }

    internal static void WriteLines(
        string destinationPath,
        IEnumerable<string> lines,
        Encoding encoding,
        string newLine)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(newLine);

        string fullDestinationPath = PrepareDestination(destinationPath);
        string temporaryPath = CreateTemporarySibling(fullDestinationPath);
        try
        {
            using (FileStream stream = OpenTemporaryFile(temporaryPath))
            {
                using (StreamWriter writer = new(stream, encoding, bufferSize, leaveOpen: true)
                {
                    NewLine = newLine,
                })
                {
                    foreach (string line in lines)
                        writer.WriteLine(line);

                    writer.Flush();
                }

                stream.Flush(true);
            }

            File.Move(temporaryPath, fullDestinationPath, true);
        }
        finally
        {
            DeleteTemporary(temporaryPath);
        }
    }

    public static async Task WriteLinesAsync(
        string destinationPath,
        IReadOnlyList<string> lines,
        Encoding encoding,
        string newLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(encoding);
        ArgumentNullException.ThrowIfNull(newLine);

        string fullDestinationPath = PrepareDestination(destinationPath);
        string temporaryPath = CreateTemporarySibling(fullDestinationPath);
        try
        {
            await using (FileStream stream = OpenTemporaryFile(temporaryPath))
            {
                await using (StreamWriter writer = new(
                                   stream,
                                   encoding,
                                   bufferSize,
                                   leaveOpen: true)
                {
                    NewLine = newLine,
                })
                {
                    foreach (string line in lines)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (line is null)
                            await writer.WriteAsync(newLine.AsMemory(), cancellationToken)
                                .ConfigureAwait(false);
                        else
                            await writer.WriteLineAsync(line.AsMemory(), cancellationToken)
                                .ConfigureAwait(false);
                    }

                    await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullDestinationPath, true);
        }
        finally
        {
            DeleteTemporary(temporaryPath);
        }
    }

    public static async Task CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        string fullSourcePath = PrepareSource(sourcePath);
        string fullDestinationPath = PrepareDestination(destinationPath);
        string temporaryPath = CreateTemporarySibling(fullDestinationPath);
        try
        {
            await using (FileStream source = new(
                               fullSourcePath,
                               FileMode.Open,
                               FileAccess.Read,
                               FileShare.Read,
                               bufferSize,
                               FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (FileStream destination = OpenTemporaryFile(temporaryPath))
            {
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullDestinationPath, true);
        }
        finally
        {
            DeleteTemporary(temporaryPath);
        }
    }

    private static FileStream OpenTemporaryFile(string path)
    {
        return new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
    }

    private static string PrepareSource(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }

    private static string PrepareDestination(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (Path.GetDirectoryName(fullPath) is null)
            throw new ArgumentException("The destination path has no parent directory.", nameof(path));

        return fullPath;
    }

    private static string CreateTemporarySibling(string destinationPath)
    {
        string directory = Path.GetDirectoryName(destinationPath)
                           ?? throw new DirectoryNotFoundException(
                               $"Path '{destinationPath}' does not have a parent directory.");
        string fileName = Path.GetFileName(destinationPath);
        return Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");
    }

    private static void DeleteTemporary(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
