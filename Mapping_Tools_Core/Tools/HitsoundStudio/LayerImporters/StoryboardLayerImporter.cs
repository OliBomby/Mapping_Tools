using System;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools_Core.Audio.DuplicateDetection;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleGeneration.Decorators;
using Mapping_Tools_Core.BeatmapHelper.Editor;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters {
    public class StoryboardLayerImporter : IStoryboardLayerImporter {
        private readonly IReadEditor<IStoryboard> editor;

        public StoryboardLayerImporter() : this(new StoryboardEditor()) {}

        public StoryboardLayerImporter(IReadEditor<IStoryboard> editor) {
            this.editor = editor;
        }

        public IEnumerable<IHitsoundLayer> Import(IStoryboardLayerImportArgs args) {
            editor.Path = args.Path;
            var storyboard = editor.ReadFile();
            
            // Detect duplicate samples
            string mapDir = editor.GetParentFolder();
            IDuplicateSampleDetector duplicateDetector;
            if (args.DetectDuplicateSamples) {
                duplicateDetector = new MonolithicDuplicateSampleDetector();
            }
            else {
                duplicateDetector = new UselessDuplicateSampleDetector();
            }
            Dictionary<string, string> firstSamples = duplicateDetector.AnalyzeSamples(mapDir, out Exception _, false);

            var hitsoundLayers = new List<HitsoundLayer>();
            foreach (var sbSample in storyboard.StoryboardSoundSamples) {
                var filepath = sbSample.FilePath;
                string samplePath = Path.Combine(mapDir, filepath);
                var filename = Path.GetFileNameWithoutExtension(filepath);

                var volume = args.DiscriminateVolumes ? sbSample.Volume : 1;

                SampleSet sampleSet = Helpers.GetSamplesetFromFilename(filename);
                Hitsound hitsound = Helpers.GetHitsoundFromFilename(filename);

                string fullPathExtLess = Path.Combine(mapDir, filename);

                // Get the first occurence of this sound to not get duplicated
                if (firstSamples.Keys.Contains(fullPathExtLess)) {
                    samplePath = firstSamples[fullPathExtLess];
                }
                
                string extLessFilename = Path.GetFileNameWithoutExtension(samplePath);
                var sourceRef = new StoryboardLayerSourceRef(args.Path, samplePath,
                    volume, args.DiscriminateVolumes, args.DetectDuplicateSamples);

                // Find the hitsoundlayer with this path
                HitsoundLayer layer = hitsoundLayers.Find(o => sourceRef.Equals(o.LayerSourceRef));

                if (layer != null) {
                    // Find hitsound layer with this path and add this time
                    layer.Times.Add(sbSample.StartTime);
                } else {
                    // Add new hitsound layer with this path
                    var newLayer = new HitsoundLayer(new VolumeSampleSoundDecorator(new PathAudioSampleGenerator(samplePath), volume)) {
                        Name = extLessFilename,
                        SampleSet = sampleSet,
                        Hitsound = hitsound,
                        LayerSourceRef = sourceRef
                    };
                    newLayer.Times.Add(sbSample.StartTime);

                    hitsoundLayers.Add(newLayer);
                }
            }

            // Sort layers by name
            return hitsoundLayers.OrderBy(o => o.Name);
        }
    }
}