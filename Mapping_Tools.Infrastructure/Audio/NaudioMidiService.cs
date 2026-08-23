using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using NAudio.Midi;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Adapts NAudio MIDI import/export to neutral timestamped note models.</summary>
public sealed class NaudioMidiService : IMidiService
{
    /// <inheritdoc />
    public Task<MidiSequence> ImportAsync(MidiImportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Import(request.Path, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task ExportAsync(MidiExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Export(request, cancellationToken), cancellationToken);
    }

    private static MidiSequence Import(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path)) throw new FileNotFoundException("The MIDI source does not exist.", path);

        MidiFile file = new(path, false);
        var tempos = file.Events
            .SelectMany(track => track.OfType<TempoEvent>())
            .OrderBy(tempo => tempo.AbsoluteTime)
            .ToList();
        var cumulativeTimes = CalculateCumulativeTimes(tempos, file.DeltaTicksPerQuarterNote);
        Dictionary<int, int> banks = new() { [10] = 128 };
        Dictionary<int, int> patches = [];
        List<MidiNote> notes = [];
        List<MidiVolumeChange> volumes = [];

        foreach (IList<MidiEvent> track in file.Events)
        foreach (var midiEvent in track)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (midiEvent)
            {
                case PatchChangeEvent patchChange:
                    patches[patchChange.Channel] = patchChange.Patch;
                    break;
                case ControlChangeEvent control when control.Controller == MidiController.BankSelect:
                    banks[control.Channel] = control.ControllerValue << 7 | (banks.TryGetValue(control.Channel, out int currentBank) ? currentBank & 0x7f : 0);
                    break;
                case ControlChangeEvent control when control.Controller == MidiController.BankSelectLsb:
                    banks[control.Channel] = control.ControllerValue & 0x7f | (banks.TryGetValue(control.Channel, out int currentBankForLsb) ? currentBankForLsb >> 7 << 7 : 0);
                    break;
                case ControlChangeEvent control when control.Controller == MidiController.MainVolume:
                    volumes.Add(new MidiVolumeChange(
                        CalculateTime(control.AbsoluteTime, tempos, cumulativeTimes, file.DeltaTicksPerQuarterNote),
                        control.Channel,
                        control.ControllerValue));
                    break;
                default:
                    if (!MidiEvent.IsNoteOn(midiEvent) || midiEvent is not NoteOnEvent noteOn) break;

                    double start = CalculateTime(noteOn.AbsoluteTime, tempos, cumulativeTimes, file.DeltaTicksPerQuarterNote);
                    double duration = noteOn.OffEvent is null
                        ? 0
                        : CalculateTime(noteOn.OffEvent.AbsoluteTime, tempos, cumulativeTimes, file.DeltaTicksPerQuarterNote) - start;
                    int noteBank = banks.TryGetValue(noteOn.Channel, out int importedBank) ? importedBank : 0;
                    int notePatch = patches.TryGetValue(noteOn.Channel, out int importedPatch) ? importedPatch : 0;
                    string instrumentName = noteBank == 128
                        ? "Percussion"
                        : notePatch is >= 0 and < 128
                            ? PatchChangeEvent.GetPatchName(notePatch)
                            : "Undefined";
                    int channel = noteOn.Channel;
                    string keyName;
                    if (noteBank == 128)
                    {
                        noteOn.Channel = 10;
                        keyName = noteOn.NoteName;
                    }
                    else if (channel == 10)
                    {
                        noteOn.Channel = 1;
                        keyName = noteOn.NoteName;
                    }
                    else
                    {
                        keyName = noteOn.NoteName;
                    }

                    noteOn.Channel = channel;
                    notes.Add(new MidiNote(
                        start,
                        Math.Max(0, duration),
                        noteBank,
                        notePatch,
                        noteOn.NoteNumber,
                        noteOn.Velocity,
                        channel,
                        instrumentName,
                        keyName));
                    break;
            }
        }

        return new MidiSequence(notes, volumes);
    }

    private static void Export(MidiExportRequest request, CancellationToken cancellationToken)
    {
        const int ticks_per_quarter = 120;
        int microsecondsPerQuarter = (int)(60_000_000 / request.BeatsPerMinute);
        var collection = new MidiEventCollection(0, ticks_per_quarter);
        collection.AddEvent(new TextEvent("Note stream", MetaEventType.TextEvent, 0), 0);
        collection.AddEvent(new TempoEvent(microsecondsPerQuarter, 0), 0);
        List<(int Bank, int Patch)> channels = [];

        foreach (var note in request.Sequence.Notes.OrderBy(note => note.StartMilliseconds))
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bank = Math.Clamp(note.Bank, 0, 16_383);
            int patch = Math.Clamp(note.Patch, 0, 127);
            int key = Math.Clamp(note.Key, 0, 127);
            int velocity = Math.Clamp(note.Velocity, 0, 127);
            int channel = FindChannel(channels, bank, patch);
            if (channel == -1)
            {
                if (channels.Count >= 15) throw new InvalidOperationException("A MIDI file can address at most 15 distinct melodic bank/patch pairs.");

                channels.Add((bank, patch));
                channel = channels.Count >= 10 ? channels.Count + 1 : channels.Count;
                collection.AddEvent(new ControlChangeEvent(ToTicks(note.StartMilliseconds, microsecondsPerQuarter, ticks_per_quarter), channel,
                    MidiController.BankSelect, bank >> 7), 0);
                collection.AddEvent(new ControlChangeEvent(ToTicks(note.StartMilliseconds, microsecondsPerQuarter, ticks_per_quarter), channel,
                    MidiController.BankSelectLsb, bank & 0x7f), 0);
                collection.AddEvent(new PatchChangeEvent(ToTicks(note.StartMilliseconds, microsecondsPerQuarter, ticks_per_quarter), channel, patch), 0);
            }

            long tick = ToTicks(note.StartMilliseconds, microsecondsPerQuarter, ticks_per_quarter);
            int duration = (int)Math.Max(ToTicks(note.DurationMilliseconds, microsecondsPerQuarter, ticks_per_quarter), 0);
            collection.AddEvent(new NoteOnEvent(tick, channel, key, velocity, duration), 0);
            collection.AddEvent(new NoteEvent(tick + duration, channel, MidiCommandCode.NoteOff, key, 0), 0);
        }

        foreach (var volume in request.Sequence.VolumeChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            collection.AddEvent(new ControlChangeEvent(
                ToTicks(volume.TimeMilliseconds, microsecondsPerQuarter, ticks_per_quarter),
                Math.Clamp(volume.Channel, 1, 16),
                MidiController.MainVolume,
                Math.Clamp(volume.Volume, 0, 127)), 1);
        }

        collection.PrepareForExport();
        string? directory = Path.GetDirectoryName(request.Path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        MidiFile.Export(request.Path, collection);
    }

    private static int FindChannel(IReadOnlyList<(int Bank, int Patch)> channels, int bank, int patch)
    {
        if (bank == 128) return 10;

        for (int index = 0; index < channels.Count; index++)
            if (channels[index] == (bank, patch))
                return index >= 9 ? index + 2 : index + 1;

        return -1;
    }

    private static long ToTicks(double milliseconds, int microsecondsPerQuarter, int ticksPerQuarter)
    {
        return (long)(milliseconds * 1000 / microsecondsPerQuarter * ticksPerQuarter);
    }

    private static double CalculateTime(
        long absoluteTime,
        IReadOnlyList<TempoEvent> tempos,
        IReadOnlyList<double> cumulativeTimes,
        int ticksPerQuarter)
    {
        int index = -1;
        for (int tempoIndex = 0; tempoIndex < tempos.Count; tempoIndex++)
        {
            if (tempos[tempoIndex].AbsoluteTime > absoluteTime) break;

            index = tempoIndex;
        }

        if (index < 0) return 500d * absoluteTime / ticksPerQuarter;

        var tempo = tempos[index];
        return cumulativeTimes[index] + tempo.MicrosecondsPerQuarterNote / 1000d * (absoluteTime - tempo.AbsoluteTime) / ticksPerQuarter;
    }

    private static List<double> CalculateCumulativeTimes(IReadOnlyList<TempoEvent> tempos, int ticksPerQuarter)
    {
        List<double> result = [];
        long previousTick = 0;
        double previousMicrosecondsPerQuarter = 500_000;
        double elapsedMilliseconds = 0;
        foreach (var tempo in tempos)
        {
            long tempoTick = Math.Max(previousTick, tempo.AbsoluteTime);
            elapsedMilliseconds += previousMicrosecondsPerQuarter / 1000d * (tempoTick - previousTick) / ticksPerQuarter;
            result.Add(elapsedMilliseconds);
            previousTick = tempoTick;
            previousMicrosecondsPerQuarter = tempo.MicrosecondsPerQuarterNote;
        }

        return result;
    }
}
