using Mapping_Tools.Core.Settings.Models;
using Tmds.DBus.Protocol;

namespace Mapping_Tools.Infrastructure.Platform;

internal static class KdeGlobalShortcuts
{
    private const string destination = "org.kde.kglobalaccel";
    private const string path = "/kglobalaccel";
    private const string interface_name = "org.kde.KGlobalAccel";

    internal static async Task PrepareAsync(DBusConnection connection, IEnumerable<string> ids,
        IReadOnlyDictionary<string, HotkeySettings> updates)
    {
        HashSet<string> enabled = new(ids, StringComparer.Ordinal);
        MessageBuffer list;
        {
            using var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(destination, path, interface_name, "allActionsForComponent", "as");
            writer.WriteArray(new[] { "MappingTools", "", "Mapping Tools", "" });
            list = writer.CreateMessage();
        }

        var actions = await connection.CallMethodAsync(list, static (message, _) =>
        {
            var reader = message.GetBodyReader();
            var end = reader.ReadArrayStart(DBusType.Array);
            List<string[]> result = [];
            while (reader.HasNext(end)) result.Add(reader.ReadArrayOfString());
            return result;
        }).ConfigureAwait(false);

        foreach (string[] action in actions)
        {
            string id = action[1];
            if (IsLegacyId(id) || (IsOwnedId(id) && !enabled.Contains(id)))
            {
                MessageBuffer remove;
                {
                    using var writer = connection.GetMessageWriter();
                    writer.WriteMethodCallHeader(destination, path, interface_name, "unregister", "ss");
                    writer.WriteString("MappingTools");
                    writer.WriteString(id);
                    remove = writer.CreateMessage();
                }
                await connection.CallMethodAsync(remove, static (message, _) =>
                    message.GetBodyReader().ReadBool()).ConfigureAwait(false);
            }
            else if (enabled.Contains(id) && updates.TryGetValue(id, out HotkeySettings? hotkey))
            {
                // The portal retains existing assignments and ignores preferred_trigger.
                // Use the same API as KDE's shortcut settings to update the live action.
                MessageBuffer update;
                {
                    using var writer = connection.GetMessageWriter();
                    writer.WriteMethodCallHeader(destination, path, interface_name, "setForeignShortcutKeys", "asa(ai)");
                    writer.WriteArray(action);
                    ArrayStart array = writer.WriteArrayStart(DBusType.Struct);
                    if (hotkey.Key != 0)
                    {
                        writer.WriteStructureStart();
                        writer.WriteArray(new[] { ConvertKey(hotkey) });
                    }
                    writer.WriteArrayEnd(array);
                    update = writer.CreateMessage();
                }
                await connection.CallMethodAsync(update).ConfigureAwait(false);
            }
        }
    }

    private static bool IsOwnedId(string id) => id is "quick-run" or "quick-undo" or "better-save";

    internal static bool IsLegacyId(string id)
    {
        string[] parts = id.Split(':');
        return parts.Length == 3 && IsOwnedId(parts[0]) &&
               int.TryParse(parts[1], out _) && int.TryParse(parts[2], out _);
    }

    internal static int ConvertKey(HotkeySettings hotkey)
    {
        // Qt::Key and Qt::KeyboardModifier values, as used by QKeySequence on D-Bus.
        int key = hotkey.Key switch
        {
            >= 34 and <= 43 => '0' + hotkey.Key - 34,
            >= 44 and <= 69 => 'A' + hotkey.Key - 44,
            >= 74 and <= 83 => '0' + hotkey.Key - 74,
            >= 90 and <= 113 => 0x01000030 + hotkey.Key - 90,
            1 => 0x01020001,
            2 => 0x01000003,
            3 => 0x01000001,
            4 or 6 => 0x01000004,
            5 => 0x0100000b,
            7 => 0x01000008,
            8 => 0x01000024,
            9 => 0x0100112d,
            12 => 0x01001134,
            13 => 0x01000000,
            14 => 0x01001123,
            15 => 0x01001122,
            18 => ' ',
            19 => 0x01000016,
            20 => 0x01000017,
            21 => 0x01000011,
            22 => 0x01000010,
            >= 23 and <= 26 => 0x01000012 + hotkey.Key - 23,
            28 or 30 => 0x01000009,
            31 => 0x01000006,
            32 => 0x01000007,
            33 => 0x01000058,
            73 => 0x01020004,
            84 => '*',
            85 => '+',
            86 => ',',
            87 => '-',
            88 => '.',
            89 => '/',
            114 => 0x01000025,
            115 => 0x01000026,
            122 => 0x01000061,
            123 => 0x01000062,
            124 => 0x01000064,
            125 => 0x01000063,
            126 => 0x01000092,
            127 => 0x01000091,
            128 => 0x01000090,
            129 => 0x01000071,
            130 => 0x01000070,
            131 => 0x01000072,
            132 => 0x01000083,
            133 => 0x01000082,
            134 => 0x01000080,
            135 => 0x01000081,
            >= 136 and <= 139 => 0x010000a0 + hotkey.Key - 136,
            140 => ';',
            141 => '=',
            142 => ',',
            143 => '-',
            144 => '.',
            145 => '/',
            146 => '`',
            147 or 154 => 0xa7,
            148 => 0xa5,
            149 => '[',
            150 => '\\',
            151 => ']',
            152 => '\'',
            _ => throw new NotSupportedException("The key cannot be represented as a KDE shortcut."),
        };
        if (hotkey.Key is >= 74 and <= 89) key |= 0x20000000;
        if ((hotkey.Modifiers & 4) != 0) key |= 0x02000000;
        if ((hotkey.Modifiers & 2) != 0) key |= 0x04000000;
        if ((hotkey.Modifiers & 1) != 0) key |= 0x08000000;
        if ((hotkey.Modifiers & 8) != 0) key |= 0x10000000;
        return key;
    }
}
