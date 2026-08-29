using System.Text.Json.Nodes;

namespace Mapping_Tools.Infrastructure.Settings.Migrations;

internal interface ISettingsMigration
{
    int ToVersion { get; }

    void Apply(JsonObject document);
}

internal static class SettingsMigrationCatalog
{
    private const int initialVersion = 1;

    private static readonly IReadOnlyDictionary<int, ISettingsMigration> migrations = CreateMigrations();

    internal static int CurrentVersion => Math.Max(
        initialVersion,
        migrations.Count == 0 ? initialVersion : migrations.Keys.Max());

    internal static ISettingsMigration Get(int version)
    {
        return migrations.TryGetValue(version, out ISettingsMigration? migration)
            ? migration
            : throw new InvalidDataException(
                $"No settings migration exists for target version {version}.");
    }

    private static IReadOnlyDictionary<int, ISettingsMigration> CreateMigrations()
    {
        var discovered = typeof(SettingsMigrationCatalog).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ISettingsMigration).IsAssignableFrom(type))
            .Select(type => Activator.CreateInstance(type, nonPublic: true) as ISettingsMigration
                            ?? throw new InvalidOperationException(
                                $"Could not create settings migration '{type.FullName}'."))
            .ToArray();

        if (discovered.Any(migration => migration.ToVersion <= initialVersion))
            throw new InvalidOperationException(
                $"Settings migrations must target a version greater than {initialVersion}.");

        return discovered.ToDictionary(
            migration => migration.ToVersion,
            migration => migration);
    }
}
