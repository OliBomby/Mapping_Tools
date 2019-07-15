using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools
{
    public interface ISavable<T>
    {
        T GetSaveData();
        void SetSaveData(T saveData);
        string AutoSavePath { get; }
        string DefaultSaveFolder { get; }
    }
}
