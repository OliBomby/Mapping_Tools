namespace Mapping_Tools.Classes.SystemTools {
    /// <summary>
    /// Creates a Savable project tool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISavable<T>
    {
        /// <summary>
        /// Grabs and imports all save data specified into the tool.
        /// </summary>
        /// <returns></returns>
        T GetSaveData();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveData"></param>
        void SetSaveData(T saveData);
        /// <summary>
        /// 
        /// </summary>
        string AutoSavePath { get; }
        /// <summary>
        /// 
        /// </summary>
        string DefaultSaveFolder { get; }
    }
}
