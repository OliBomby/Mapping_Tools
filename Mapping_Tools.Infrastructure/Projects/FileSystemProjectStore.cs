using System.Text;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Projects.Contracts;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
///     Persists project JSON through a same-directory temporary file so an
///     interrupted save cannot truncate the previous project.
/// </summary>
public sealed class FileSystemProjectStore : IProjectStore
{
    private static readonly Encoding utf8WithoutByteOrderMark = new UTF8Encoding(false);
    private readonly IProjectSerializer serializer;

    /// <summary>
    ///     Creates a filesystem store for the supplied project-document format.
    /// </summary>
    /// <param name="serializer">
    ///     Encodes and reconstructs project models; production uses the legacy-compatible serializer.
    /// </param>
    public FileSystemProjectStore(IProjectSerializer serializer)
    {
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    ///     Creates the requested local directory and any absent parents.
    /// </summary>
    /// <param name="path">The directory required by a picker or project write.</param>
    public void EnsureDirectoryExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(path);
    }

    /// <summary>
    ///     Serializes before touching the destination, writes a unique sibling
    ///     temporary file, then atomically moves the complete document into place.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="path">The local destination JSON file.</param>
    /// <param name="project">The complete non-null project snapshot.</param>
    /// <param name="cancellationToken">
    ///     Prevents replacement when cancellation is observed before the final move.
    /// </param>
    public async Task SaveAsync<TProject>(
        string path,
        TProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        string json = serializer.Serialize(project);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = Path.GetFullPath(path);
        string parent = Path.GetDirectoryName(fullPath)
                        ?? throw new ArgumentException("The project path has no parent directory.", nameof(path));
        Directory.CreateDirectory(parent);

        string temporaryPath = Path.Combine(
            parent,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                json,
                utf8WithoutByteOrderMark,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    /// <summary>
    ///     Reads the complete UTF-8 document before invoking the serializer, so a
    ///     cancelled read never yields partially reconstructed feature state.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model expected by the caller.</typeparam>
    /// <param name="path">The existing local project file.</param>
    /// <param name="cancellationToken">Cancels the asynchronous file read.</param>
    /// <returns>The non-null reconstructed project.</returns>
    public async Task<TProject> LoadAsync<TProject>(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string json = await File.ReadAllTextAsync(
            Path.GetFullPath(path),
            utf8WithoutByteOrderMark,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return serializer.Deserialize<TProject>(json);
    }
}
