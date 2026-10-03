using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Binds a view's text to a resource that updates when the selected language changes.</summary>
public sealed class TrExtension : MarkupExtension
{
    /// <summary>Creates a live translation binding.</summary>
    /// <param name="key">The stable resource key.</param>
    public TrExtension(string key)
    {
        Key = key;
    }

    /// <summary>Gets the key identifying the translated text.</summary>
    public string Key { get; }

    /// <summary>Gets or sets the generated resource class; plugins supply their own class with x:Type.</summary>
    public Type ResourceType { get; set; } = typeof(DesktopStrings);

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var getter = ResourceAccessor.FindGetter(ResourceType, Key)
                     ?? throw new MissingMemberException(ResourceType.FullName, Key);
        return new Binding(nameof(LocalizationState.LanguageVersion))
        {
            Source = LocalizationState.Instance,
            Mode = BindingMode.OneWay,
            Converter = new ResourceConverter(getter),
        };
    }

    private sealed class ResourceConverter(Func<string> getter) : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return getter();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }
}
