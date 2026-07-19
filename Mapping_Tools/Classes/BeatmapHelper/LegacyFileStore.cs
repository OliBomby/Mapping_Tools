using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Classes.BeatmapHelper;

internal static class LegacyFileStore {
    internal static readonly ITextFileStore Default = new FileSystemFileStore();
}
