using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Infrastructure.Projects.Migrations;

internal interface IProjectMigration
{
    int ToVersion { get; }

    void Apply(JObject document);
}

internal static class ProjectMigrationCatalog
{
    private const int initialVersion = 1;

    private static readonly IReadOnlyDictionary<int, IProjectMigration> migrations = CreateMigrations();

    internal static int CurrentVersion => Math.Max(
        initialVersion,
        migrations.Count == 0 ? initialVersion : migrations.Keys.Max());

    internal static IProjectMigration Get(int version)
    {
        return migrations.TryGetValue(version, out IProjectMigration? migration)
            ? migration
            : throw new InvalidDataException(
                $"No project migration exists for target version {version}.");
    }

    private static IReadOnlyDictionary<int, IProjectMigration> CreateMigrations()
    {
        var discovered = typeof(ProjectMigrationCatalog).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IProjectMigration).IsAssignableFrom(type))
            .Select(type => Activator.CreateInstance(type, nonPublic: true) as IProjectMigration
                            ?? throw new InvalidOperationException(
                                $"Could not create project migration '{type.FullName}'."))
            .ToArray();

        if (discovered.Any(migration => migration.ToVersion <= initialVersion))
            throw new InvalidOperationException(
                $"Project migrations must target a version greater than {initialVersion}.");

        return discovered.ToDictionary(
            migration => migration.ToVersion,
            migration => migration);
    }
}
