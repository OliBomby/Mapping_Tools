using Mapping_Tools.Classes.SystemTools.OverlaySettings;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Components {
    /// <summary>
    /// Interaction logic for HotkeyEditorControl_.xaml
    /// </summary>
    public partial class RelevantObjectSettingsControl {
        public static readonly DependencyProperty RelevantObjectSettingsProperty =
            DependencyProperty.Register(nameof(RelevantObjectSettings), typeof(RelevantObjectSettings), typeof(RelevantObjectSettings),
                new FrameworkPropertyMetadata(default(RelevantObjectSettings), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));



        private static bool IsOneOf(Key key, params Key[] keys) {
            return keys.Contains(key);
        }
    }
}
