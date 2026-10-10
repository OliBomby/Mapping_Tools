namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

/// <summary>Messages supported by the osu! mapping-tools IPC server.</summary>
public enum MtipcMessageType
{
    Hello = 0,
    ReadBeatmap = 3,
    ReadBookmarks = 4,
    ReadControlPoints = 5,
    ReadObjects = 6,
    EditorTime = 13,
    ReloadEditor = 18,
}
