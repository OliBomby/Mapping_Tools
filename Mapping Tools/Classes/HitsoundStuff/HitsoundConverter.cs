using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundConverter {
        public static List<SamplePackage> MixLayers(List<HitsoundLayer> layers, Sample defaultSample) {
            List<SamplePackage> packages = new List<SamplePackage>();
            foreach (HitsoundLayer hl in layers) {
                foreach (double t in hl.Times) {
                    SamplePackage packageOnTime = packages.Find(o => Math.Abs(o.Time - t) < 5);
                    if (packageOnTime != null) {
                        packageOnTime.Samples.Add(new Sample(hl));
                    } else {
                        packages.Add(new SamplePackage(t, new HashSet<Sample> { new Sample(hl) }));
                    }
                }
            }
            // Packages without a hitnormal sample
            foreach (SamplePackage p in packages.Where(o => !o.Samples.Any(s => s.Hitsound == 0))) {
                p.Samples.Add(defaultSample);
            }
            packages = packages.OrderBy(o => o.Time).ToList();
            return packages;
        }

        public static CompleteHitsounds ConvertPackages(List<SamplePackage> packages) {
            CompleteHitsounds ch = new CompleteHitsounds();

            foreach (SamplePackage p in packages) {
                int sampleSet = p.GetSampleSet();
                int additions = p.GetAdditions();

                bool whistle = p.Samples.Any(o => o.Hitsound == 1);
                bool finish = p.Samples.Any(o => o.Hitsound == 2);
                bool clap = p.Samples.Any(o => o.Hitsound == 3);
                
                // Check if package fits in any CustomIndex or if any CustomIndex can be modified to fit the package
                int index = -1;
                CustomIndex pci = p.GetCustomIndex();

                foreach (CustomIndex ci in ch.CustomIndices) {
                    if (ci.CheckSupport(pci)) {
                        index = ci.Index;
                    } else if (ci.CheckCanSupport(pci)) {
                        ci.MergeWith(pci);
                        index = ci.Index;
                    }
                }
                if (index == -1) {
                    CustomIndex ci = new CustomIndex(ch.CustomIndices.Count + 1);
                    ci.MergeWith(pci);
                    index = ci.Index;
                    ch.CustomIndices.Add(ci);
                }

                ch.Hitsounds.Add(new Hitsound(p.Time, sampleSet, additions, index, whistle, finish, clap));
            }
            return ch;
        }
    }
}
