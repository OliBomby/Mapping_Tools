using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Models;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public partial class SettingsViewModel(IStateStore store) : ViewModelBase, IHasModel<SettingsModel> {

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

    public SettingsViewModel() : this(null!) { }

    public SettingsModel GetModel() => new(Note, Counter);

    public void SetModel(SettingsModel model)
    {
        Note    = model.Note;
        Counter = model.Counter;
    }
}