namespace Mapping_Tools.Classes.SystemTools {
    public interface ISavable<T>
    {
        T GetSaveData();
        void SetSaveData(T saveData);
        string AutoSavePath { get; }
        string DefaultSaveFolder { get; }
    }
}
