using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundConverter {
        public static List<SamplePackage> ZipLayers(List<HitsoundLayer> layers, Sample defaultSample) {
            List<SamplePackage> packages = new List<SamplePackage>();
            foreach (HitsoundLayer hl in layers) {
                foreach (double t in hl.Times) {
                    SamplePackage packageOnTime = packages.Find(o => Math.Abs(o.Time - t) < 15);
                    if (packageOnTime != null) {
                        packageOnTime.Samples.Add(new Sample(hl));
                    } else {
                        packages.Add(new SamplePackage(t, new HashSet<Sample> { new Sample(hl) }));
                    }
                }
            }
            // Packages without a hitnormal sample
            foreach (SamplePackage p in packages.Where(o => !o.Samples.Any(s => s.Hitsound == 0))) {
                p.Samples.Add(defaultSample.Copy());
            }
            packages = packages.OrderBy(o => o.Time).ToList();
            return packages;
        }

        public static void BalanceVolumes(List<SamplePackage> packages, VolumeBalancingArgs args) {
            foreach (SamplePackage package in packages) {
                double maxVolume = package.Samples.Max(o => o.SampleArgs.Volume);
                if (maxVolume == -0.01)
                    maxVolume = 1;
                double sampleMultiplier = 1 / maxVolume;
                package.Volume = maxVolume;
                foreach (Sample sample in package.Samples) {
                    if (sample.SampleArgs.Volume == -0.01) sample.SampleArgs.Volume = 1;

                    double newVolume = sample.SampleArgs.Volume * sampleMultiplier;
                    if (Math.Abs(newVolume - 1) > args.Roughness && !args.AllwaysFullVolume) {
                        sample.SampleArgs.Volume = args.Roughness * Math.Round(newVolume / args.Roughness);
                    } else {
                        sample.SampleArgs.Volume = 1;
                    }
                }
            }
        }

        public static List<CustomIndex> GetCustomIndices(List<SamplePackage> packages, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null) {
            var indices = packages.Select(o => o.GetCustomIndex()).ToList();
            indices.ForEach(o => o.CleanInvalids(loadedSamples));
            return indices;
        }

        /// <summary>
        /// Makes a new smaller list of CustomIndices which still fits every CustomIndex
        /// </summary>
        /// <param name="customIndices">The CustomIndices that it has to support</param>
        /// <returns></returns>
        public static List<CustomIndex> OptimizeCustomIndices(List<CustomIndex> customIndices) {
            List<CustomIndex> newCustomIndices = new List<CustomIndex>();

            // Try merging together CustomIndices as much as possible
            foreach (CustomIndex ci in customIndices) {
                CustomIndex mergingWith = newCustomIndices.Find(o => o.CanMerge(ci));

                if (mergingWith != null) {
                    mergingWith.MergeWith(ci);
                } else {
                    // There is no CustomIndex to merge with so add a new one
                    newCustomIndices.Add(ci.Copy());
                }
            }

            // Remove any CustomIndices that might be obsolete
            newCustomIndices.RemoveAll(o => !IsUsefull(o, newCustomIndices.Except(new CustomIndex[] { o }).ToList(), customIndices));

            return newCustomIndices;
        }

        private static bool IsUsefull(CustomIndex subject, List<CustomIndex> otherCustomIndices, List<CustomIndex> supportedCustomIndices) {
            // Subject is usefull if it can fit a CustomIndex that no other can fit
            if (supportedCustomIndices.Any(ci => subject.Fits(ci) && !otherCustomIndices.Any(o => o.Fits(ci)))) {
                return true;
            }
            return false;
        }

        public static void GiveCustomIndicesIndices(List<CustomIndex> customIndices) {
            for (int i = 0; i < customIndices.Count; i++) {
                customIndices[i].Index = i + 1;  // osu! CustomIndices start from 1
            }
        }

        /// <summary>
        /// Gets hitsounds of out SamplePackages
        /// </summary>
        /// <param name="packages">The SamplePackages to get hitsounds out of</param>
        /// <param name="customIndices">The CustomIndices that fit all the packages</param>
        /// <returns></returns>
        public static List<HitsoundEvent> GetHitsounds(List<SamplePackage> packages, List<CustomIndex> customIndices) {
            List<HitsoundEvent> hitsounds = new List<HitsoundEvent>(packages.Count);
            List<CustomIndex> packageCustomIndices = GetCustomIndices(packages);

            int index = 0;
            while (index < packageCustomIndices.Count) {
                // Find CustomIndex that fits the most packages from here
                CustomIndex bestCustomIndex = null;
                int bestFits = 0;

                foreach (CustomIndex ci in customIndices) {
                    int fits = NumSupportedPackages(packageCustomIndices, index, ci);

                    if (fits > bestFits) {
                        bestCustomIndex = ci;
                        bestFits = fits;
                    }
                }

                if (bestFits == 0) {
                    throw new Exception("Custom indices can't fit the sample packages.");
                } else {
                    // Add all the fitted packages as hitsounds
                    for (int i = 0; i < bestFits; i++) {
                        hitsounds.Add(packages[index + i].GetHitsound(bestCustomIndex.Index));
                    }
                    index += bestFits;
                }
            }
            return hitsounds;
        }

        private static int NumSupportedPackages(List<CustomIndex> packageCustomIndices, int i, CustomIndex ci) {
            int supported = 0;
            int index = i;
            while (index < packageCustomIndices.Count) {
                if (ci.Fits(packageCustomIndices[index++])) {
                    supported++;
                } else {
                    return supported;
                }
            }
            return supported;
        }

        public static CompleteHitsounds GetCompleteHitsounds(List<SamplePackage> packages, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null) {
            var customIndices = OptimizeCustomIndices(GetCustomIndices(packages, loadedSamples));
            GiveCustomIndicesIndices(customIndices);

            var hitsounds = GetHitsounds(packages, customIndices);

            return new CompleteHitsounds(hitsounds, customIndices);
        }
    }
}
