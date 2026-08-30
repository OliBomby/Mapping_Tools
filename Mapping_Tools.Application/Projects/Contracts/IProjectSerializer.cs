using Mapping_Tools.Application.Projects.Models;

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
    ///     Serializes a project using the supplied tool-owned schema and its
    ///     current version.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="schema">The schema that owns the document identity and migrations.</param>
    /// <param name="project">The non-null project snapshot to encode.</param>
    /// <returns>The complete JSON document.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="schema" /> or <paramref name="project" /> is null.
    /// </exception>
    string Serialize<TProject>(ToolConfigSchema schema, TProject project)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return Serialize(project);
    }

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

    /// <summary>
    ///     Deserializes a project after applying only migrations owned by the
    ///     supplied tool schema.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model expected by the caller.</typeparam>
    /// <param name="schema">The schema that owns the document identity and migrations.</param>
    /// <param name="json">The complete JSON document.</param>
    /// <returns>The reconstructed project instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="schema" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="json" /> is empty or whitespace.</exception>
    TProject Deserialize<TProject>(ToolConfigSchema schema, string json)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return Deserialize<TProject>(json);
    }
}
