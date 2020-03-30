using System.Windows.Controls;

namespace Mapping_Tools.Classes.SystemTools {
    /// <summary>
    /// Indicates that this tool has extra menu items that have to be shown in the Project tab
    /// </summary>
    public interface IHaveExtraProjectMenuItems {
        /// <summary>
        /// Gets the menu items that are going to be shown in the Project tab
        /// </summary>
        MenuItem[] GetMenuItems();
    }
}