using Avalonia.Controls;
using Avalonia.Controls.Utils;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     A ProDataGrid combo box column whose generated editing combo box uses the
///     application's compact combo-box class.
/// </summary>
public sealed class CompactDataGridComboBoxColumn : DataGridComboBoxColumn
{
    /// <summary>
    ///     Creates the editing combo box and applies the compact editor class.
    /// </summary>
    /// <param name="cell">The cell that will host the editor.</param>
    /// <param name="dataItem">The row item being edited.</param>
    /// <param name="editBinding">The edit binding for the cell.</param>
    /// <returns>The generated compact combo-box editor.</returns>
    protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding editBinding)
    {
        Control element = base.GenerateEditingElement(cell, dataItem, out editBinding);
        if (element is ComboBox comboBox && !comboBox.Classes.Contains("compact"))
            comboBox.Classes.Add("compact");

        return element;
    }

    /// <summary>
    ///     Creates the view combo box and applies the compact editor class.
    /// </summary>
    /// <param name="cell">The cell that will host the view.</param>
    /// <param name="dataItem">The row item being edited.</param>
    /// <returns>The generated compact combo-box view.</returns>
    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        Control element = base.GenerateElement(cell, dataItem);
        if (element is ComboBox comboBox && !comboBox.Classes.Contains("compact"))
            comboBox.Classes.Add("compact");

        return element;
    }
}
