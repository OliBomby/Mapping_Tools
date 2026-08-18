using System;
using System.Linq;
using System.Reflection;
using Mapping_Tools.Core.Classes.SystemTools;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;

/// <summary>Serializable, frontend-neutral settings shared by all geometry generators.</summary>
public class GeneratorSettings : BindableBase, ICloneable
{
    private bool _isActive;
    private bool _isSequential;
    private bool _isDeep;
    private double _relevancyRatio = 0.4;
    private bool _generatesInheritable = true;
    private SelectionPredicateCollection _inputPredicate = new();

    /// <summary>Gets or sets the runtime generator owning these settings.</summary>
    [JsonIgnore]
    public RelevantObjectsGenerator? Generator { get; set; }

    /// <summary>Creates settings with the legacy inactive/default selection behavior.</summary>
    public GeneratorSettings()
    {
    }

    /// <summary>Creates settings associated with a generator.</summary>
    /// <param name="generator">The owning generator.</param>
    public GeneratorSettings(RelevantObjectsGenerator generator) => Generator = generator;

    /// <summary>Gets or sets whether this generator participates in calculation.</summary>
    public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

    /// <summary>Gets or sets whether inputs must be selected in sequence.</summary>
    public bool IsSequential { get => _isSequential; set => Set(ref _isSequential, value); }

    /// <summary>Gets or sets whether all preceding layers may supply inputs.</summary>
    public bool IsDeep { get => _isDeep; set => Set(ref _isDeep, value); }

    /// <summary>Gets or sets the multiplier applied to parent relevance.</summary>
    public double RelevancyRatio { get => _relevancyRatio; set => Set(ref _relevancyRatio, value); }

    /// <summary>Gets or sets whether generated objects can be inherited by later layers.</summary>
    public bool GeneratesInheritable { get => _generatesInheritable; set => Set(ref _generatesInheritable, value); }

    /// <summary>Gets or sets the OR-combined input selection predicates.</summary>
    public SelectionPredicateCollection InputPredicate { get => _inputPredicate; set => Set(ref _inputPredicate, value ?? new SelectionPredicateCollection()); }

    /// <summary>Copies matching serializable properties into another settings instance.</summary>
    /// <param name="other">The target settings instance.</param>
    public void CopyTo(GeneratorSettings other)
    {
        string[] otherPropertyNames = other.GetType().GetProperties().Select(o => o.Name).ToArray();
        foreach (PropertyInfo property in GetType().GetProperties())
        {
            if (!property.CanWrite || !property.CanRead || !otherPropertyNames.Contains(property.Name) ||
                property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
            {
                continue;
            }

            try
            {
                property.SetValue(other, property.GetValue(this));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message + exception.StackTrace);
            }
        }
    }

    /// <inheritdoc/>
    public virtual object Clone() => new GeneratorSettings
    {
        Generator = Generator,
        IsActive = IsActive,
        IsSequential = IsSequential,
        IsDeep = IsDeep,
        RelevancyRatio = RelevancyRatio,
        GeneratesInheritable = GeneratesInheritable,
        InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone()
    };
}
