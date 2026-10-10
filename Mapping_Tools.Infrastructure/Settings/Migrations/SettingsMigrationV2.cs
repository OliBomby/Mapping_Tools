using System.Text.Json.Nodes;

namespace Mapping_Tools.Infrastructure.Settings.Migrations;

internal sealed class SettingsMigrationV2 : ISettingsMigration
{
    public int ToVersion => 2;

    public void Apply(JsonObject document)
    {
        ApplyLegacy(document);
    }

    internal static void ApplyLegacy(JsonObject document)
    {
        if (!document.ContainsKey("CurrentBeatmapFetching"))
            document["CurrentBeatmapFetching"] = "MemoryRead";

        if (TryReadBoolean(document, "UseEditorReader", out bool useEditorReader))
        {
            document["BeatmapLiveStateReading"] = useEditorReader
                ? "EditorReader"
                : "Disabled";
        }
        else if (!document.ContainsKey("BeatmapLiveStateReading"))
        {
            document["BeatmapLiveStateReading"] = "EditorReader";
        }

        if (TryReadBoolean(document, "AutoReload", out bool autoReload))
        {
            document["EditorReload"] = autoReload
                ? "SimulatedKeypress"
                : "Disabled";
        }
        else if (!document.ContainsKey("EditorReload"))
        {
            document["EditorReload"] = "SimulatedKeypress";
        }

        document.Remove("UseEditorReader");
        document.Remove("AutoReload");
    }

    private static bool TryReadBoolean(JsonObject document, string name, out bool value)
    {
        if (document[name] is JsonValue node
            && node.TryGetValue(out bool parsed))
        {
            value = parsed;
            return true;
        }

        value = default;
        return false;
    }
}
