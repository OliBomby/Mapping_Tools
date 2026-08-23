using System.Reflection;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Core.Tools.TimingCopier;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RelevantObjectCollectionType = Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
///     Preserves the Newtonsoft type metadata and vector representation emitted by
///     legacy Mapping Tools project files, including domain types moved to Core.
/// </summary>
/// <remarks>
///     Project JSON historically records concrete CLR type names. This serializer
///     is intentionally limited to trusted local project files because enabling
///     that compatibility for untrusted documents would permit construction of
///     types named by the input.
/// </remarks>
public sealed class LegacyProjectJsonSerializer : IProjectSerializer
{
    private static readonly Type migratedCoreMarker = typeof(Beatmap);

    /// <summary>
    ///     Serializes the runtime object graph with legacy simple assembly names,
    ///     indented formatting, omitted nulls, and ignored reference loops.
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
    ///     Restores a trusted project document while redirecting legacy
    ///     <c>Mapping Tools</c> domain type names to their current Core assembly.
    /// </summary>
    /// <typeparam name="TProject">The root project model expected by the feature.</typeparam>
    /// <param name="json">A complete legacy or newly written project document.</param>
    /// <returns>The reconstructed non-null project.</returns>
    /// <exception cref="ArgumentException"><paramref name="json" /> is blank.</exception>
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
            ContractResolver = new TumourProjectContractResolver(),
            Converters =
            [
                new Vector2Converter(),
                new GeometryGeneratorSettingsDictionaryConverter(),
                new GeometryRelevantObjectCollectionConverter(),
                new TimingCopierResnapModeConverter(),
            ],
        };
    }

    private sealed class TimingCopierResnapModeConverter : JsonConverter
    {
        private const string legacy_preserve_beat_spacing =
            "Number of beats between objects stays the same";
        private const string legacy_resnap = "Just resnap";
        private const string legacy_keep_objects_fixed = "Don't move objects";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimingCopierResnapMode);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not TimingCopierResnapMode mode || !Enum.IsDefined(mode))
                throw new JsonSerializationException("The Timing Copier resnap mode was invalid.");

            writer.WriteValue(mode switch
            {
                TimingCopierResnapMode.PreserveBeatSpacing => legacy_preserve_beat_spacing,
                TimingCopierResnapMode.Resnap => legacy_resnap,
                TimingCopierResnapMode.KeepObjectsFixed => legacy_keep_objects_fixed,
                _ => throw new JsonSerializationException("The Timing Copier resnap mode was invalid."),
            });
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                try
                {
                    return Parse(Convert.ToInt32(reader.Value));
                }
                catch (Exception exception) when (exception is FormatException or OverflowException)
                {
                    throw new JsonSerializationException("The Timing Copier resnap mode was invalid.", exception);
                }
            }

            if (reader.TokenType == JsonToken.String && reader.Value is string text)
            {
                if (text.Equals(legacy_preserve_beat_spacing, StringComparison.Ordinal))
                    return TimingCopierResnapMode.PreserveBeatSpacing;
                if (text.Equals(legacy_resnap, StringComparison.Ordinal))
                    return TimingCopierResnapMode.Resnap;
                if (text.Equals(legacy_keep_objects_fixed, StringComparison.Ordinal))
                    return TimingCopierResnapMode.KeepObjectsFixed;
                if (Enum.TryParse(text, true, out TimingCopierResnapMode mode)) return Parse((int)mode);
            }

            throw new JsonSerializationException("The Timing Copier resnap mode was invalid.");
        }

        private static TimingCopierResnapMode Parse(int value)
        {
            TimingCopierResnapMode mode = (TimingCopierResnapMode)value;
            if (!Enum.IsDefined(mode))
                throw new JsonSerializationException("The Timing Copier resnap mode was invalid.");

            return mode;
        }
    }

    private sealed class TumourProjectContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(GraphState)) property.Converter = new GraphStateConverter();

            return property;
        }
    }

    /// <summary>
    ///     Reads persisted graphs into an empty graph so JSON anchors replace
    ///     constructor defaults instead of being appended to them.
    /// </summary>
    private sealed class GraphStateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(GraphState);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            var graphJson = JObject.Load(reader);
            GraphState graph = new([], 0, 0, 1, 1);
            using var graphReader = graphJson.CreateReader();
            serializer.Populate(graphReader, graph);
            return graph;
        }
    }

    private sealed class LegacyProjectTypeBinder : ISerializationBinder
    {
        private const string legacy_assembly_name = "Mapping Tools";
        private const string legacy_hotkey = "Mapping_Tools.Classes.SystemTools.Hotkey";
        private const string intermediate_core_hotkey = "Mapping_Tools.Core.Classes.SystemTools.Hotkey";
        private const string current_namespace_prefix = "Mapping_Tools.Core.";
        private const string legacy_relevant_objects_prefix =
            "Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects.";
        private const string intermediate_relevant_objects_prefix =
            "Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects.";
        private const string current_relevant_objects_prefix =
            "Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects.";
        private const string current_relevant_object_prefix =
            "Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject.";
        private const string legacy_rhythm_guide_project = "Mapping_Tools.Viewmodels.RhythmGuideVm";

        private const string legacy_hitsound_preview_helper_project =
            "Mapping_Tools.Viewmodels.HitsoundPreviewHelperVm";

        private const string legacy_hitsound_copier_project =
            "Mapping_Tools.Viewmodels.HitsoundCopierVm";

        private const string legacy_hitsound_studio_project =
            "Mapping_Tools.Viewmodels.HitsoundStudioVm";

        private const string legacy_rhythm_guide_options =
            "Mapping_Tools.Classes.Tools.RhythmGuide+RhythmGuideGeneratorArgs";

        private const string legacy_map_cleaner_project = "Mapping_Tools.Viewmodels.MapCleanerVm";

        private const string legacy_map_cleaner_options =
            "Mapping_Tools.Classes.Tools.MapCleanerStuff.MapCleanerArgs";

        private const string legacy_metadata_manager_project =
            "Mapping_Tools.Viewmodels.MetadataManagerVm";

        private const string legacy_property_transformer_project =
            "Mapping_Tools.Viewmodels.PropertyTransformerVm";

        private const string legacy_timing_copier_project =
            "Mapping_Tools.Viewmodels.TimingCopierVm";

        private const string legacy_timing_helper_project =
            "Mapping_Tools.Viewmodels.TimingHelperVm";

        private const string legacy_slider_completionator_project =
            "Mapping_Tools.Viewmodels.SliderCompletionatorVm";

        private const string legacy_slider_merger_project =
            "Mapping_Tools.Viewmodels.SliderMergerVm";

        private const string legacy_slider_picturator_project =
            "Mapping_Tools.Viewmodels.SliderPicturatorVm";

        private const string legacy_sliderator_project =
            "Mapping_Tools.Viewmodels.SlideratorVm";

        private const string legacy_tumour_generator_project =
            "Mapping_Tools.Viewmodels.TumourGeneratorVm";

        private const string legacy_tumour_layer =
            "Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourLayer";

        private const string legacy_graph_state =
            "Mapping_Tools.Components.Graph.GraphState";

        private const string legacy_graph_anchor =
            "Mapping_Tools.Components.Graph.AnchorState";

        private const string legacy_mapset_merger_project =
            "Mapping_Tools.Viewmodels.MapsetMergerVm";

        private const string legacy_mapset_merger_item =
            "Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem";

        private const string legacy_combo_colour_project =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ComboColourProject";

        private const string legacy_combo_colour_point =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ColourPoint";

        private const string legacy_pattern_gallery_project =
            "Mapping_Tools.Viewmodels.PatternGalleryVm";

        private const string legacy_pattern_gallery_pattern =
            "Mapping_Tools.Classes.Tools.PatternGallery.OsuPattern";

        private const string legacy_pattern_gallery_handler =
            "Mapping_Tools.Classes.Tools.PatternGallery.OsuPatternFileHandler";

        private readonly DefaultSerializationBinder fallback = new();

        public Type BindToType(string? assemblyName, string typeName)
        {
            if (typeName.StartsWith("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal)
                && typeName.Contains("RelevantObjectPreferences", StringComparison.Ordinal))
                return typeof(Dictionary<string, RelevantObjectPreferences>);

            if (typeName.StartsWith("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal) && typeName.Contains("GeneratorSettings", StringComparison.Ordinal))
                return typeof(Dictionary<Type, GeneratorSettings>);

            if (IsLegacyAssembly(assemblyName) || IsCurrentCoreAssembly(assemblyName))
            {
                if (typeName == legacy_rhythm_guide_project) return typeof(RhythmGuideProject);
                if (typeName == legacy_hitsound_preview_helper_project) return typeof(HitsoundPreviewHelperProject);
                if (typeName == legacy_hitsound_copier_project) return typeof(HitsoundCopierProject);
                if (typeName == legacy_hitsound_studio_project) return typeof(HitsoundStudioProject);
                if (typeName == legacy_rhythm_guide_options) return typeof(RhythmGuideOptions);
                if (typeName == legacy_map_cleaner_project) return typeof(MapCleanerProject);
                if (typeName == legacy_map_cleaner_options) return typeof(MapCleanerOptions);
                if (typeName == legacy_metadata_manager_project) return typeof(MetadataManagerProject);
                if (typeName == legacy_property_transformer_project) return typeof(PropertyTransformerProject);
                if (typeName == legacy_timing_copier_project) return typeof(TimingCopierProject);
                if (typeName == legacy_timing_helper_project) return typeof(TimingHelperProject);
                if (typeName == legacy_slider_completionator_project) return typeof(SliderCompletionatorProject);
                if (typeName == legacy_slider_merger_project) return typeof(SliderMergerProject);
                if (typeName == legacy_slider_picturator_project) return typeof(SliderPicturatorProject);
                if (typeName == legacy_sliderator_project) return typeof(SlideratorProject);
                if (typeName == legacy_tumour_generator_project) return typeof(TumourGeneratorProject);
                if (typeName == legacy_tumour_layer) return typeof(TumourLayer);
                if (typeName == legacy_graph_state) return typeof(GraphState);
                if (typeName == legacy_graph_anchor) return typeof(GraphAnchor);
                if (typeName == legacy_mapset_merger_project) return typeof(MapsetMergerProject);
                if (typeName == legacy_mapset_merger_item) return typeof(MapsetMergerProject.MapsetItem);
                if (typeName == legacy_combo_colour_project) return typeof(ComboColourProject);
                if (typeName == legacy_combo_colour_point) return typeof(ColourPoint);
                if (typeName == legacy_pattern_gallery_project) return typeof(PatternGalleryProject);
                if (typeName == legacy_pattern_gallery_pattern) return typeof(PatternGalleryPattern);
                if (typeName == legacy_pattern_gallery_handler) return typeof(PatternGalleryCollectionMetadata);
                if (typeName == legacy_hotkey) return typeof(Hotkey);
                if (typeName == intermediate_core_hotkey) return typeof(Hotkey);

                // Accept both the former namespace and documents emitted by
                // an intermediate migration build that already used Core names.
                var migratedType = ResolveMigratedType(typeName);
                if (migratedType is not null) return migratedType;
            }

            return fallback.BindToType(assemblyName, typeName);
        }

        public void BindToName(
            Type serializedType,
            out string? assemblyName,
            out string? typeName)
        {
            if (serializedType == typeof(RhythmGuideProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_rhythm_guide_project;
                return;
            }

            if (serializedType == typeof(HitsoundPreviewHelperProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_preview_helper_project;
                return;
            }

            if (serializedType == typeof(HitsoundCopierProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_copier_project;
                return;
            }

            if (serializedType == typeof(HitsoundStudioProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_studio_project;
                return;
            }

            if (serializedType == typeof(RhythmGuideOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_rhythm_guide_options;
                return;
            }

            if (serializedType == typeof(MapCleanerProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_map_cleaner_project;
                return;
            }

            if (serializedType == typeof(MapCleanerOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_map_cleaner_options;
                return;
            }

            if (serializedType == typeof(MetadataManagerProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_metadata_manager_project;
                return;
            }

            if (serializedType == typeof(PropertyTransformerProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_property_transformer_project;
                return;
            }

            if (serializedType == typeof(TimingCopierProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_timing_copier_project;
                return;
            }

            if (serializedType == typeof(TimingHelperProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_timing_helper_project;
                return;
            }

            if (serializedType == typeof(SliderCompletionatorProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_completionator_project;
                return;
            }

            if (serializedType == typeof(SliderMergerProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_merger_project;
                return;
            }

            if (serializedType == typeof(SliderPicturatorProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_picturator_project;
                return;
            }

            if (serializedType == typeof(SlideratorProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_sliderator_project;
                return;
            }

            if (serializedType == typeof(TumourGeneratorProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_tumour_generator_project;
                return;
            }

            if (serializedType == typeof(TumourLayer))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_tumour_layer;
                return;
            }

            if (serializedType == typeof(GraphState))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_graph_state;
                return;
            }

            if (serializedType == typeof(GraphAnchor))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_graph_anchor;
                return;
            }

            if (serializedType == typeof(MapsetMergerProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_mapset_merger_project;
                return;
            }

            if (serializedType == typeof(MapsetMergerProject.MapsetItem))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_mapset_merger_item;
                return;
            }

            if (serializedType == typeof(ComboColourProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_combo_colour_project;
                return;
            }

            if (serializedType == typeof(ColourPoint))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_combo_colour_point;
                return;
            }

            if (serializedType == typeof(PatternGalleryProject))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_pattern_gallery_project;
                return;
            }

            if (serializedType == typeof(PatternGalleryPattern))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_pattern_gallery_pattern;
                return;
            }

            if (serializedType == typeof(PatternGalleryCollectionMetadata))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_pattern_gallery_handler;
                return;
            }

            if (serializedType == typeof(Hotkey))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hotkey;
                return;
            }

            if (serializedType.Assembly == migratedCoreMarker.Assembly)
            {
                assemblyName = legacy_assembly_name;
                typeName = ToLegacyTypeName(serializedType.FullName);
                return;
            }

            fallback.BindToName(serializedType, out assemblyName, out typeName);
        }

        private static bool IsLegacyAssembly(string? assemblyName)
        {
            return string.Equals(
                assemblyName?.Split(',', 2)[0].Trim(),
                legacy_assembly_name,
                StringComparison.Ordinal);
        }

        private static bool IsCurrentCoreAssembly(string? assemblyName)
        {
            return string.Equals(
                assemblyName?.Split(',', 2)[0].Trim(),
                migratedCoreMarker.Assembly.GetName().Name,
                StringComparison.Ordinal);
        }

        private static Type? ResolveMigratedType(string typeName)
        {
            foreach (string candidate in GetCurrentTypeNameCandidates(typeName))
            {
                Type? migratedType = migratedCoreMarker.Assembly.GetType(candidate);
                if (migratedType is not null) return migratedType;
            }

            return null;
        }

        private static IEnumerable<string> GetCurrentTypeNameCandidates(string typeName)
        {
            yield return typeName;

            if (typeName.StartsWith(legacy_relevant_objects_prefix, StringComparison.Ordinal))
                yield return current_relevant_object_prefix
                             + typeName[legacy_relevant_objects_prefix.Length..];

            if (typeName.StartsWith(intermediate_relevant_objects_prefix, StringComparison.Ordinal))
                yield return current_relevant_object_prefix
                             + typeName[intermediate_relevant_objects_prefix.Length..];

            if (typeName.StartsWith(current_relevant_objects_prefix, StringComparison.Ordinal))
                yield return current_relevant_object_prefix
                             + typeName[current_relevant_objects_prefix.Length..];

            if (typeName.StartsWith("Mapping_Tools.Core.Classes.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core." + typeName["Mapping_Tools.Core.Classes.".Length..];

            if (typeName.StartsWith("Mapping_Tools.Core.Components.Graph.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core.Graph" + typeName["Mapping_Tools.Core.Components.Graph".Length..];

            if (typeName.StartsWith("Mapping_Tools.Classes.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core." + typeName["Mapping_Tools.Classes.".Length..];

            if (typeName.StartsWith("Mapping_Tools.Components.Graph.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core.Graph" + typeName["Mapping_Tools.Components.Graph".Length..];
        }

        internal static string? ToLegacyTypeName(string? typeName)
        {
            if (typeName?.StartsWith(current_relevant_object_prefix, StringComparison.Ordinal) == true
                && (typeName.EndsWith("RelevantPoint", StringComparison.Ordinal)
                    || typeName.EndsWith("RelevantCircle", StringComparison.Ordinal)
                    || typeName.EndsWith("RelevantHitObject", StringComparison.Ordinal)))
                return legacy_relevant_objects_prefix
                       + typeName[current_relevant_object_prefix.Length..];

            return typeName?.StartsWith("Mapping_Tools.Core.Graph.", StringComparison.Ordinal) == true
                ? "Mapping_Tools.Components.Graph" + typeName["Mapping_Tools.Core.Graph".Length..]
                : typeName?.StartsWith(current_namespace_prefix, StringComparison.Ordinal) == true
                    ? "Mapping_Tools.Classes." + typeName[current_namespace_prefix.Length..]
                    : typeName;
        }
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
            var vector = (Vector2)(value
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
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType == typeof(Vector2?)) return null!;

                throw new JsonSerializationException("A non-nullable legacy Vector2 value cannot be null.");
            }

            double x = default;
            double y = default;
            bool gotX = false;
            bool gotY = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;

                if (reader.TokenType != JsonToken.PropertyName) continue;

                string? propertyName = reader.Value as string;
                if (!reader.Read()) break;

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
                throw new InvalidDataException(
                    "A legacy Vector2 object must contain numeric X and Y properties.");

            return new Vector2(x, y);
        }
    }

    private sealed class GeometryGeneratorSettingsDictionaryConverter : JsonConverter
    {
        private const string preferences_dictionary_type =
            "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjectPreferences, Mapping Tools]], System.Private.CoreLib";

        private const string generator_dictionary_type =
            "System.Collections.Generic.Dictionary`2[[System.Type, System.Private.CoreLib],[Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettings, Mapping Tools]], System.Private.CoreLib";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<Type, GeneratorSettings>) || objectType == typeof(Dictionary<string, RelevantObjectPreferences>);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            switch (value)
            {
                case Dictionary<Type, GeneratorSettings> generatorSettings:
                    writer.WriteStartObject();
                    writer.WritePropertyName("$type");
                    writer.WriteValue(generator_dictionary_type);
                    foreach (var (type, settings) in generatorSettings)
                    {
                        writer.WritePropertyName(ToLegacyGeneratorTypeKey(type));
                        serializer.Serialize(writer, settings);
                    }

                    writer.WriteEndObject();
                    return;
                case Dictionary<string, RelevantObjectPreferences> preferences:
                    writer.WriteStartObject();
                    writer.WritePropertyName("$type");
                    writer.WriteValue(preferences_dictionary_type);
                    foreach ((string name, var preference) in preferences)
                    {
                        writer.WritePropertyName(name);
                        serializer.Serialize(writer, preference);
                    }

                    writer.WriteEndObject();
                    return;
                default:
                    throw new JsonSerializationException("Unexpected Geometry Dashboard dictionary type.");
            }
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            var json = JObject.Load(reader);
            if (objectType == typeof(Dictionary<string, RelevantObjectPreferences>))
            {
                Dictionary<string, RelevantObjectPreferences> preferences = new();
                foreach (var property in json.Properties().Where(property => property.Name != "$type"))
                    preferences[property.Name] = property.Value.ToObject<RelevantObjectPreferences>(serializer)
                                                 ?? throw new JsonSerializationException("A Geometry Dashboard preference value was null.");

                return preferences;
            }

            Dictionary<Type, GeneratorSettings> generatorSettings = new();
            LegacyProjectTypeBinder binder = new();
            foreach (var property in json.Properties().Where(property => property.Name != "$type"))
            {
                int separator = property.Name.IndexOf(',');
                string legacyTypeName = separator < 0 ? property.Name : property.Name[..separator];
                var generatorType = binder.BindToType("Mapping Tools", legacyTypeName);
                var settings = property.Value.ToObject<GeneratorSettings>(serializer)
                               ?? throw new JsonSerializationException("A Geometry Dashboard generator setting value was null.");
                generatorSettings[generatorType] = settings;
            }

            return generatorSettings;
        }

        private static string ToLegacyGeneratorTypeKey(Type type)
        {
            string typeName = LegacyProjectTypeBinder.ToLegacyTypeName(type.FullName)
                              ?? throw new JsonSerializationException("A generator type had no full name.");
            return $"{typeName}, Mapping Tools, Version=1.12.28.0, Culture=neutral, PublicKeyToken=null";
        }
    }

    private sealed class GeometryRelevantObjectCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RelevantObjectCollectionType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not RelevantObjectCollectionType collection) throw new JsonSerializationException("Unexpected Geometry Dashboard object collection type.");

            writer.WriteStartObject();
            writer.WritePropertyName("$type");
            writer.WriteValue("Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection, Mapping Tools");
            foreach (var (type, objects) in collection)
            {
                writer.WritePropertyName(ToLegacyObjectTypeKey(type));
                serializer.Serialize(writer, objects);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            var json = JObject.Load(reader);
            RelevantObjectCollectionType collection = new();
            LegacyProjectTypeBinder binder = new();
            foreach (var property in json.Properties().Where(property => property.Name != "$type"))
            {
                int separator = property.Name.IndexOf(',');
                string legacyTypeName = separator < 0 ? property.Name : property.Name[..separator];
                var objectTypeKey = binder.BindToType("Mapping Tools", legacyTypeName);
                var objects = property.Value.ToObject<List<IRelevantObject>>(serializer) ?? [];
                collection[objectTypeKey] = objects;
            }

            return collection;
        }

        private static string ToLegacyObjectTypeKey(Type type)
        {
            string typeName = LegacyProjectTypeBinder.ToLegacyTypeName(type.FullName)
                              ?? throw new JsonSerializationException("A relevant object type had no full name.");
            return $"{typeName}, Mapping Tools, Version=1.12.30.0, Culture=neutral, PublicKeyToken=null";
        }
    }
}
