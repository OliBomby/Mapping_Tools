using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters {
    public class StackLayerImporter : IStackLayerImporter {
        private readonly Editor<Beatmap> editor;

        public StackLayerImporter() : this(new BeatmapEditor()) {}

        public StackLayerImporter(Editor<Beatmap> editor) {
            this.editor = editor;
        }

        public IEnumerable<IHitsoundLayer> Import(IStackLayerImportArgs args) {
            // TODO: Add concrete layer source reference
            HitsoundLayer layer = new HitsoundLayer(TimesFromStack(args.Path, args.X, args.Y, args.Leniency), null);
            yield return layer;
        }

        private IEnumerable<double> TimesFromStack(string path, double x, double y, double leniency) {
            editor.Path = path;
            editor.ReadFile();

            bool xIgnore = double.IsNaN(x);
            bool yIgnore = double.IsNaN(y);

            foreach (var ho in editor.Instance.HitObjects.Where(ho => (Math.Abs(ho.Pos.X - x) < leniency || xIgnore) && 
                                                                      (Math.Abs(ho.Pos.Y - y) < leniency || yIgnore))) {
                yield return ho.Time;
            }
        }
    }
}