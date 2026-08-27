using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Converters;

/// <summary>Maps persisted Hitsound Studio sample formats to the legacy dialog labels.</summary>
public sealed class HitsoundStudioSampleFormatDisplayConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is HitsoundStudioSampleExportFormat format
            ? format switch
            {
                HitsoundStudioSampleExportFormat.Default => "Default",
                HitsoundStudioSampleExportFormat.WaveIeeeFloat => "IEEE Float (.wav)",
                HitsoundStudioSampleExportFormat.WavePcm => "PCM 16-bit (.wav)",
                HitsoundStudioSampleExportFormat.OggVorbis => "Vorbis (.ogg)",
                HitsoundStudioSampleExportFormat.MidiChords => "Single-chord MIDI (.mid)",
                _ => format.ToString(),
            }
            : string.Empty;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
