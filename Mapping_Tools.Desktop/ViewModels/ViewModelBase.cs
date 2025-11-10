using System;
using Mapping_Tools.Application.Persistence;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class ViewModelBase : ReactiveObject, IDisposable {
    protected ViewModelBase() {
        if (this is IPersistable persistable) {
            persistable.LoadAsync();
        }
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        if (this is IPersistable persistable) {
            persistable.SaveAsync();
        }
    }
}