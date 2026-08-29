namespace Mapping_Tools.Application.Projects.Contracts;

/// <summary>
///     Converts typed feature projects to and from their persisted JSON document.
/// </summary>
public interface IProjectSerializer
{
    /// <summary>
    ///     Serializes a complete project without writing it to a filesystem target.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="project">The non-null project snapshot to encode.</param>
    /// <returns>The complete JSON document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="project" /> is null.</exception>
    string Serialize<TProject>(TProject project);

    /// <summary>
    ///     Deserializes a current or legacy project document and rejects a JSON
    ///     <c>null</c> root.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model expected by the caller.</typeparam>
    /// <param name="json">The complete JSON document.</param>
    /// <returns>The reconstructed project instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="json" /> is empty or whitespace.</exception>
    /// <exception cref="InvalidDataException">The document contains a JSON <c>null</c> root.</exception>
    TProject Deserialize<TProject>(string json);
}
