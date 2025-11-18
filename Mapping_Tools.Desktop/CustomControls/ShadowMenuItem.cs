using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Mapping_Tools.Desktop.CustomControls;


public class ShadowMenuItem : MenuItem
{
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find("PART_Popup") is Popup popup)
        {
            popup.WindowManagerAddShadowHint = true;
        }
    }
}