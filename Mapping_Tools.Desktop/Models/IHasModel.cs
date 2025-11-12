namespace Mapping_Tools.Desktop.Models;

public interface IHasModel<T>
{
    T GetModel();
    void SetModel(T model);
}