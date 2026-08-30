using System.Text.Json.Nodes;

namespace Mapping_Tools.Application.Projects.Contracts;

/// <summary>
///     Transforms one tool configuration document to the next version of the
///     same tool-owned schema.
/// </summary>
/// <remarks>
///     Migrations are deliberately scoped to a <see cref="ToolConfigSchema" />.
///     A migration must only change the supplied JSON object and must not rely
///     on the CLR type used to deserialize the final configuration.
/// </remarks>
public interface IConfigMigration
{
    /// <summary>
    ///     Gets the version produced by this migration.
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    ///     Applies the migration in place to a version immediately before
    ///     <see cref="ToVersion" />.
    /// </summary>
    /// <param name="document">The mutable root object being migrated.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is null.</exception>
    void Apply(JsonObject document);
}
