using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Models;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public partial class SettingsViewModel(IStateStore store) : ViewModelBase, IHasModel<SettingsModel> {

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

    public SettingsViewModel() : this(null!) { }

    public SettingsModel GetModel() => new(Note, Counter);

    public void SetModel(SettingsModel model)
    {
        Note    = model.Note;
        Counter = model.Counter;
    }
}