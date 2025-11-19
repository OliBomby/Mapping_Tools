using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Models;
using ReactiveUI;
using System;
using System.Reactive;

namespace Mapping_Tools.Desktop.ViewModels;

public class HomeViewModel : ViewModelBase, IHasModel<HomeModel>
{
    private readonly IStateStore _store;
    private readonly INotificationService _notificationService;

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

    // Command used by the Home view to add a test notification
    public ReactiveCommand<Unit, Unit> AddTestNotification { get; }

    public HomeViewModel(IStateStore store, INotificationService notificationService)
    {
        _store = store;
        _notificationService = notificationService;

        AddTestNotification = ReactiveCommand.Create(() => {
            _notificationService.AddNotification("Test notification", "This is a test notification added from Home view.", NotificationType.Info);
        });
    }

    public HomeModel GetModel() => new(Note, Counter);

    public void SetModel(HomeModel model)
    {
        Note    = model.Note;
        Counter = model.Counter;
    }
}