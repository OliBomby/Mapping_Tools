using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using ProcessMemoryDataFinder;

namespace Mapping_Tools.Infrastructure.Editor;

internal static class CurrentBeatmapMemoryReader
{
    private static readonly Lazy<StructuredOsuMemoryReader> reader = new(() =>
        StructuredOsuMemoryReader.GetInstance(CreateProcessTarget(OperatingSystem.IsWindows())));
    private static readonly Lock readerGate = new();

    internal static bool CanRead
    {
        get
        {
            lock (readerGate)
            {
                return reader.Value.CanRead;
            }
        }
    }

    internal static ProcessTargetOptions CreateProcessTarget(bool isWindows) => isWindows
        ? new ProcessTargetOptions("osu!")
        // Wine retains the executable extension. Skip the kernel32 bitness query;
        // OsuMemoryDataProvider still decodes stable's pointers as 32-bit values.
        : new ProcessTargetOptions("osu!.exe", Target64Bit: null);

    internal static CurrentBeatmap? TryRead()
    {
        lock (readerGate)
        {
            var beatmap = new CurrentBeatmap();
            return reader.Value.TryRead(beatmap) ? beatmap : null;
        }
    }
}
