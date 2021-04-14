using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Editor;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters {
    public class StackLayerImporter : IStackLayerImporter {
        private readonly Editor<Beatmap> editor;

        public StackLayerImporter() : this(new BeatmapEditor()) {}

        public StackLayerImporter(Editor<Beatmap> editor) {
            this.editor = editor;
        }

        public IEnumerable<IHitsoundLayer> Import(IStackLayerImportArgs args) {
            HitsoundLayer layer = new HitsoundLayer(TimesFromStack(args.Path, args.X, args.Y, args.Leniency)) {
                LayerSourceRef = new StackLayerSourceRef(args.Path, args.X, args.Y, args.Leniency)
            };
            yield return layer;
        }

        private IEnumerable<double> TimesFromStack(string path, double x, double y, double leniency) {
            editor.Path = path;
            var beatmap = editor.ReadFile();

            bool xIgnore = double.IsNaN(x);
            bool yIgnore = double.IsNaN(y);

            foreach (var ho in beatmap.HitObjects.Where(ho => 
                (Math.Abs(ho.Pos.X - x) <= leniency + Precision.DOUBLE_EPSILON || xIgnore) && 
                (Math.Abs(ho.Pos.Y - y) <= leniency + Precision.DOUBLE_EPSILON || yIgnore))) {
                yield return ho.StartTime;
            }
        }
    }
}