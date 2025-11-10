using System.Threading;
using System.Threading.Tasks;
using Mapping_Tools.Application.Persistence;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public partial class SettingsViewModel(IStateStore store) : ViewModelBase, IPersistable {

    private int counter;

    public int Counter {
        get => counter;
        set => this.RaiseAndSetIfChanged(ref counter, value);
    }

    private string? note;

    public string? Note {
        get => note;
        set => this.RaiseAndSetIfChanged(ref note, value);
    }

    public SettingsViewModel() : this(null!) {
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var dto = await store.LoadAsync<SettingsState>("settings", ct) ?? new SettingsState();
        Note    = dto.Note;
        Counter = dto.Counter;
    }

    public Task SaveAsync(CancellationToken ct = default)
        => store.SaveAsync("settings", new SettingsState(Note, Counter), ct);
}

public record SettingsState(string? Note = null, int Counter = 0);