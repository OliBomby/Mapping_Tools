namespace Mapping_Tools.Infrastructure.Editor.Memory;

/// <summary>Read-only access to a stable process, independent of its host operating system.</summary>
internal interface IEditorProcessMemory
{
    bool TryRead(nint address, Span<byte> destination);

    IEnumerable<EditorMemoryRegion> EnumerateWritableRegions();
}
