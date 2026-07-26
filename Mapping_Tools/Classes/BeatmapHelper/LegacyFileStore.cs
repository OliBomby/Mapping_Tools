using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Classes.BeatmapHelper;

internal static class LegacyFileStore {
    internal static readonly ITextFileStore Default = new FileSystemFileStore();
}
