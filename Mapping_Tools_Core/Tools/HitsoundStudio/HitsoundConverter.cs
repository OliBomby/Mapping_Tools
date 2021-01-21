using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.Audio;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;

namespace Mapping_Tools_Core.Tools.HitsoundStudio {
    public class HitsoundConverter {
        public static List<ISamplePackage> ZipLayers(IEnumerable<IHitsoundLayer> layers, ISample defaultSample, double leniency=15, bool needNormalSample=true) {
            List<ISamplePackage> packages = new List<ISamplePackage>();
            foreach (IHitsoundLayer hl in layers) {
                foreach (double t in hl.Times) {
                    ISamplePackage packageOnTime = packages.Find(o => Math.Abs(o.Time - t) <= leniency);
                    if (packageOnTime != null) {
                        packageOnTime.Samples.Add(new Sample(hl));
                    } else {
                        packages.Add(new SamplePackage(t, new HashSet<ISample> { new Sample(hl) }));
                    }
                }
            }

            if (needNormalSample) {
                // Packages without a hitnormal sample
                foreach (ISamplePackage p in packages.Where(o => o.Samples.All(s => s.Hitsound != 0))) {
                    p.Samples.Add((ISample) defaultSample.Clone());
                }
            }

            packages = packages.OrderBy(o => o.Time).ToList();
            return packages;
        }

        /// <summary>
        /// Balances the volume of <see cref="ISamplePackage"/> such that volume is mostly handled by osu!'s volume controllers rather than
        /// in-sample amplitude changes.
        /// </summary>
        /// <param name="packages"></param>
        /// <param name="roughness">Quantizing level in the new volumes of samples. Can be used to decrease the number of distinct volume levels.</param>
        /// <param name="alwaysFullVolume">Forces to always use maximum amplitude in the samples.</param>
        /// <param name="individualVolume">Allows for multiple distinct volume levels within a single <see cref="ISamplePackage"/>.</param>
        public static void BalanceVolumes(IEnumerable<ISamplePackage> packages, double roughness, bool alwaysFullVolume, bool individualVolume=false) {
            foreach (ISamplePackage package in packages) {
                if (individualVolume) {
                    // Simply mix the volume in the sample to the outside volume
                    foreach (ISample sample in package.Samples) {
                        sample.OutsideVolume = OsuVolumeConverter.AmplitudeToVolume(
                            OsuVolumeConverter.VolumeToAmplitude(sample.OutsideVolume) *
                            OsuVolumeConverter.VolumeToAmplitude(sample.SampleGeneratingArgs.Volume));

                        sample.SampleGeneratingArgs = new SampleGeneratingArgs(sample.SampleGeneratingArgs.ImportArgs, 1);
                    }
                    continue;
                }

                double maxVolume = package.Samples.Max(o => o.SampleGeneratingArgs.Volume);
                if (Math.Abs(maxVolume - -0.01) < Precision.DOUBLE_EPSILON) {
                    maxVolume = 1;
                }

                foreach (ISample sample in package.Samples) {
                    var sampleVolume = sample.SampleGeneratingArgs.Volume;

                    if (Math.Abs(sampleVolume - -0.01) < Precision.DOUBLE_EPSILON) {
                        sampleVolume = 1;
                    }

                    // Pick the new volume such that the samples have a volume as high as possible and the greenline brings the volume down.
                    // With this equation the final amplitude stays the same while the greenline has the volume of the loudest sample at this time.
                    double newVolume = OsuVolumeConverter.AmplitudeToVolume(
                        OsuVolumeConverter.VolumeToAmplitude(sample.OutsideVolume) *
                        OsuVolumeConverter.VolumeToAmplitude(sampleVolume) /
                        OsuVolumeConverter.VolumeToAmplitude(maxVolume));


                    if (Math.Abs(newVolume - 1) > roughness && !alwaysFullVolume) {
                        // If roughness is not 0 it will quantize the new volume in order to reduce the number of different volumes
                        sampleVolume = Math.Abs(roughness) > Precision.DOUBLE_EPSILON ? 
                            roughness * Math.Round(newVolume / roughness) : 
                            newVolume;
                    } else {
                        sampleVolume = 1;
                    }

                    sample.SampleGeneratingArgs = new SampleGeneratingArgs(sample.SampleGeneratingArgs.ImportArgs, sampleVolume);
                }

                if (alwaysFullVolume) {
                    // Assuming the volume of the sample is always maximum, this equation makes sure that 
                    // the loudest sample at this time has the wanted amplitude using the volume change from the greenline.
                    package.SetAllOutsideVolume(OsuVolumeConverter.AmplitudeToVolume(
                        OsuVolumeConverter.VolumeToAmplitude(package.GetMaxOutsideVolume()) *
                        OsuVolumeConverter.VolumeToAmplitude(maxVolume)));
                } else {
                    package.SetAllOutsideVolume(maxVolume);
                }
            }
        }

        public static List<ICustomIndex> GetCustomIndices(List<ISamplePackage> packages, 
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples = null) {

            var indices = packages.Select(o => o.GetCustomIndex()).ToList();
            indices.ForEach(o => o.CleanInvalids(loadedSamples));
            return indices;
        }

        /// <summary>
        /// Makes a new smaller list of CustomIndices which still fits every CustomIndex
        /// </summary>
        /// <param name="customIndices">The CustomIndices that it has to support</param>
        /// <returns></returns>
        public static List<ICustomIndex> OptimizeCustomIndices(List<ICustomIndex> customIndices) {
            List<ICustomIndex> newCustomIndices = new List<ICustomIndex>();

            // Try merging together CustomIndices as much as possible
            foreach (ICustomIndex ci in customIndices) {
                ICustomIndex mergingWith = newCustomIndices.Find(o => o.CanMerge(ci));

                if (mergingWith != null) {
                    mergingWith.MergeWith(ci);
                } else {
                    // There is no CustomIndex to merge with so add a new one
                    newCustomIndices.Add((ICustomIndex) ci.Clone());
                }
            }

            // Remove any CustomIndices that might be obsolete
            newCustomIndices.RemoveAll(o => !IsUseful(o, 
                newCustomIndices.Except(new[] { o }).ToList(), customIndices));

            return newCustomIndices;
        }

        private static bool IsUseful(ICustomIndex subject, IReadOnlyCollection<ICustomIndex> otherCustomIndices, IEnumerable<ICustomIndex> supportedCustomIndices) {
            // Subject is useful if it can fit a CustomIndex that no other can fit
            return supportedCustomIndices.Any(ci => subject.Fits(ci) && !otherCustomIndices.Any(o => o.Fits(ci)));
        }

        public static void GiveCustomIndicesIndices(List<CustomIndex> customIndices, bool keepExistingIndices, int startOffset=1) {
            if (!keepExistingIndices) {
                int i = startOffset;
                foreach (var customIndex in customIndices) {
                    customIndex.Index = i++;
                }
            } else {
                int i = startOffset;
                HashSet<int> usedIndices = new HashSet<int>(customIndices.Where(o => o.Index != -1).Select(o => o.Index));
                foreach (var customIndex in customIndices.Where(o => o.Index == -1)) {
                    while (usedIndices.Contains(i)) {
                        i++;
                    }

                    customIndex.Index = i++;
                    usedIndices.Add(customIndex.Index);
                }
            }
        }

        public static List<IHitsoundEvent> GetHitsounds(List<ISamplePackage> samplePackages,
            ref Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples,
            ref Dictionary<ISampleGeneratingArgs, string> names,
            ref Dictionary<ISampleGeneratingArgs, Vector2> positions,
            bool maniaPositions=false, bool includeRegularHitsounds=true, bool allowNamingGrowth=false) {

            HashSet<ISampleGeneratingArgs> allSampleArgs = new HashSet<ISampleGeneratingArgs>();
            foreach (ISamplePackage sp in samplePackages) {
                allSampleArgs.UnionWith(sp.Samples.Select(o => o.SampleGeneratingArgs));
            }

            if (loadedSamples == null) {
                loadedSamples = Helpers.LoadSampleSoundGenerators(allSampleArgs);
            }

            if (names == null) {
                names = HitsoundExporter.GenerateSampleNames(allSampleArgs, loadedSamples);
            }

            if (positions == null) {
                positions = maniaPositions ? HitsoundExporter.GenerateManiaHitsoundPositions(allSampleArgs) :
                    HitsoundExporter.GenerateHitsoundPositions(allSampleArgs);
            }

            var hitsounds = new List<IHitsoundEvent>();
            foreach (var p in samplePackages) {
                foreach (var s in p.Samples) {
                    string filename;

                    if (names.ContainsKey(s.SampleGeneratingArgs)) {
                        filename = names[s.SampleGeneratingArgs];
                    } else {
                        // Validate the sample because we expect only valid samples to be present in the sample schema
                        if (SampleImporter.ValidateSampleArgs(s.SampleGeneratingArgs, loadedSamples)) {
                            if (allowNamingGrowth) {
                                HitsoundExporter.AddNewSampleName(names, s.SampleGeneratingArgs, loadedSamples);
                                filename = names[s.SampleGeneratingArgs];
                            } else {
                                throw new Exception($"Given sample schema doesn't support sample ({s.SampleGeneratingArgs}) and growth is disabled.");
                            }
                        } else {
                            filename = string.Empty;
                        }
                    }

                    if (includeRegularHitsounds) {
                        hitsounds.Add(new HitsoundEvent(p.Time,
                            positions[s.SampleGeneratingArgs], s.OutsideVolume, filename, s.SampleSet, s.SampleSet,
                            0, s.Whistle, s.Finish, s.Clap));
                    } else {
                        hitsounds.Add(new HitsoundEvent(p.Time,
                            positions[s.SampleArgs], s.OutsideVolume, filename, SampleSet.Auto, SampleSet.Auto,
                            0, false, false, false));
                    }
                }
            }

            return hitsounds;
        }

        /// <summary>
        /// Generates 1-to-1 <see cref="HitsoundEvent"/> of out <see cref="SamplePackage"/> using provided custom indices.
        /// </summary>
        /// <param name="samplePackages">The SamplePackages to get hitsounds out of</param>
        /// <param name="customIndices">The CustomIndices that fit all the packages</param>
        /// <param name="loadedSamples">Loaded samples for the validation of samples files from the sample packages.</param>
        /// <param name="validateSampleFile">Whether to validate sample files from the sample packages.</param>
        /// <param name="comparer">Comparer for <see cref="SampleGeneratingArgs"/></param>
        /// <returns></returns>
        public static List<HitsoundEvent> GetHitsounds(List<SamplePackage> samplePackages, 
            List<CustomIndex> customIndices,
            Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null,
            bool validateSampleFile = true, SampleGeneratingArgsComparer comparer = null) {
            List<HitsoundEvent> hitsounds = new List<HitsoundEvent>(samplePackages.Count);
            List<CustomIndex> packageCustomIndices = GetCustomIndices(samplePackages, loadedSamples, validateSampleFile, comparer);

            int index = 0;
            while (index < packageCustomIndices.Count) {
                // Find CustomIndex that fits the most packages from here
                CustomIndex bestCustomIndex = null;
                int bestFits = 0;

                foreach (CustomIndex ci in customIndices) {
                    int fits = NumSupportedPackages(packageCustomIndices, index, ci);

                    if (fits <= bestFits) continue;
                    bestCustomIndex = ci;
                    bestFits = fits;
                }


                if (bestFits == 0) {
                    throw new Exception("Custom indices can't fit the sample packages.\n" +
                                        "Maybe you are using an incompatible previous sample schema and growth is disabled.");
                }

                // Add all the fitted packages as hitsounds
                for (int i = 0; i < bestFits; i++)
                {
                    if (bestCustomIndex != null)
                        hitsounds.Add(samplePackages[index + i].GetHitsound(bestCustomIndex.Index));
                }
                index += bestFits;
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

        public static CompleteHitsounds GetCompleteHitsounds(List<SamplePackage> packages, 
            Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null,
            List<CustomIndex> customIndices = null, bool allowGrowth=false, int firstCustomIndex=1,
            bool validateSampleFile = true, SampleGeneratingArgsComparer comparer = null) {

            if (customIndices == null) {
                customIndices = OptimizeCustomIndices(GetCustomIndices(packages, loadedSamples, validateSampleFile, comparer));
                GiveCustomIndicesIndices(customIndices, false, firstCustomIndex);
            } else if (allowGrowth) {
                customIndices = OptimizeCustomIndices(customIndices.Concat(GetCustomIndices(packages, loadedSamples, validateSampleFile, comparer)).ToList());
                GiveCustomIndicesIndices(customIndices, true, firstCustomIndex);
            }

            var hitsounds = GetHitsounds(packages, customIndices, loadedSamples, validateSampleFile, comparer);

            return new CompleteHitsounds(hitsounds, customIndices);
        }
    }
}
