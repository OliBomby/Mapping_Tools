using System.Threading;
using System.Threading.Tasks;
using Mapping_Tools.Application.Persistence;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class HomeViewModel(IStateStore store) : ViewModelBase, IPersistable {
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

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var dto = await store.LoadAsync<HomeState>("home", ct) ?? new HomeState();
        Note    = dto.Note;
        Counter = dto.Counter;
    }

    public Task SaveAsync(CancellationToken ct = default)
        => store.SaveAsync("home", new HomeState(Note, Counter), ct);
}

public record HomeState(string? Note = null, int Counter = 0);

public class DesignHomeViewModel : HomeViewModel {
    public DesignHomeViewModel() : base(null) {
        Note    = "This is a design-time note 2.";
        Counter = 43;
    }
}

public class FakeStateStore : IStateStore {
    public Task<T?> LoadAsync<T>(string key, CancellationToken ct = default) {
        throw new System.NotImplementedException();
    }

    public Task SaveAsync<T>(string key, T value, CancellationToken ct = default) {
        throw new System.NotImplementedException();
    }
}