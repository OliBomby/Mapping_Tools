using System.Text;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
///     Serializes project JSON before writing directly to its destination
///     without temporary sibling files.
/// </summary>
public sealed class FileSystemProjectStore : IProjectStore
{
    private static readonly Encoding utf8WithoutByteOrderMark = new UTF8Encoding(false);
    private readonly IProjectSerializer serializer;

    /// <summary>
    ///     Creates a filesystem store for the supplied project-document format.
    /// </summary>
    /// <param name="serializer">
    ///     Encodes and reconstructs project models; production accepts current
    ///     and legacy documents but writes only the current format.
    /// </param>
    public FileSystemProjectStore(IProjectSerializer serializer)
    {
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>Reads a project document while retaining additional host-owned metadata.</summary>
    /// <param name="path">The existing project document.</param>
    /// <param name="cancellationToken">Cancels reading the document.</param>
    /// <returns>The original serialized document.</returns>
    public Task<string> LoadDocumentAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(Path.GetFullPath(path), utf8WithoutByteOrderMark, cancellationToken);

    /// <summary>Persists an already serialized project document, including additional host-owned metadata.</summary>
    /// <param name="path">The destination project file.</param>
    /// <param name="document">The complete serialized document.</param>
    /// <param name="overwrite">Whether an existing destination may be replaced.</param>
    /// <param name="cancellationToken">Cancels before opening the destination.</param>
    /// <returns>A task that completes after the document is written.</returns>
    public Task SaveDocumentAsync(string path, string document, bool overwrite, CancellationToken cancellationToken = default)
    {
        string destination = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        return PhysicalFileWriter.WriteTextAsync(destination, document, utf8WithoutByteOrderMark, cancellationToken, overwrite);
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
    ///     Serializes and encodes before touching the destination, then writes
    ///     the complete document directly without temporary sibling files.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="path">The local destination JSON file.</param>
    /// <param name="project">The complete non-null project snapshot.</param>
    /// <param name="cancellationToken">
    ///     Cancels preparation before the destination is opened. Once started,
    ///     the destination write completes without cancellation.
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

        await SaveDocumentAsync(path, json, true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync<TProject>(
        ToolConfigSchema schema,
        string path,
        TProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        string json = serializer.Serialize(schema, project);
        cancellationToken.ThrowIfCancellationRequested();

        await SaveDocumentAsync(path, json, true, cancellationToken).ConfigureAwait(false);
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
        string json = await LoadDocumentAsync(path, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return serializer.Deserialize<TProject>(json);
    }

    /// <inheritdoc />
    public async Task<TProject> LoadAsync<TProject>(
        ToolConfigSchema schema,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string json = await LoadDocumentAsync(path, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return serializer.Deserialize<TProject>(schema, json);
    }
}
