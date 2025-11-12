using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Desktop.Models;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class HomeViewModel(IStateStore store) : ViewModelBase, IHasModel<HomeModel> {
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
    
    public HomeViewModel() : this(null!) {
    }

    public HomeModel GetModel() => new(Note, Counter);

    public void SetModel(HomeModel model)
    {
        Note    = model.Note;
        Counter = model.Counter;
    }
}