using Mapping_Tools.Application.Projects.Contracts;

namespace Mapping_Tools.Application.Projects.Models;

/// <summary>
///     Describes the stable identity and version history of one tool-owned
///     configuration document.
/// </summary>
public sealed class ToolConfigSchema
{
    private const int initial_version = 1;

    /// <summary>
    ///     Creates a schema with an optional ordered set of version migrations.
    /// </summary>
    /// <param name="id">
    ///     The stable document identifier written to <c>$schema</c>. It should
    ///     be owned by one tool and must remain unchanged after release.
    /// </param>
    /// <param name="migrations">
    ///     Migrations keyed by their target version. Version one is the initial
    ///     document and therefore has no migration entry.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     The identifier is blank, or migrations contain duplicate, invalid,
    ///     or non-contiguous target versions.
    /// </exception>
    public ToolConfigSchema(
        string id,
        IEnumerable<IConfigMigration>? migrations = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        List<IConfigMigration> configuredMigrations = [.. migrations ?? []];

        if (configuredMigrations.Any(migration => migration.ToVersion <= initial_version))
            throw new ArgumentException(
                $"Configuration migrations must target a version greater than {initial_version}.",
                nameof(migrations));

        if (configuredMigrations
            .GroupBy(migration => migration.ToVersion)
            .Any(group => group.Count() > 1))
            throw new ArgumentException(
                "Configuration migrations cannot target the same version more than once.",
                nameof(migrations));

        int currentVersion = configuredMigrations.Count == 0
            ? initial_version
            : configuredMigrations.Max(migration => migration.ToVersion);
        if (Enumerable.Range(initial_version + 1, currentVersion - initial_version)
            .Except(configuredMigrations.Select(migration => migration.ToVersion))
            .Any())
            throw new ArgumentException(
                "Configuration migration target versions must be contiguous.",
                nameof(migrations));

        Id = id;
        Migrations = configuredMigrations
            .OrderBy(migration => migration.ToVersion)
            .ToArray();
        CurrentVersion = currentVersion;
    }

    /// <summary>
    ///     Gets the schema used by standalone compatibility serialization.
    /// </summary>
    public static ToolConfigSchema Default { get; } = new("mapping-tools.project");

    /// <summary>
    ///     Creates the conventional schema identity for a discovered tool.
    /// </summary>
    /// <param name="toolId">The stable tool identifier.</param>
    /// <returns>A schema with the identifier <c>mapping-tools.tool.&lt;toolId&gt;</c>.</returns>
    public static ToolConfigSchema ForTool(string toolId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
        return new ToolConfigSchema($"mapping-tools.tool.{toolId}");
    }

    /// <summary>
    ///     Gets the stable identifier written to the document's <c>$schema</c> field.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Gets the highest version supported by this schema.
    /// </summary>
    public int CurrentVersion { get; }

    /// <summary>
    ///     Gets the migrations in ascending target-version order.
    /// </summary>
    public IReadOnlyList<IConfigMigration> Migrations { get; }
}
