using System.Windows;

namespace Mapping_Tools.Components.Domain;

class BooleanToVisibilityConverter : BooleanConverter<Visibility> {
    public BooleanToVisibilityConverter() :
        base(Visibility.Visible, Visibility.Collapsed)
    { }
}