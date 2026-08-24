using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Desktop.Interactions.GeometryDashboard;

/// <summary>Provides a reflected generator property to Avalonia bindings.</summary>
public sealed class GeometryDashboardGeneratorSettingRowViewModel : ObservableObject
{
    private readonly PropertyInfo property;
    private readonly GeneratorSettings settings;
    private string? pendingValueText;

    /// <summary>Creates one reflected property row.</summary>
    public GeometryDashboardGeneratorSettingRowViewModel(GeneratorSettings settings, PropertyInfo property)
    {
        this.settings = settings;
        this.property = property;
    }

    /// <summary>Gets the property display name.</summary>
    public string Name => property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;

    /// <summary>Gets the explanatory tooltip declared by the Core setting.</summary>
    public string? Description => property.GetCustomAttribute<DescriptionAttribute>()?.Description;

    /// <summary>Gets the underlying property value.</summary>
    public object? Value
    {
        get => property.GetValue(settings);
        set
        {
            property.SetValue(settings, value);
            pendingValueText = null;
            ValueTextError = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValueText));
            OnPropertyChanged(nameof(ValueTextError));
        }
    }

    /// <summary>Gets or parses the reflected value using invariant text.</summary>
    public string ValueText
    {
        get => pendingValueText ?? Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty;
        set
        {
            try
            {
                object? converted = Convert.ChangeType(
                    value,
                    Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType,
                    CultureInfo.InvariantCulture);
                Value = converted;
            }
            catch (FormatException)
            {
                pendingValueText = value;
                ValueTextError = "Number format error.";
                OnPropertyChanged(nameof(ValueTextError));
            }
            catch (OverflowException)
            {
                pendingValueText = value;
                ValueTextError = "Number format error.";
                OnPropertyChanged(nameof(ValueTextError));
            }
            catch (InvalidCastException)
            {
                pendingValueText = value;
                ValueTextError = "Number format error.";
                OnPropertyChanged(nameof(ValueTextError));
            }
        }
    }

    /// <summary>Gets the validation message for an invalid typed setting value.</summary>
    public string? ValueTextError { get; private set; }

    /// <summary>Gets whether the reflected value has a simple text editor.</summary>
    public bool IsTextEditable => property.PropertyType != typeof(bool);

    /// <summary>Gets whether this row represents a Boolean setting.</summary>
    public bool IsBoolean => property.PropertyType == typeof(bool);

    /// <summary>Gets or sets the Boolean setting value.</summary>
    public bool BooleanValue
    {
        get => (bool)(Value ?? false);
        set => Value = value;
    }

    /// <summary>Gets the reflected property type.</summary>
    public Type ValueType => property.PropertyType;
}
