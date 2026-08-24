using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio.Models;

/// <summary>Describes the result of a completed Hitsound Studio export.</summary>
public sealed class HitsoundStudioExportResult
{
    /// <summary>Creates an export result.</summary>
    /// <param name="mapPath">The written map path, if a map was requested.</param>
    /// <param name="sampleCount">The number of generated sample files.</param>
    /// <param name="layerCount">The number of input layers.</param>
    /// <param name="eventCount">The number of exported events or MIDI notes.</param>
    /// <param name="schema">The schema produced by this run.</param>
    /// <param name="detailedSummary">The legacy Show Results text for this run.</param>
    public HitsoundStudioExportResult(
        string? mapPath,
        int sampleCount,
        int layerCount,
        int eventCount,
        SampleSchema schema,
        string detailedSummary)
    {
        MapPath = mapPath;
        SampleCount = sampleCount;
        LayerCount = layerCount;
        EventCount = eventCount;
        Schema = schema;
        DetailedSummary = detailedSummary;
    }

    /// <summary>Gets the written map path.</summary>
    public string? MapPath { get; }

    /// <summary>Gets the number of written sample files.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the number of input layers.</summary>
    public int LayerCount { get; }

    /// <summary>Gets the number of generated events or notes.</summary>
    public int EventCount { get; }

    /// <summary>Gets the schema produced by the run.</summary>
    public SampleSchema Schema { get; }

    /// <summary>Gets the legacy detailed completion text.</summary>
    public string DetailedSummary { get; }
}

