using System.Text.Json.Nodes;

namespace Mapping_Tools.Infrastructure.Settings.Migrations;

internal interface ISettingsMigration
{
    int ToVersion { get; }

    void Apply(JsonObject document);
}

internal static class SettingsMigrationCatalog
{
    private const int initial_version = 1;

    private static readonly IReadOnlyDictionary<int, ISettingsMigration> migrations = CreateMigrations();

    internal static int CurrentVersion => Math.Max(
        initial_version,
        migrations.Count == 0 ? initial_version : migrations.Keys.Max());

    internal static ISettingsMigration Get(int version)
    {
        return migrations.TryGetValue(version, out var migration)
            ? migration
            : throw new InvalidDataException(
                $"No settings migration exists for target version {version}.");
    }

    private static IReadOnlyDictionary<int, ISettingsMigration> CreateMigrations()
    {
        var discovered = typeof(SettingsMigrationCatalog).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ISettingsMigration).IsAssignableFrom(type))
            .Select(type => Activator.CreateInstance(type, true) as ISettingsMigration
                            ?? throw new InvalidOperationException(
                                $"Could not create settings migration '{type.FullName}'."))
            .ToArray();

        if (discovered.Any(migration => migration.ToVersion <= initial_version))
            throw new InvalidOperationException(
                $"Settings migrations must target a version greater than {initial_version}.");

        return discovered.ToDictionary(
            migration => migration.ToVersion,
            migration => migration);
    }
}
