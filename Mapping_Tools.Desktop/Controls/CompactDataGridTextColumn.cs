using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     A ProDataGrid text column whose generated editing text box uses the
///     application's compact text-box class.
/// </summary>
public sealed class CompactDataGridTextColumn : DataGridTextColumn
{
    /// <summary>
    ///     Creates the editing text box and applies the compact editor class.
    /// </summary>
    /// <param name="cell">The cell that will host the editor.</param>
    /// <param name="dataItem">The row item being edited.</param>
    /// <returns>The generated compact text-box editor.</returns>
    protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
    {
        Control element = base.GenerateEditingElementDirect(cell, dataItem);
        if (element is TextBox textBox && !textBox.Classes.Contains("compact"))
            textBox.Classes.Add("compact");

        return element;
    }
}
