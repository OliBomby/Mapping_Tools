using System.Globalization;
using System.Reflection;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Core.Tools.MapCleaner.Models;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RelevantObjectCollectionType = Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectCollection.RelevantObjectCollection;

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
                new FlexibleDoubleArrayConverter(),
                new LegacySampleConverter(),
                new LegacyHitsoundLayerConverter(),
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
        private const string desktop_assembly_name = "Mapping_Tools.Desktop";
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
        private const string intermediate_current_relevant_objects_prefix =
            "Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject.RelevantObjects.";
        private const string intermediate_current_relevant_object_prefix =
            "Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject.";

        private const string legacy_relevant_objects_prefix_without_tools =
            "Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects.";
        private const string legacy_rhythm_guide_project = "Mapping_Tools.Viewmodels.RhythmGuideVm";

        private const string legacy_hitsound_preview_helper_project =
            "Mapping_Tools.Viewmodels.HitsoundPreviewHelperVm";

        private const string legacy_hitsound_preview_helper_project_uppercase =
            "Mapping_Tools.Viewmodels.HitsoundPreviewHelperVM";

        private const string legacy_hitsound_copier_project =
            "Mapping_Tools.Viewmodels.HitsoundCopierVm";

        private const string legacy_hitsound_studio_project =
            "Mapping_Tools.Viewmodels.HitsoundStudioVm";

        private const string legacy_hitsound_studio_project_uppercase =
            "Mapping_Tools.Viewmodels.HitsoundStudioVM";

        private const string legacy_rhythm_guide_options =
            "Mapping_Tools.Classes.Tools.RhythmGuide+RhythmGuideGeneratorArgs";

        private const string legacy_map_cleaner_project = "Mapping_Tools.Viewmodels.MapCleanerVm";

        private const string legacy_map_cleaner_options =
            "Mapping_Tools.Classes.Tools.MapCleanerStuff.MapCleanerArgs";

        private const string legacy_map_cleaner_options_without_folder =
            "Mapping_Tools.Classes.Tools.MapCleanerArgs";

        private const string legacy_metadata_manager_project =
            "Mapping_Tools.Viewmodels.MetadataManagerVm";

        private const string legacy_property_transformer_project =
            "Mapping_Tools.Viewmodels.PropertyTransformerVm";

        private const string legacy_property_transformer_project_uppercase =
            "Mapping_Tools.Viewmodels.PropertyTransformerVM";

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

        private const string legacy_external_file_tempo_signature =
            "Mapping_Tools.Classes.ExternalFileUtil.TempoSignature";

        private const string legacy_mapset_merger_project =
            "Mapping_Tools.Viewmodels.MapsetMergerVm";

        private const string legacy_mapset_merger_item =
            "Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem";

        private const string legacy_snapping_tools_project =
            "Mapping_Tools.Classes.Tools.SnappingTools.Serialization.SnappingToolsEngineOptions";

        private const string legacy_combo_colour_project =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ComboColourEngineOptions";

        private const string legacy_combo_colour_project_without_tools =
            "Mapping_Tools.Classes.ComboColourStudio.ComboColourEngineOptions";

        private const string migrated_combo_colour_project =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ComboColourProject";

        private const string migrated_snapping_tools_project =
            "Mapping_Tools.Classes.Tools.SnappingTools.Serialization.SnappingToolsProject";

        private const string legacy_combo_colour_point =
            "Mapping_Tools.Classes.Tools.ComboColourStudio.ColourPoint";

        private const string legacy_combo_colour_point_without_tools =
            "Mapping_Tools.Classes.ComboColourStudio.ColourPoint";

        private const string legacy_hitsound_sample_export_format =
            "Mapping_Tools.Classes.HitsoundStuff.HitsoundExporter+SampleExportFormat";

        private const string legacy_pattern_gallery_project =
            "Mapping_Tools.Viewmodels.PatternGalleryVm";

        private const string legacy_pattern_gallery_pattern =
            "Mapping_Tools.Classes.Tools.PatternGallery.OsuPattern";

        private const string legacy_pattern_gallery_handler =
            "Mapping_Tools.Classes.Tools.PatternGallery.OsuPatternFileHandler";

        private static readonly string[] current_desktop_model_namespaces =
        [
            "Mapping_Tools.Desktop.Tools.ComboColourStudio.Models",
            "Mapping_Tools.Desktop.Tools.GeometryDashboard.Models",
            "Mapping_Tools.Desktop.Tools.HitsoundCopier.Models",
            "Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Models",
            "Mapping_Tools.Desktop.Tools.HitsoundStudio.Models",
            "Mapping_Tools.Desktop.Tools.MapCleaner.Models",
            "Mapping_Tools.Desktop.Tools.MapsetMerger.Models",
            "Mapping_Tools.Desktop.Tools.MetadataManager.Models",
            "Mapping_Tools.Desktop.Tools.PatternGallery.Models",
            "Mapping_Tools.Desktop.Tools.PropertyTransformer.Models",
            "Mapping_Tools.Desktop.Tools.RhythmGuide.Models",
            "Mapping_Tools.Desktop.Tools.SliderCompletionator.Models",
            "Mapping_Tools.Desktop.Tools.SliderMerger.Models",
            "Mapping_Tools.Desktop.Tools.SliderPicturator.Models",
            "Mapping_Tools.Desktop.Tools.Sliderator.Models",
            "Mapping_Tools.Desktop.Tools.TimingCopier.Models",
            "Mapping_Tools.Desktop.Tools.TimingHelper.Models",
            "Mapping_Tools.Desktop.Tools.TumourGenerator.Models",
        ];

        private readonly DefaultSerializationBinder fallback = new();

        public Type BindToType(string? assemblyName, string typeName)
        {
            if (typeName.StartsWith("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal)
                && typeName.Contains("RelevantObjectPreferences", StringComparison.Ordinal))
                return typeof(Dictionary<string, RelevantObjectPreferences>);

            if (typeName.StartsWith("System.Collections.Generic.Dictionary`2", StringComparison.Ordinal) && typeName.Contains("GeneratorSettings", StringComparison.Ordinal))
                return typeof(Dictionary<Type, GeneratorSettings>);

            if (TryResolveLegacyGenericType(typeName, out Type genericType))
                return genericType;

            if (typeName.EndsWith("[]", StringComparison.Ordinal)
                && (IsLegacyAssembly(assemblyName) || IsCurrentCoreAssembly(assemblyName)))
            {
                Type elementType = BindToType(assemblyName, typeName[..^2]);
                return elementType.MakeArrayType();
            }

            if (IsCurrentApplicationAssembly(assemblyName))
            {
                if (typeName == typeof(ComboColourServiceOptions).FullName)
                    return ResolveDesktopProject("ComboColourProject", typeof(ComboColourServiceOptions));
                if (typeName == typeof(HitsoundCopierServiceOptions).FullName)
                    return ResolveDesktopProject("HitsoundCopierProject", typeof(HitsoundCopierServiceOptions));
                if (typeName == typeof(HitsoundPreviewHelperServiceOptions).FullName)
                    return ResolveDesktopProject("HitsoundPreviewHelperProject", typeof(HitsoundPreviewHelperServiceOptions));
                if (typeName == typeof(MapCleanerServiceOptions).FullName)
                    return ResolveDesktopProject("MapCleanerProject", typeof(MapCleanerServiceOptions));
                if (typeName == typeof(MapsetMergerServiceOptions).FullName)
                    return ResolveDesktopProject("MapsetMergerProject", typeof(MapsetMergerServiceOptions));
                if (typeName == typeof(MetadataManagerServiceOptions).FullName)
                    return ResolveDesktopProject("MetadataManagerProject", typeof(MetadataManagerServiceOptions));
                if (typeName == typeof(HitsoundStudioServiceOptions).FullName)
                    return ResolveDesktopProject("HitsoundStudioProject", typeof(HitsoundStudioServiceOptions));
                if (typeName == typeof(PatternGalleryServiceOptions).FullName)
                    return ResolveDesktopProject("PatternGalleryProject", typeof(PatternGalleryServiceOptions));
                if (typeName == typeof(PropertyTransformerServiceOptions).FullName)
                    return ResolveDesktopProject("PropertyTransformerProject", typeof(PropertyTransformerServiceOptions));
                if (typeName == typeof(RhythmGuideServiceOptions).FullName)
                    return ResolveDesktopProject("RhythmGuideProject", typeof(RhythmGuideServiceOptions));
                if (typeName == typeof(SliderCompletionatorServiceOptions).FullName)
                    return ResolveDesktopProject("SliderCompletionatorProject", typeof(SliderCompletionatorServiceOptions));
                if (typeName == typeof(SliderMergerServiceOptions).FullName)
                    return ResolveDesktopProject("SliderMergerProject", typeof(SliderMergerServiceOptions));
                if (typeName == typeof(SlideratorServiceOptions).FullName)
                    return ResolveDesktopProject("SlideratorProject", typeof(SlideratorServiceOptions));
                if (typeName == typeof(SliderPicturatorServiceOptions).FullName)
                    return ResolveDesktopProject("SliderPicturatorProject", typeof(SliderPicturatorServiceOptions));
                if (typeName == typeof(SnappingToolsServiceOptions).FullName)
                    return ResolveDesktopProject("SnappingToolsProject", typeof(SnappingToolsServiceOptions));
                if (typeName == typeof(TimingCopierServiceOptions).FullName)
                    return ResolveDesktopProject("TimingCopierProject", typeof(TimingCopierServiceOptions));
                if (typeName == typeof(TimingHelperServiceOptions).FullName)
                    return ResolveDesktopProject("TimingHelperProject", typeof(TimingHelperServiceOptions));
                if (typeName == typeof(TumourGeneratorServiceOptions).FullName)
                    return ResolveDesktopProject("TumourGeneratorProject", typeof(TumourGeneratorServiceOptions));

                // Accept the application model names written by the previous
                // migration before the ServiceOptions naming was introduced.
                if (typeName.EndsWith(".ComboColourProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("ComboColourProject", typeof(ComboColourServiceOptions));
                if (typeName.EndsWith(".HitsoundCopierProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("HitsoundCopierProject", typeof(HitsoundCopierServiceOptions));
                if (typeName.EndsWith(".HitsoundPreviewHelperProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("HitsoundPreviewHelperProject", typeof(HitsoundPreviewHelperServiceOptions));
                if (typeName.EndsWith(".HitsoundStudioProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("HitsoundStudioProject", typeof(HitsoundStudioServiceOptions));
                if (typeName.EndsWith(".MapCleanerProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("MapCleanerProject", typeof(MapCleanerServiceOptions));
                if (typeName.EndsWith(".MapsetMergerProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("MapsetMergerProject", typeof(MapsetMergerServiceOptions));
                if (typeName.EndsWith(".MetadataManagerProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("MetadataManagerProject", typeof(MetadataManagerServiceOptions));
                if (typeName.EndsWith(".PatternGalleryProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("PatternGalleryProject", typeof(PatternGalleryServiceOptions));
                if (typeName.EndsWith(".PropertyTransformerProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("PropertyTransformerProject", typeof(PropertyTransformerServiceOptions));
                if (typeName.EndsWith(".RhythmGuideProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("RhythmGuideProject", typeof(RhythmGuideServiceOptions));
                if (typeName.EndsWith(".SliderCompletionatorProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("SliderCompletionatorProject", typeof(SliderCompletionatorServiceOptions));
                if (typeName.EndsWith(".SliderMergerProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("SliderMergerProject", typeof(SliderMergerServiceOptions));
                if (typeName.EndsWith(".SliderPicturatorProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("SliderPicturatorProject", typeof(SliderPicturatorServiceOptions));
                if (typeName.EndsWith(".SlideratorProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("SlideratorProject", typeof(SlideratorServiceOptions));
                if (typeName.EndsWith(".SnappingToolsProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("SnappingToolsProject", typeof(SnappingToolsServiceOptions));
                if (typeName.EndsWith(".TimingCopierProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("TimingCopierProject", typeof(TimingCopierServiceOptions));
                if (typeName.EndsWith(".TimingHelperProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("TimingHelperProject", typeof(TimingHelperServiceOptions));
                if (typeName.EndsWith(".TumourGeneratorProject", StringComparison.Ordinal))
                    return ResolveDesktopProject("TumourGeneratorProject", typeof(TumourGeneratorServiceOptions));
            }

            if (IsLegacyAssembly(assemblyName) || IsCurrentCoreAssembly(assemblyName))
            {
                if (typeName == typeof(ComboColourEngineOptions).FullName)
                    return ResolveDesktopProject("ComboColourProject", typeof(ComboColourServiceOptions));
                if (typeName == typeof(PatternGalleryEngineOptions).FullName)
                    return ResolveDesktopProject("PatternGalleryProject", typeof(PatternGalleryServiceOptions));
                if (typeName == typeof(SnappingToolsEngineOptions).FullName)
                    return ResolveDesktopProject("SnappingToolsProject", typeof(SnappingToolsServiceOptions));

                if (typeName == legacy_rhythm_guide_project) return ResolveDesktopProject("RhythmGuideProject", typeof(RhythmGuideServiceOptions));
                if (typeName == legacy_hitsound_preview_helper_project) return ResolveDesktopProject("HitsoundPreviewHelperProject", typeof(HitsoundPreviewHelperServiceOptions));
                if (typeName == legacy_hitsound_preview_helper_project_uppercase) return ResolveDesktopProject("HitsoundPreviewHelperProject", typeof(HitsoundPreviewHelperServiceOptions));
                if (typeName == legacy_hitsound_copier_project) return ResolveDesktopProject("HitsoundCopierProject", typeof(HitsoundCopierServiceOptions));
                if (typeName == legacy_hitsound_studio_project)
                    return ResolveDesktopProject("HitsoundStudioProject", typeof(HitsoundStudioServiceOptions));
                if (typeName == legacy_hitsound_studio_project_uppercase)
                    return ResolveDesktopProject("HitsoundStudioProject", typeof(HitsoundStudioServiceOptions));
                if (typeName == legacy_rhythm_guide_options
                    || typeName == typeof(RhythmGuideEngineOptions).FullName)
                    return typeof(RhythmGuideServiceOptions.RhythmGuideRunOptions);
                if (typeName == legacy_map_cleaner_project) return ResolveDesktopProject("MapCleanerProject", typeof(MapCleanerServiceOptions));
                if (typeName == legacy_map_cleaner_options
                    || typeName == legacy_map_cleaner_options_without_folder
                    || typeName == typeof(MapCleanerEngineOptions).FullName)
                    return typeof(MapCleanerServiceOptions.MapCleanerCleanupOptions);
                if (typeName == legacy_metadata_manager_project) return ResolveDesktopProject("MetadataManagerProject", typeof(MetadataManagerServiceOptions));
                if (typeName == legacy_property_transformer_project)
                    return ResolveDesktopProject("PropertyTransformerProject", typeof(PropertyTransformerServiceOptions));
                if (typeName == legacy_property_transformer_project_uppercase)
                    return ResolveDesktopProject("PropertyTransformerProject", typeof(PropertyTransformerServiceOptions));
                if (typeName == legacy_timing_copier_project) return ResolveDesktopProject("TimingCopierProject", typeof(TimingCopierServiceOptions));
                if (typeName == legacy_timing_helper_project) return ResolveDesktopProject("TimingHelperProject", typeof(TimingHelperServiceOptions));
                if (typeName == legacy_slider_completionator_project) return ResolveDesktopProject("SliderCompletionatorProject", typeof(SliderCompletionatorServiceOptions));
                if (typeName == legacy_slider_merger_project) return ResolveDesktopProject("SliderMergerProject", typeof(SliderMergerServiceOptions));
                if (typeName == legacy_slider_picturator_project)
                    return ResolveDesktopProject("SliderPicturatorProject", typeof(SliderPicturatorServiceOptions));
                if (typeName == legacy_sliderator_project)
                    return ResolveDesktopProject("SlideratorProject", typeof(SlideratorServiceOptions));
                if (typeName == legacy_tumour_generator_project) return ResolveDesktopProject("TumourGeneratorProject", typeof(TumourGeneratorServiceOptions));
                if (typeName == legacy_tumour_layer) return typeof(TumourLayer);
                if (typeName == legacy_graph_state) return typeof(GraphState);
                if (typeName == legacy_graph_anchor) return typeof(GraphAnchor);
                if (typeName == legacy_external_file_tempo_signature) return typeof(TempoSignature);
                if (typeName == legacy_mapset_merger_project) return ResolveDesktopProject("MapsetMergerProject", typeof(MapsetMergerServiceOptions));
                if (typeName == legacy_mapset_merger_item) return typeof(MapsetMergerServiceOptions.MapsetItem);
                if (typeName == legacy_combo_colour_project
                    || typeName == legacy_combo_colour_project_without_tools
                    || typeName == migrated_combo_colour_project)
                    return ResolveDesktopProject("ComboColourProject", typeof(ComboColourServiceOptions));
                if (typeName == legacy_combo_colour_point) return typeof(ColourPoint);
                if (typeName == legacy_combo_colour_point_without_tools) return typeof(ColourPoint);
                if (typeName == legacy_hitsound_sample_export_format) return typeof(HitsoundStudioSampleExportFormat);
                if (typeName == legacy_pattern_gallery_project) return ResolveDesktopProject("PatternGalleryProject", typeof(PatternGalleryServiceOptions));
                if (typeName == legacy_snapping_tools_project
                    || typeName == migrated_snapping_tools_project)
                    return ResolveDesktopProject("SnappingToolsProject", typeof(SnappingToolsServiceOptions));
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
            if (IsCurrentDesktopProjectModel(serializedType))
            {
                typeName = serializedType.Name switch
                {
                    "ComboColourProject" => legacy_combo_colour_project,
                    "HitsoundCopierProject" => legacy_hitsound_copier_project,
                    "HitsoundPreviewHelperProject" => legacy_hitsound_preview_helper_project,
                    "HitsoundStudioProject" => legacy_hitsound_studio_project,
                    "MapCleanerProject" => legacy_map_cleaner_project,
                    "MapsetMergerProject" => legacy_mapset_merger_project,
                    "MetadataManagerProject" => legacy_metadata_manager_project,
                    "PatternGalleryProject" => legacy_pattern_gallery_project,
                    "PropertyTransformerProject" => legacy_property_transformer_project,
                    "RhythmGuideProject" => legacy_rhythm_guide_project,
                    "SliderCompletionatorProject" => legacy_slider_completionator_project,
                    "SliderMergerProject" => legacy_slider_merger_project,
                    "SliderPicturatorProject" => legacy_slider_picturator_project,
                    "SlideratorProject" => legacy_sliderator_project,
                    "SnappingToolsProject" => legacy_snapping_tools_project,
                    "TimingCopierProject" => legacy_timing_copier_project,
                    "TimingHelperProject" => legacy_timing_helper_project,
                    "TumourGeneratorProject" => legacy_tumour_generator_project,
                    _ => null,
                };
                if (typeName is not null)
                {
                    assemblyName = legacy_assembly_name;
                    return;
                }
            }

            if (serializedType.FullName == "Mapping_Tools.Desktop.Models.HitsoundStudioProject")
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_studio_project;
                return;
            }

            if (serializedType.FullName == "Mapping_Tools.Desktop.Models.PropertyTransformerProject")
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_property_transformer_project;
                return;
            }

            if (serializedType.FullName == "Mapping_Tools.Desktop.Models.SliderPicturatorProject")
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_picturator_project;
                return;
            }

            if (serializedType.FullName == "Mapping_Tools.Desktop.Models.SlideratorProject")
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_sliderator_project;
                return;
            }

            if (serializedType == typeof(RhythmGuideServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_rhythm_guide_project;
                return;
            }

            if (serializedType == typeof(HitsoundPreviewHelperServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_preview_helper_project;
                return;
            }

            if (serializedType == typeof(HitsoundCopierServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_copier_project;
                return;
            }

            if (serializedType == typeof(ComboColourServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_combo_colour_project;
                return;
            }

            if (serializedType == typeof(MapsetMergerServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_mapset_merger_project;
                return;
            }

            if (serializedType == typeof(PatternGalleryServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_pattern_gallery_project;
                return;
            }

            if (serializedType == typeof(SnappingToolsServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_snapping_tools_project;
                return;
            }

            if (serializedType == typeof(HitsoundStudioServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_hitsound_studio_project;
                return;
            }

            if (serializedType == typeof(RhythmGuideEngineOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_rhythm_guide_options;
                return;
            }

            if (serializedType == typeof(RhythmGuideServiceOptions.RhythmGuideRunOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_rhythm_guide_options;
                return;
            }

            if (serializedType == typeof(MapCleanerServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_map_cleaner_project;
                return;
            }

            if (serializedType == typeof(MapCleanerEngineOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_map_cleaner_options;
                return;
            }

            if (serializedType == typeof(MapCleanerServiceOptions.MapCleanerCleanupOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_map_cleaner_options;
                return;
            }

            if (serializedType == typeof(MetadataManagerServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_metadata_manager_project;
                return;
            }

            if (serializedType == typeof(PropertyTransformerServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_property_transformer_project;
                return;
            }

            if (serializedType == typeof(TimingCopierServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_timing_copier_project;
                return;
            }

            if (serializedType == typeof(TimingHelperServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_timing_helper_project;
                return;
            }

            if (serializedType == typeof(SliderCompletionatorServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_completionator_project;
                return;
            }

            if (serializedType == typeof(SliderMergerServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_merger_project;
                return;
            }

            if (serializedType == typeof(SliderPicturatorServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_slider_picturator_project;
                return;
            }

            if (serializedType == typeof(SlideratorServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_sliderator_project;
                return;
            }

            if (serializedType == typeof(TumourGeneratorServiceOptions))
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

            if (serializedType == typeof(MapsetMergerServiceOptions))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_mapset_merger_project;
                return;
            }

            if (serializedType == typeof(MapsetMergerServiceOptions.MapsetItem))
            {
                assemblyName = legacy_assembly_name;
                typeName = legacy_mapset_merger_item;
                return;
            }

            if (serializedType == typeof(ComboColourEngineOptions))
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

            if (serializedType == typeof(PatternGalleryEngineOptions))
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

        private static bool IsCurrentApplicationAssembly(string? assemblyName)
        {
            return string.Equals(
                assemblyName?.Split(',', 2)[0].Trim(),
                "Mapping_Tools.Application",
                StringComparison.Ordinal);
        }

        private static Type ResolveDesktopProject(string typeName, Type fallbackType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (string modelNamespace in current_desktop_model_namespaces)
            {
                Type? currentType = assembly.GetType(
                    $"{modelNamespace}.{typeName}",
                    throwOnError: false);
                if (currentType is not null) return currentType;
            }

            return Type.GetType(
                       $"Mapping_Tools.Desktop.Models.{typeName}, {desktop_assembly_name}",
                       throwOnError: false)
                   ?? fallbackType;
        }

        private static bool IsCurrentDesktopProjectModel(Type serializedType)
        {
            return serializedType.Namespace?.StartsWith(
                       "Mapping_Tools.Desktop.Tools.",
                       StringComparison.Ordinal) == true
                   && serializedType.Namespace.EndsWith(
                       ".Models",
                       StringComparison.Ordinal);
        }

        private bool TryResolveLegacyGenericType(string typeName, out Type resolvedType)
        {
            Type? genericDefinition = typeName.StartsWith(
                                          "System.Collections.Generic.List`1",
                                          StringComparison.Ordinal)
                ? typeof(List<>)
                : typeName.StartsWith(
                    "System.Collections.ObjectModel.ObservableCollection`1",
                    StringComparison.Ordinal)
                    ? typeof(List<>)
                    : typeName.StartsWith(
                        "System.Collections.Generic.Dictionary`2",
                        StringComparison.Ordinal)
                        ? typeof(Dictionary<,>)
                        : null;
            if (genericDefinition is null)
            {
                resolvedType = null!;
                return false;
            }

            int argumentsStart = typeName.IndexOf("[[", StringComparison.Ordinal);
            int argumentsEnd = typeName.LastIndexOf("]]", StringComparison.Ordinal);
            if (argumentsStart < 0 || argumentsEnd <= argumentsStart + 2)
            {
                resolvedType = null!;
                return false;
            }

            string argumentsText = typeName.Substring(
                argumentsStart + 2,
                argumentsEnd - argumentsStart - 2);
            string[] arguments = argumentsText.Split(
                "],[",
                StringSplitOptions.None);
            Type[] argumentTypes = arguments.Select(ResolveLegacyGenericArgument).ToArray();
            resolvedType = genericDefinition.MakeGenericType(argumentTypes);
            return true;

            Type ResolveLegacyGenericArgument(string argument)
            {
                int separator = FindAssemblySeparator(argument);
                string nestedTypeName = separator < 0 ? argument.Trim() : argument[..separator].Trim();
                string? nestedAssemblyName = separator < 0 ? null : argument[(separator + 1)..].Trim();
                return BindToType(nestedAssemblyName, nestedTypeName);
            }
        }

        private static int FindAssemblySeparator(string typeName)
        {
            int bracketDepth = 0;
            for (int index = 0; index < typeName.Length; index++)
            {
                switch (typeName[index])
                {
                    case '[':
                        bracketDepth++;
                        break;
                    case ']':
                        bracketDepth--;
                        break;
                    case ',' when bracketDepth == 0:
                        return index;
                }
            }

            return -1;
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

            if (typeName.StartsWith("Mapping_Tools.Classes.Tools.SnappingTools.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core.Tools.GeometryDashboard."
                             + typeName["Mapping_Tools.Classes.Tools.SnappingTools.".Length..];

            if (typeName.StartsWith("Mapping_Tools.Core.Classes.Tools.SnappingTools.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core.Tools.GeometryDashboard."
                             + typeName["Mapping_Tools.Core.Classes.Tools.SnappingTools.".Length..];

            if (typeName.StartsWith("Mapping_Tools.Core.Tools.SnappingTools.", StringComparison.Ordinal))
                yield return "Mapping_Tools.Core.Tools.GeometryDashboard."
                             + typeName["Mapping_Tools.Core.Tools.SnappingTools.".Length..];

            if (typeName.StartsWith("Mapping_Tools.Classes.SnappingTools.", StringComparison.Ordinal))
            {
                yield return "Mapping_Tools.Core.Tools.SnappingTools."
                             + typeName["Mapping_Tools.Classes.SnappingTools.".Length..];
                yield return "Mapping_Tools.Core.Tools.GeometryDashboard."
                             + typeName["Mapping_Tools.Classes.SnappingTools.".Length..];
            }

            if (typeName.StartsWith(legacy_relevant_objects_prefix, StringComparison.Ordinal))
            {
                yield return intermediate_current_relevant_object_prefix
                             + typeName[legacy_relevant_objects_prefix.Length..];
                yield return current_relevant_object_prefix
                             + typeName[legacy_relevant_objects_prefix.Length..];
            }

            if (typeName.StartsWith(legacy_relevant_objects_prefix_without_tools, StringComparison.Ordinal))
            {
                yield return intermediate_current_relevant_object_prefix
                             + typeName[legacy_relevant_objects_prefix_without_tools.Length..];
                yield return current_relevant_object_prefix
                             + typeName[legacy_relevant_objects_prefix_without_tools.Length..];
            }

            if (typeName.StartsWith(intermediate_relevant_objects_prefix, StringComparison.Ordinal))
            {
                yield return intermediate_current_relevant_object_prefix
                             + typeName[intermediate_relevant_objects_prefix.Length..];
                yield return current_relevant_object_prefix
                             + typeName[intermediate_relevant_objects_prefix.Length..];
            }

            if (typeName.StartsWith(current_relevant_objects_prefix, StringComparison.Ordinal))
            {
                yield return intermediate_current_relevant_object_prefix
                             + typeName[current_relevant_objects_prefix.Length..];
                yield return current_relevant_object_prefix
                             + typeName[current_relevant_objects_prefix.Length..];
            }

            if (typeName.StartsWith(intermediate_current_relevant_objects_prefix, StringComparison.Ordinal))
            {
                yield return intermediate_current_relevant_object_prefix
                             + typeName[intermediate_current_relevant_objects_prefix.Length..];
                yield return current_relevant_object_prefix
                             + typeName[intermediate_current_relevant_objects_prefix.Length..];
            }

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
            if ((typeName?.StartsWith(current_relevant_object_prefix, StringComparison.Ordinal) == true
                 || typeName?.StartsWith(intermediate_current_relevant_object_prefix, StringComparison.Ordinal) == true)
                && (typeName.EndsWith("RelevantPoint", StringComparison.Ordinal)
                    || typeName.EndsWith("RelevantCircle", StringComparison.Ordinal)
                    || typeName.EndsWith("RelevantHitObject", StringComparison.Ordinal)))
            {
                string prefix = typeName.StartsWith(
                    current_relevant_object_prefix,
                    StringComparison.Ordinal)
                    ? current_relevant_object_prefix
                    : intermediate_current_relevant_object_prefix;
                return legacy_relevant_objects_prefix + typeName[prefix.Length..];
            }

            const string current_geometry_dashboard_namespace_prefix =
                "Mapping_Tools.Core.Tools.GeometryDashboard.";
            if (typeName?.StartsWith(current_geometry_dashboard_namespace_prefix, StringComparison.Ordinal) == true)
                return "Mapping_Tools.Classes.Tools.SnappingTools."
                       + typeName[current_geometry_dashboard_namespace_prefix.Length..];

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

    private sealed class FlexibleDoubleArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(double[]);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not double[] values)
                throw new JsonSerializationException("A legacy double array value was invalid.");

            writer.WriteStartArray();
            foreach (double item in values) writer.WriteValue(item);
            writer.WriteEndArray();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Array.Empty<double>();

            if (reader.TokenType is JsonToken.Integer or JsonToken.Float)
                return new[] { Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture) };

            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("A legacy double array must contain a number or an array.");

            var array = JArray.Load(reader);
            return array.Select(token => token.ToObject<double>()).ToArray();
        }
    }

    private sealed class LegacySampleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Sample);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Sample sample)
                throw new JsonSerializationException("A Hitsound Studio sample value was invalid.");

            writer.WriteStartObject();
            writer.WritePropertyName("$type");
            writer.WriteValue("Mapping_Tools.Classes.HitsoundStuff.Sample, Mapping Tools");
            writer.WritePropertyName("SampleArgs");
            serializer.Serialize(writer, sample.SampleArgs);
            writer.WritePropertyName("Priority");
            writer.WriteValue(sample.Priority);
            writer.WritePropertyName("OutsideVolume");
            writer.WriteValue(sample.OutsideVolume);
            writer.WritePropertyName("SampleSet");
            writer.WriteValue(sample.SampleSet);
            writer.WritePropertyName("Hitsound");
            writer.WriteValue(sample.Hitsound);
            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            Sample sample = new()
            {
                Priority = ReadValue(json, "Priority", serializer, 0),
                OutsideVolume = ReadValue(json, "OutsideVolume", serializer, 1d),
                SampleSet = ReadValue(json, "SampleSet", serializer, SampleSet.Normal),
                Hitsound = ReadValue(json, "Hitsound", serializer, Hitsound.Normal),
            };

            sample.SampleArgs = json["SampleArgs"]?.ToObject<SampleGeneratingArgs>(serializer)
                                ?? new SampleGeneratingArgs(
                                    ReadValue(json, "SamplePath", serializer, string.Empty));
            return sample;
        }
    }

    private sealed class LegacyHitsoundLayerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(HitsoundLayer);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not HitsoundLayer layer)
                throw new JsonSerializationException("A Hitsound Studio layer value was invalid.");

            writer.WriteStartObject();
            writer.WritePropertyName("$type");
            writer.WriteValue("Mapping_Tools.Classes.HitsoundStuff.HitsoundLayer, Mapping Tools");
            writer.WritePropertyName("Name");
            writer.WriteValue(layer.Name);
            writer.WritePropertyName("SampleSet");
            writer.WriteValue(layer.SampleSet);
            writer.WritePropertyName("Hitsound");
            writer.WriteValue(layer.Hitsound);
            writer.WritePropertyName("Priority");
            writer.WriteValue(layer.Priority);
            writer.WritePropertyName("ImportArgs");
            serializer.Serialize(writer, layer.ImportArgs);
            writer.WritePropertyName("SampleArgs");
            serializer.Serialize(writer, layer.SampleArgs);
            writer.WritePropertyName("Times");
            serializer.Serialize(writer, layer.Times);
            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            HitsoundLayer layer = new()
            {
                Name = ReadValue(json, "Name", serializer, string.Empty),
                SampleSet = ReadValue(json, "SampleSet", serializer, SampleSet.Normal),
                Hitsound = ReadValue(json, "Hitsound", serializer, Hitsound.Normal),
                Priority = ReadValue(json, "Priority", serializer, int.MaxValue),
                Times = json["Times"]?.ToObject<List<double>>(serializer) ?? [],
            };

            layer.ImportArgs = json["ImportArgs"]?.ToObject<LayerImportArgs>(serializer)
                                ?? new LayerImportArgs
                                {
                                    ImportType = ReadValue(json, "ImportType", serializer, ImportType.None),
                                    Path = ReadValue(json, "Path", serializer, string.Empty),
                                    X = ReadValue(json, "X", serializer, -1d),
                                    Y = ReadValue(json, "Y", serializer, -1d),
                                    SamplePath = ReadValue(json, "SamplePath", serializer, string.Empty),
                                };
            layer.SampleArgs = json["SampleArgs"]?.ToObject<SampleGeneratingArgs>(serializer)
                               ?? new SampleGeneratingArgs();
            if (string.IsNullOrEmpty(layer.SampleArgs.Path))
                layer.SampleArgs.Path = ReadValue(json, "SamplePath", serializer, string.Empty);

            return layer;
        }
    }

    private static T ReadValue<T>(
        JObject json,
        string propertyName,
        JsonSerializer serializer,
        T fallback)
    {
        JToken? token = json[propertyName];
        return token is null || token.Type == JTokenType.Null
            ? fallback
            : token.ToObject<T>(serializer)!;
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
