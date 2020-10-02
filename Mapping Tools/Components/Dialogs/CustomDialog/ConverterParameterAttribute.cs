using System;

namespace Mapping_Tools.Components.Dialogs.CustomDialog {
    public class ConverterParameterAttribute : Attribute {
        public readonly object Parameter;

        public ConverterParameterAttribute(object parameter) {
            Parameter = parameter;
        }
    }
}