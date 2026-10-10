using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Reevaluates a one-way text converter whenever either its value or the text language changes.</summary>
public sealed class TranslatedBindingExtension : MarkupExtension
{
    /// <summary>Creates a translated display binding for the data context itself.</summary>
    public TranslatedBindingExtension() : this(string.Empty)
    {
    }

    /// <summary>Creates a binding for a translated enum or other display-only value.</summary>
    /// <param name="path">The source property path, or an empty path for the data context itself.</param>
    public TranslatedBindingExtension(string path)
    {
        Path = path;
    }

    /// <summary>Gets the original data-context path.</summary>
    public string Path { get; }

    /// <summary>Gets or sets the existing converter that produces translated display text.</summary>
    public IValueConverter? Converter { get; set; }

    /// <summary>Gets or sets the parameter forwarded to the display converter.</summary>
    public object? ConverterParameter { get; set; }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new MultiBinding
        {
            Bindings =
            [
                new Binding(Path),
                new Binding(nameof(LocalizationState.LanguageVersion)) { Source = LocalizationState.Instance },
            ],
            Converter = new DisplayConverter(Converter, ConverterParameter),
            Mode = BindingMode.OneWay,
        };
    }

    private sealed class DisplayConverter(IValueConverter? converter, object? parameter) : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? unused, CultureInfo culture)
        {
            return values.Count == 0 ? BindingOperations.DoNothing : converter?.Convert(values[0], targetType, parameter, culture) ?? values[0];
        }
    }
}
