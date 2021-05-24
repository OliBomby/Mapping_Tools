using System;
using System.Windows;

namespace Mapping_Tools.Components.Dialogs.CustomDialog {
    public class TextWrappingAttribute : Attribute {
        public readonly TextWrapping TextWrapping;

        public TextWrappingAttribute(TextWrapping textWrapping) {
            TextWrapping = textWrapping;
        }
    }
}