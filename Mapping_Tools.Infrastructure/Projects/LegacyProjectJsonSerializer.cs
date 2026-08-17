using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.HitsoundPreviewHelper;
using Mapping_Tools.Application.HitsoundCopier;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Application.SliderMerger;
using Mapping_Tools.Application.SliderPicturator;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.MetadataManager;
using Mapping_Tools.Application.MapsetMerger;
using Mapping_Tools.Application.PropertyTransformer;
using Mapping_Tools.Application.TimingCopier;
using Mapping_Tools.Application.TimingHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Core.Tools.PatternGallery;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
/// Preserves the Newtonsoft type metadata and vector representation emitted by
/// legacy Mapping Tools project files, including domain types moved to Core.
/// </summary>
/// <remarks>
/// Project JSON historically records concrete CLR type names. This serializer
/// is intentionally limited to trusted local project files because enabling
/// that compatibility for untrusted documents would permit construction of
/// types named by the input.
/// </remarks>
public sealed class LegacyProjectJsonSerializer : IProjectSerializer
{
    private static readonly Type MigratedCoreMarker = typeof(Beatmap);

    /// <summary>
    /// Serializes the runtime object graph with legacy simple assembly names,
    /// indented formatting, omitted nulls, and ignored reference loops.
    /// </summary>
    /// <typeparam name="TProject">The concrete project model being persisted.</typeparam>
    /// <param name="project">The complete non-null project snapshot.</param>
    /// <returns>JSON compatible with the former WPF <c>ProjectManager</c>.</returns>
    public string Serialize<TProject>(TProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonConvert.SerializeObject(project, CreateSettings());
    }

    /// <summary>
    /// Restores a trusted project document while redirecting legacy
    /// <c>Mapping Tools</c> domain type names to their current Core assembly.
    /// </summary>
    /// <typeparam name="TProject">The root project model expected by the feature.</typeparam>
    /// <param name="json">A complete legacy or newly written project document.</param>
    /// <returns>The reconstructed non-null project.</returns>
    /// <exception cref="ArgumentException"><paramref name="json"/> is blank.</exception>
    /// <exception cref="InvalidDataException">The JSON root resolves to null.</exception>
    public TProject Deserialize<TProject>(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonConvert.DeserializeObject<TProject>(json, CreateSettings())
            ?? throw new InvalidDataException("The project document contained a JSON null root.");
    }

    private static JsonSerializerSettings CreateSettings()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            SerializationBinder = new LegacyProjectTypeBinder(),
            Converters = [new Vector2Converter()]
        };
    }

    private sealed class LegacyProjectTypeBinder : ISerializationBinder
    {
        private const string LegacyAssemblyName = "Mapping Tools";
        private const string LegacyNamespacePrefix = "Mapping_Tools.";
        private const string CurrentNamespacePrefix = "Mapping_Tools.Core.";
        private const string LegacyRhythmGuideProject = "Mapping_Tools.Viewmodels.RhythmGuideVm";
        private const string LegacyHitsoundPreviewHelperProject =
            "Mapping_Tools.Viewmodels.HitsoundPreviewHelperVm";
        private const string LegacyHitsoundCopierProject =
            "Mapping_Tools.Viewmodels.HitsoundCopierVm";
        private const string LegacyRhythmGuideOptions =
            "Mapping_Tools.Classes.Tools.RhythmGuide+RhythmGuideGeneratorArgs";
        private const string LegacyMapCleanerProject = "Mapping_Tools.Viewmodels.MapCleanerVm";
        private const string LegacyMapCleanerOptions =
            "Mapping_Tools.Classes.Tools.MapCleanerStuff.MapCleanerArgs";
        private const string LegacyMetadataManagerProject =
            "Mapping_Tools.Viewmodels.MetadataManagerVm";
        private const string LegacyPropertyTransformerProject =
            "Mapping_Tools.Viewmodels.PropertyTransformerVm";
        private const string LegacyTimingCopierProject =
            "Mapping_Tools.Viewmodels.TimingCopierVm";
        private const string LegacyTimingHelperProject =
            "Mapping_Tools.Viewmodels.TimingHelperVm";
        private const string LegacySliderCompletionatorProject =
            "Mapping_Tools.Viewmodels.SliderCompletionatorVm";
        private const string LegacySliderMergerProject =
            "Mapping_Tools.Viewmodels.SliderMergerVm";
        private const string LegacySliderPicturatorProject =
            "Mapping_Tools.Viewmodels.SliderPicturatorVm";
        private const string LegacyMapsetMergerProject =
            "Mapping_Tools.Viewmodels.MapsetMergerVm";
        private const string LegacyMapsetMergerItem =
            "Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem";
        private const string LegacyComboColourProject =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ComboColourProject";
        private const string LegacyComboColourPoint =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ColourPoint";
        private const string LegacyPatternGalleryProject =
            "Mapping_Tools.Viewmodels.PatternGalleryVm";
        private const string LegacyPatternGalleryPattern =
            "Mapping_Tools.Classes.Tools.PatternGallery.OsuPattern";
        private const string LegacyPatternGalleryHandler =
            "Mapping_Tools.Classes.Tools.PatternGallery.OsuPatternFileHandler";
        private readonly DefaultSerializationBinder _fallback = new();

        public Type BindToType(string? assemblyName, string typeName)
        {
            if (assemblyName == LegacyAssemblyName)
            {
                if (typeName == LegacyRhythmGuideProject)
                {
                    return typeof(RhythmGuideProject);
                }
                if (typeName == LegacyHitsoundPreviewHelperProject)
                {
                    return typeof(HitsoundPreviewHelperProject);
                }
                if (typeName == LegacyHitsoundCopierProject)
                {
                    return typeof(HitsoundCopierProject);
                }
                if (typeName == LegacyRhythmGuideOptions)
                {
                    return typeof(RhythmGuideOptions);
                }
                if (typeName == LegacyMapCleanerProject)
                {
                    return typeof(MapCleanerProject);
                }
                if (typeName == LegacyMapCleanerOptions)
                {
                    return typeof(MapCleanerOptions);
                }
                if (typeName == LegacyMetadataManagerProject)
                {
                    return typeof(MetadataManagerProject);
                }
                if (typeName == LegacyPropertyTransformerProject)
                {
                    return typeof(PropertyTransformerProject);
                }
                if (typeName == LegacyTimingCopierProject)
                {
                    return typeof(TimingCopierProject);
                }
                if (typeName == LegacyTimingHelperProject)
                {
                    return typeof(TimingHelperProject);
                }
                if (typeName == LegacySliderCompletionatorProject)
                {
                    return typeof(SliderCompletionatorProject);
                }
                if (typeName == LegacySliderMergerProject)
                {
                    return typeof(SliderMergerProject);
                }
                if (typeName == LegacySliderPicturatorProject)
                {
                    return typeof(SliderPicturatorProject);
                }
                if (typeName == LegacyMapsetMergerProject)
                {
                    return typeof(MapsetMergerProject);
                }
                if (typeName == LegacyMapsetMergerItem)
                {
                    return typeof(MapsetMergerProject.MapsetItem);
                }
                if (typeName == LegacyComboColourProject)
                {
                    return typeof(ComboColourProject);
                }
                if (typeName == LegacyComboColourPoint)
                {
                    return typeof(ColourPoint);
                }
                if (typeName == LegacyPatternGalleryProject)
                {
                    return typeof(PatternGalleryProject);
                }
                if (typeName == LegacyPatternGalleryPattern)
                {
                    return typeof(PatternGalleryPattern);
                }
                if (typeName == LegacyPatternGalleryHandler)
                {
                    return typeof(PatternGalleryCollectionMetadata);
                }

                // Accept both the former namespace and documents emitted by
                // an intermediate migration build that already used Core names.
                Type? migratedType = MigratedCoreMarker.Assembly.GetType(typeName)
                    ?? MigratedCoreMarker.Assembly.GetType(ToCurrentTypeName(typeName));
                if (migratedType is not null)
                {
                    return migratedType;
                }
            }

            return _fallback.BindToType(assemblyName, typeName);
        }

        public void BindToName(
            Type serializedType,
            out string? assemblyName,
            out string? typeName)
        {
            if (serializedType == typeof(RhythmGuideProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyRhythmGuideProject;
                return;
            }
            if (serializedType == typeof(HitsoundPreviewHelperProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyHitsoundPreviewHelperProject;
                return;
            }
            if (serializedType == typeof(HitsoundCopierProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyHitsoundCopierProject;
                return;
            }
            if (serializedType == typeof(RhythmGuideOptions))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyRhythmGuideOptions;
                return;
            }
            if (serializedType == typeof(MapCleanerProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyMapCleanerProject;
                return;
            }
            if (serializedType == typeof(MapCleanerOptions))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyMapCleanerOptions;
                return;
            }
            if (serializedType == typeof(MetadataManagerProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyMetadataManagerProject;
                return;
            }
            if (serializedType == typeof(PropertyTransformerProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyPropertyTransformerProject;
                return;
            }
            if (serializedType == typeof(TimingCopierProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyTimingCopierProject;
                return;
            }
            if (serializedType == typeof(TimingHelperProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyTimingHelperProject;
                return;
            }
            if (serializedType == typeof(SliderCompletionatorProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacySliderCompletionatorProject;
                return;
            }
            if (serializedType == typeof(SliderMergerProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacySliderMergerProject;
                return;
            }
            if (serializedType == typeof(SliderPicturatorProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacySliderPicturatorProject;
                return;
            }
            if (serializedType == typeof(MapsetMergerProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyMapsetMergerProject;
                return;
            }
            if (serializedType == typeof(MapsetMergerProject.MapsetItem))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyMapsetMergerItem;
                return;
            }
            if (serializedType == typeof(ComboColourProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyComboColourProject;
                return;
            }
            if (serializedType == typeof(ColourPoint))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyComboColourPoint;
                return;
            }
            if (serializedType == typeof(PatternGalleryProject))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyPatternGalleryProject;
                return;
            }
            if (serializedType == typeof(PatternGalleryPattern))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyPatternGalleryPattern;
                return;
            }
            if (serializedType == typeof(PatternGalleryCollectionMetadata))
            {
                assemblyName = LegacyAssemblyName;
                typeName = LegacyPatternGalleryHandler;
                return;
            }

            if (serializedType.Assembly == MigratedCoreMarker.Assembly)
            {
                assemblyName = LegacyAssemblyName;
                typeName = ToLegacyTypeName(serializedType.FullName);
                return;
            }

            _fallback.BindToName(serializedType, out assemblyName, out typeName);
        }

        private static string ToCurrentTypeName(string typeName) =>
            typeName.StartsWith(LegacyNamespacePrefix, StringComparison.Ordinal)
                ? CurrentNamespacePrefix + typeName[LegacyNamespacePrefix.Length..]
                : typeName;

        private static string? ToLegacyTypeName(string? typeName) =>
            typeName?.StartsWith(CurrentNamespacePrefix, StringComparison.Ordinal) == true
                ? LegacyNamespacePrefix + typeName[CurrentNamespacePrefix.Length..]
                : typeName;
    }

    private sealed class Vector2Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2) || objectType == typeof(Vector2?);
        }

        public override void WriteJson(
            JsonWriter writer,
            object? value,
            JsonSerializer serializer)
        {
            Vector2 vector = (Vector2)(value
                ?? throw new JsonSerializationException("A Vector2 value cannot be null."));

            writer.WriteStartObject();
            writer.WritePropertyName("X");
            serializer.Serialize(writer, vector.X);
            writer.WritePropertyName("Y");
            serializer.Serialize(writer, vector.Y);
            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            double x = default;
            double y = default;
            bool gotX = false;
            bool gotY = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }

                string? propertyName = reader.Value as string;
                if (!reader.Read())
                {
                    break;
                }

                switch (propertyName)
                {
                    case "X":
                        x = serializer.Deserialize<double>(reader);
                        gotX = true;
                        break;
                    case "Y":
                        y = serializer.Deserialize<double>(reader);
                        gotY = true;
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (!gotX || !gotY)
            {
                throw new InvalidDataException(
                    "A legacy Vector2 object must contain numeric X and Y properties.");
            }

            return new Vector2(x, y);
        }
    }
}
