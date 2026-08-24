using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Imports and exports neutral MIDI event models.</summary>
public interface IMidiService
{
    /// <summary>Imports note and volume events from a MIDI file.</summary>
    /// <param name="request">The source file.</param>
    /// <param name="cancellationToken">Token checked while reading tracks.</param>
    /// <returns>The imported sequence.</returns>
    Task<MidiSequence> ImportAsync(MidiImportRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exports a neutral MIDI sequence to a file.</summary>
    /// <param name="request">The destination and event data.</param>
    /// <param name="cancellationToken">Token checked while building tracks.</param>
    Task ExportAsync(MidiExportRequest request, CancellationToken cancellationToken = default);
}

