using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Models;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class HomeViewModel(IStateStore store) : ViewModelBase, IHasModel<HomeModel> {
    private int _counter;

    public int Counter {
        get => _counter;
        set => this.RaiseAndSetIfChanged(ref _counter, value);
    }

    private string? _note;

    public string? Note {
        get => _note;
        set => this.RaiseAndSetIfChanged(ref _note, value);
    }
    
    public HomeViewModel() : this(null!) { }

    public HomeModel GetModel() => new(Note, Counter);

    public void SetModel(HomeModel model)
    {
        Note    = model.Note;
        Counter = model.Counter;
    }
}