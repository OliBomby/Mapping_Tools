// Editor discovery and stable memory layouts adapted from Karoo13's EditorReader.dll.
// Copyright (c) 2019 Karoo13. See EditorReader.LICENSE.txt for the original license.
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Editor.Memory;

/// <summary>
/// Decodes stable's 32-bit CLR editor graph. Offsets describe the target process,
/// not the runtime hosting Mapping Tools, and are shared by Windows and Wine.
/// </summary>
internal sealed class StableEditorMemoryReader(IEditorProcessMemory memory)
{
    private const int signature_offset = 160;
    private const int scan_chunk_size = 64 * 1024;
    private static readonly byte[] editorSignature = Convert.FromHexString(
        "230000001400000019000000eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee0C000000eeeeeeeeeeeeeeeeeeeeeeeeee00");
    private nint editorAddress;
    private CancellationToken cancellationToken;

    internal LiveBeatmapSnapshot? ReadSnapshot(string songsPath, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(songsPath);
        cancellationToken = token;
        cancellationToken.ThrowIfCancellationRequested();

        // Map switches and edits can invalidate an otherwise readable graph.
        // Retry unreadable or structurally invalid state from the editor root.
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (!IsEditor(editorAddress)) editorAddress = FindEditor();
            if (editorAddress == 0) return null;

            try
            {
                return ReadEditor(songsPath);
            }
            catch (InvalidDataException)
            {
                editorAddress = 0;
                if (attempt == 1) throw;
            }
        }

        throw new InvalidDataException("The osu! editor changed while its live state was being read.");
    }

    private LiveBeatmapSnapshot ReadEditor(string songsPath)
    {
        byte[] editor = Read(editorAddress, 210);
        nint managerAddress = Pointer(editor, 28);
        byte[] manager = Read(managerAddress, 80);
        nint beatmapAddress = Pointer(manager, 48);
        byte[] beatmap = Read(beatmapAddress, 320);
        string folder = ReadString(Pointer(beatmap, 120));
        string filename = ReadString(Pointer(beatmap, 144));
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(filename))
            throw new InvalidDataException("The live beatmap has no folder or filename.");

        byte[] timingPointers = ReadList(Pointer(beatmap, 176), 4);
        var timingPoints = new List<TimingPoint>();
        for (int offset = 0; offset < timingPointers.Length; offset += 4)
            timingPoints.Add(ReadTimingPoint(Pointer(timingPointers, offset)));

        if (timingPoints.Count == 0)
            throw new InvalidDataException("The live beatmap has no timing points.");

        byte[] objectPointers = ReadList(Pointer(manager, 72), 4);
        var hitObjects = new List<HitObject>();
        var selectedObjects = new List<HitObject>();
        for (int offset = 0; offset < objectPointers.Length; offset += 4)
        {
            var hitObject = ReadHitObject(Pointer(objectPointers, offset), out bool selected);
            hitObjects.Add(hitObject);
            if (selected) selectedObjects.Add(hitObject);
        }

        byte[] bookmarkData = ReadList(Pointer(manager, 56), 4);
        var bookmarks = new List<double>();
        for (int offset = 0; offset < bookmarkData.Length; offset += 4)
            bookmarks.Add(BitConverter.ToInt32(bookmarkData, offset));

        byte[] currentEditor = Read(editorAddress, 210);
        byte[] currentManager = Read(managerAddress, 80);
        if (!IsEditor(editorAddress) || Pointer(currentEditor, 28) != managerAddress
            || Pointer(currentManager, 48) != beatmapAddress
            || Pointer(currentManager, 56) != Pointer(manager, 56)
            || Pointer(currentManager, 72) != Pointer(manager, 72)
            || !Read(beatmapAddress, 320).AsSpan().SequenceEqual(beatmap))
            throw new InvalidDataException("The active editor or beatmap changed during the snapshot.");

        // Stable stores two timeline positions; EditorReader reports their mean.
        long editorTime = ((long)BitConverter.ToInt32(currentEditor, 184)
                             + BitConverter.ToInt32(currentEditor, 188)) / 2;
        string path = Path.GetFullPath(Path.Combine(songsPath,
            folder.Replace('\\', Path.DirectorySeparatorChar),
            filename.Replace('\\', Path.DirectorySeparatorChar)));
        return new LiveBeatmapSnapshot(path, bookmarks, timingPoints, hitObjects,
            BitConverter.ToInt32(beatmap, 288), BitConverter.ToDouble(beatmap, 8),
            BitConverter.ToDouble(beatmap, 16), BitConverter.ToSingle(beatmap, 44),
            BitConverter.ToSingle(beatmap, 48), editorTime, selectedObjects);
    }

    private TimingPoint ReadTimingPoint(nint address)
    {
        byte[] data = Read(address, 48);
        int effects = BitConverter.ToInt32(data, 36);
        if (!Read(address, data.Length).AsSpan().SequenceEqual(data))
            throw new InvalidDataException("A timing point changed during the snapshot.");
        return new TimingPoint(BitConverter.ToDouble(data, 12), BitConverter.ToDouble(data, 4),
            BitConverter.ToInt32(data, 28), (SampleSet)BitConverter.ToInt32(data, 24),
            BitConverter.ToInt32(data, 20), BitConverter.ToInt32(data, 32),
            data[40] != 0, (effects & 1) != 0, (effects & 8) != 0);
    }

    private HitObject ReadHitObject(nint address, out bool selected)
    {
        byte[] data = Read(address, 336);
        int type = BitConverter.ToInt32(data, 24);
        if (type == 0) throw new InvalidDataException("The editor returned an uninitialized hit object.");

        float stackedX = BitConverter.ToSingle(data, 56);
        float stackedY = BitConverter.ToSingle(data, 60);
        float stackOffsetX = stackedX - BitConverter.ToSingle(data, 140);
        float stackOffsetY = stackedY - BitConverter.ToSingle(data, 144);
        // Keep stable's single-precision subtraction when undoing stacking.
        var basePosition = new Vector2(stackedX - stackOffsetX, stackedY - stackOffsetY);
        var hitObject = new HitObject
        {
            PixelLength = BitConverter.ToDouble(data, 8),
            Time = BitConverter.ToInt32(data, 16),
            ObjectType = type | ((BitConverter.ToInt32(data, 96) & 7) << 4),
            Hitsounds = BitConverter.ToInt32(data, 28),
            Pos = basePosition,
            EndPos = basePosition,
            Filename = ReadString(Pointer(data, 84)),
            SampleVolume = BitConverter.ToInt32(data, 108),
            SampleSet = (SampleSet)BitConverter.ToInt32(data, 112),
            AdditionSet = (SampleSet)BitConverter.ToInt32(data, 116),
            CustomIndex = BitConverter.ToInt32(data, 120),
        };
        selected = data[133] != 0;

        if (hitObject.IsSlider)
        {
            hitObject.Repeat = BitConverter.ToInt32(data, 32);
            if (hitObject.Repeat < 0) throw new InvalidDataException("The editor returned a negative slider repeat count.");

            // Stored curve anchors include the head and the visual stack offset.
            // The domain representation starts at the unstacked head instead.
            byte[] curveData = ReadList(Pointer(data, 196), 8);
            var points = new List<Vector2> { basePosition };
            for (int offset = 8; offset < curveData.Length; offset += 8)
                points.Add(new Vector2(BitConverter.ToSingle(curveData, offset) - stackOffsetX,
                    BitConverter.ToSingle(curveData, offset + 4) - stackOffsetY));

            hitObject.ControlPoints = PathControlPoint.FromLegacyPositions(points, basePosition,
                (PathType)BitConverter.ToInt32(data, 248));
            if (data[286] == 0)
            {
                hitObject.EdgeHitsounds = ReadIntegers(Pointer(data, 224));
                hitObject.EdgeSampleSets = ReadIntegers(Pointer(data, 228)).Select(value => (SampleSet)value).ToList();
                hitObject.EdgeAdditionSets = ReadIntegers(Pointer(data, 232)).Select(value => (SampleSet)value).ToList();
            }

            if (hitObject.Repeat >= Array.MaxLength)
                throw new InvalidDataException("The slider edges exceed the CLR array representation.");
            int edgeCount = hitObject.Repeat + 1;
            Pad(hitObject.EdgeHitsounds, edgeCount, 0);
            Pad(hitObject.EdgeSampleSets, edgeCount, SampleSet.None);
            Pad(hitObject.EdgeAdditionSets, edgeCount, SampleSet.None);
        }
        else hitObject.Repeat = hitObject.IsSpinner || hitObject.IsHoldNote ? 1 : 0;

        // EndTime derives span duration and must be assigned after Repeat.
        hitObject.EndTime = BitConverter.ToInt32(data, 20);
        if (!Read(address, data.Length).AsSpan().SequenceEqual(data))
            throw new InvalidDataException("A hit object changed during the snapshot.");
        return hitObject;
    }

    private List<int> ReadIntegers(nint listAddress)
    {
        byte[] data = ReadList(listAddress, 4);
        var result = new List<int>();
        for (int offset = 0; offset < data.Length; offset += 4)
            result.Add(BitConverter.ToInt32(data, offset));
        return result;
    }

    private byte[] ReadList(nint address, int elementSize)
    {
        // CLR List<T>: items at +4, size at +12.
        // CLR array: length at +4, contents at +8 (for this 32-bit target).
        byte[] header = Read(address, 16);
        nint arrayAddress = Pointer(header, 4);
        int count = BitConverter.ToInt32(header, 12);
        int capacity = BitConverter.ToInt32(Read(arrayAddress + 4, 4));
        if (count < 0 || capacity < count)
            throw new InvalidDataException("An editor collection has an inconsistent size.");

        // Capture the current contents. Later list mutations do not invalidate
        // this copy; requiring an unchanged version rejects normal live edits.
        return Read(arrayAddress + 8, GetByteLength(count, elementSize));
    }

    private string ReadString(nint address)
    {
        if (address == 0) return string.Empty;
        int length = BitConverter.ToInt32(Read(address + 4, 4));
        byte[] data = Read(address + 8, GetByteLength(length, 2));
        // Preserve the CLR's UTF-16 code units, including unpaired surrogates,
        // rather than applying an encoding decoder's replacement fallback.
        char[] characters = new char[length];
        Buffer.BlockCopy(data, 0, characters, 0, data.Length);
        return new string(characters);
    }

    private byte[] Read(nint address, int length)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (length == 0) return [];
        long numericAddress = address;
        if (numericAddress <= 0 || numericAddress + length > (long)uint.MaxValue + 1)
            throw new InvalidDataException("An editor pointer is outside stable's 32-bit address space.");

        byte[] result = new byte[length];
        if (!memory.TryRead(address, result))
            throw new InvalidDataException($"Unable to read {length} bytes of editor state at 0x{address:X}.");
        return result;
    }

    private static int GetByteLength(int count, int elementSize)
    {
        long length = (long)count * elementSize;
        if (count < 0 || length > Array.MaxLength)
            throw new InvalidDataException("An editor collection exceeds the CLR array representation.");
        return (int)length;
    }

    private static nint Pointer(byte[] data, int offset) => (nint)BitConverter.ToUInt32(data, offset);

    private bool IsEditor(nint address)
    {
        if (address == 0) return false;
        try
        {
            byte[] editor = Read(address, 210);
            return MatchesSignature(editor.AsSpan(signature_offset)) && editor[209] == 0
                   && Pointer(editor, 28) != 0 && Pointer(Read(Pointer(editor, 28), 80), 72) != 0;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private nint FindEditor()
    {
        foreach (var region in memory.EnumerateWritableRegions().OrderBy(region => region.Length))
        {
            // Chunked scanning avoids allocating whole Wine heap mappings. Overlap
            // preserves signatures that straddle a chunk boundary.
            for (long offset = 0; offset < region.Length; offset += scan_chunk_size)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int length = (int)Math.Min(scan_chunk_size + editorSignature.Length - 1, region.Length - offset);
                byte[] buffer = new byte[length];
                nint address = (nint)(region.Address + offset);
                if (!memory.TryRead(address, buffer)) continue;

                for (int index = 0; index <= buffer.Length - editorSignature.Length; index += 4)
                {
                    if (!MatchesSignature(buffer.AsSpan(index))) continue;
                    nint candidate = address + index - signature_offset;
                    if (IsEditor(candidate)) return candidate;
                }
            }
        }

        return 0;
    }

    private static bool MatchesSignature(ReadOnlySpan<byte> data)
    {
        if (data.Length < editorSignature.Length) return false;
        for (int index = 0; index < editorSignature.Length; index++)
        {
            // 0xEE is the wildcard byte in the recovered EditorReader signature.
            if (editorSignature[index] != 0xEE && editorSignature[index] != data[index]) return false;
        }

        return true;
    }

    private static void Pad<T>(List<T> values, int count, T value)
    {
        while (values.Count < count) values.Add(value);
    }
}

