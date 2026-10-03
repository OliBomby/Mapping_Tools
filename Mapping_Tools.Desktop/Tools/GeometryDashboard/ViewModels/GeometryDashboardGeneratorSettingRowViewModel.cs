using Mapping_Tools.Desktop.Services.Undo;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Provides a reflected generator property to Avalonia bindings.</summary>
public sealed class GeometryDashboardGeneratorSettingRowViewModel : LocalizedObservableObject
{
    private readonly PropertyInfo property;
    private readonly GeneratorSettings settings;
    private readonly Func<string>? nameGetter;
    private readonly Func<string>? descriptionGetter;
    private string? pendingValueText;
    private bool hasValueTextError;

    /// <summary>Creates one reflected property row.</summary>
    public GeometryDashboardGeneratorSettingRowViewModel(GeneratorSettings settings, PropertyInfo property)
    {
        this.settings = settings;
        this.property = property;
        nameGetter = ResourceAccessor.FindGetter(
            typeof(DesktopStrings), $"GeometryDashboard_Setting_{property.Name}_Name");
        descriptionGetter = ResourceAccessor.FindGetter(
            typeof(DesktopStrings), $"GeometryDashboard_Setting_{property.Name}_Description");
    }

    /// <summary>Gets the property display name.</summary>
    public string Name => nameGetter?.Invoke()
        ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
        ?? property.Name;

    /// <summary>Gets the explanatory tooltip declared by the Core setting.</summary>
    public string? Description => descriptionGetter?.Invoke()
        ?? property.GetCustomAttribute<DescriptionAttribute>()?.Description;

    /// <summary>Gets the underlying property value.</summary>
    [Undoable]
    public object? Value
    {
        get => property.GetValue(settings);
        set
        {
            property.SetValue(settings, value);
            pendingValueText = null;
            hasValueTextError = false;
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
                object converted = Convert.ChangeType(
                    value,
                    Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType,
                    CultureInfo.InvariantCulture);
                Value = converted;
            }
            catch (FormatException)
            {
                pendingValueText = value;
                hasValueTextError = true;
                OnPropertyChanged(nameof(ValueTextError));
            }
            catch (OverflowException)
            {
                pendingValueText = value;
                hasValueTextError = true;
                OnPropertyChanged(nameof(ValueTextError));
            }
            catch (InvalidCastException)
            {
                pendingValueText = value;
                hasValueTextError = true;
                OnPropertyChanged(nameof(ValueTextError));
            }
        }
    }

    /// <summary>Gets the validation message for an invalid typed setting value.</summary>
    public string? ValueTextError => hasValueTextError ? DesktopStrings.GeometryDashboard_NumberFormatError : null;

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
