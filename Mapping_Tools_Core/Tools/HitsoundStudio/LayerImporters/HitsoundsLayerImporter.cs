using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools_Core.Audio.DuplicateDetection;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleGeneration.Decorators;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Editor;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters {
    public class HitsoundsLayerImporter : IHitsoundsLayerImporter {
        private readonly Editor<Beatmap> editor;

        public HitsoundsLayerImporter() : this(new BeatmapEditor()) {}

        public HitsoundsLayerImporter(Editor<Beatmap> editor) {
            this.editor = editor;
        }

        public IEnumerable<IHitsoundLayer> Import(IHitsoundsLayerImportArgs args) {
            editor.Path = args.Path;

            Beatmap beatmap = editor.ReadFile();
            Timeline timeline = beatmap.GetTimeline();
            GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;

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

            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();
            foreach (TimelineObject tlo in timeline.TimelineObjects) {
                if (!tlo.HasHitsound) { continue; }

                double volume = args.DiscriminateVolumes ? tlo.FenoSampleVolume / 100 : 1;

                List<string> samples = tlo.GetPlayingFilenames(mode);

                foreach (string filename in samples) {
                    bool isFilename = tlo.UsesFilename;

                    SampleSet sampleSet = isFilename ? tlo.FenoSampleSet : Helpers.GetSamplesetFromFilename(filename);
                    Hitsound hitsound = isFilename ? tlo.GetHitsound() : Helpers.GetHitsoundFromFilename(filename);

                    string samplePath = Path.Combine(mapDir, filename);
                    string fullPathExtLess = Path.Combine(mapDir,
                        Path.GetFileNameWithoutExtension(samplePath));

                    // Get the first occurence of this sound to not get duplicated
                    if (firstSamples.Keys.Contains(fullPathExtLess)) {
                        samplePath = firstSamples[fullPathExtLess];
                    } else {
                        // Sample doesn't exist
                        if (!isFilename) {
                            samplePath = Path.Combine(
                                Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                                $"{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}-1.wav");
                        }
                    }
                    
                    string extLessFilename = Path.GetFileNameWithoutExtension(samplePath);
                    var sourceRef = new HitsoundsLayerSourceRef(args.Path, samplePath,
                        volume, args.DiscriminateVolumes, args.DetectDuplicateSamples);

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => sourceRef.Equals(o.LayerSourceRef));

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        var newLayer = new HitsoundLayer(new VolumeSampleSoundDecorator(new PathAudioSampleGenerator(samplePath), volume)) {
                            Name = extLessFilename,
                            SampleSet = sampleSet,
                            Hitsound = hitsound,
                            LayerSourceRef = sourceRef
                        };
                        newLayer.Times.Add(tlo.Time);

                        hitsoundLayers.Add(newLayer);
                    }
                }
            }

            // Sort layers by name
            return hitsoundLayers.OrderBy(o => o.Name);
        }
    }
}