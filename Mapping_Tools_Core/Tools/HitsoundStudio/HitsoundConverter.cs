using Mapping_Tools_Core.Audio;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleGeneration.Decorators;

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
                        if (!(sample.SampleGenerator is IAudioSampleGenerator audioSampleGenerator))
                            continue;

                        var amplitudeFactor = audioSampleGenerator.GetAmplitudeFactor();

                        sample.OutsideVolume = OsuVolumeConverter.AmplitudeToVolume(
                            OsuVolumeConverter.VolumeToAmplitude(sample.OutsideVolume) * amplitudeFactor);

                        sample.SampleGenerator = new AmplitudeSampleDecorator(audioSampleGenerator, (float)(1 / amplitudeFactor));
                    }
                    continue;
                }

                var audioSamples = package.Samples.Where(s => s.SampleGenerator is IAudioSampleGenerator).ToArray();

                if (audioSamples.Length == 0) {
                    continue;
                }

                // ReSharper disable once PossibleNullReferenceException
                double maxAmplitude = audioSamples.Max(o => ((IAudioSampleGenerator)o.SampleGenerator).GetAmplitudeFactor());

                if (maxAmplitude < Precision.DOUBLE_EPSILON) {
                    maxAmplitude = 1;
                }

                foreach (ISample sample in audioSamples) {
                    var audioSampleGenerator = (IAudioSampleGenerator)sample.SampleGenerator;
                    // ReSharper disable once PossibleNullReferenceException
                    var sampleAmplitude = audioSampleGenerator.GetAmplitudeFactor();

                    if (sampleAmplitude < Precision.DOUBLE_EPSILON) {
                        sampleAmplitude = 1;
                    }

                    // Pick the new volume such that the samples have a volume as high as possible and the greenline brings the volume down.
                    // With this equation the final amplitude stays the same while the greenline has the volume of the loudest sample at this time.
                    double newAmplitude = OsuVolumeConverter.VolumeToAmplitude(sample.OutsideVolume) * sampleAmplitude / maxAmplitude;

                    if (Math.Abs(newAmplitude - 1) > roughness && !alwaysFullVolume) {
                        // If roughness is not 0 it will quantize the new volume in order to reduce the number of different volumes
                        newAmplitude = Math.Abs(roughness) > Precision.DOUBLE_EPSILON ? 
                            roughness * Math.Round(newAmplitude / roughness) : 
                            newAmplitude;
                    } else {
                        newAmplitude = 1;
                    }

                    sample.SampleGenerator = new AmplitudeSampleDecorator(audioSampleGenerator, (float)(newAmplitude / sampleAmplitude));
                }

                if (alwaysFullVolume) {
                    // Assuming the volume of the sample is always maximum, this equation makes sure that 
                    // the loudest sample at this time has the wanted amplitude using the volume change from the greenline.
                    package.SetAllOutsideVolume(OsuVolumeConverter.AmplitudeToVolume(
                        OsuVolumeConverter.VolumeToAmplitude(package.GetMaxOutsideVolume()) * maxAmplitude));
                } else {
                    package.SetAllOutsideVolume(OsuVolumeConverter.AmplitudeToVolume(maxAmplitude));
                }
            }
        }

        public static List<ICustomIndex> GetCustomIndices(List<ISamplePackage> packages) {

            var indices = packages.Select(o => o.GetCustomIndex()).ToList();
            indices.ForEach(o => o.CleanInvalids());
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

        public static void GiveCustomIndicesIndices(List<ICustomIndex> customIndices, bool keepExistingIndices, int startOffset=1) {
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

        /// <summary>
        /// Generates <see cref="IHitsoundEvent"/> from <see cref="ISamplePackage"/> with one hitsound event
        /// for each sample in the packages, thus possibly creating simultaneous hitsound events.
        /// </summary>
        /// <param name="samplePackages"></param>
        /// <param name="names"></param>
        /// <param name="positions"></param>
        /// <param name="maniaPositions"></param>
        /// <param name="includeRegularHitsounds"></param>
        /// <param name="allowNamingGrowth"></param>
        /// <returns></returns>
        public static List<IHitsoundEvent> GetHitsounds(ICollection<ISamplePackage> samplePackages,
            ref Dictionary<ISampleGenerator, string> names,
            ref Dictionary<ISample, Vector2> positions,
            bool maniaPositions=false, bool includeRegularHitsounds=true, bool allowNamingGrowth=false) {

            // Get all unique ISample and ISampleGenerator objects
            HashSet<ISample> allSamples = new HashSet<ISample>();
            foreach (ISamplePackage sp in samplePackages) {
                allSamples.UnionWith(sp.Samples);
            }

            HashSet<ISampleGenerator> allSampleArgs = new HashSet<ISampleGenerator>(allSamples.Select(o => o.SampleGenerator));

            // Generate names and positions for the hitsounds
            if (names == null) {
                names = HitsoundExporter.GenerateSampleNames(allSampleArgs);
            }

            if (positions == null) {
                positions = maniaPositions ? HitsoundExporter.GenerateManiaHitsoundPositions(allSamples) :
                    HitsoundExporter.GenerateHitsoundPositions(allSamples);
            }

            var hitsounds = new List<IHitsoundEvent>();
            foreach (var p in samplePackages) {
                foreach (var s in p.Samples) {
                    string filename;

                    if (s.SampleGenerator == null || !s.SampleGenerator.IsValid()) {
                        filename = string.Empty;
                    } else if (names.ContainsKey(s.SampleGenerator)) {
                        filename = names[s.SampleGenerator];
                    } else {
                        if (allowNamingGrowth) {
                            HitsoundExporter.AddNewSampleName(names, s.SampleGenerator);
                            filename = names[s.SampleGenerator];
                        } else {
                            throw new Exception($"Given sample naming schema doesn't support sample ({s.SampleGenerator}) and growth is disabled.");
                        }
                    }

                    if (s.SampleGenerator == null) {

                    }

                    if (includeRegularHitsounds) {
                        hitsounds.Add(new HitsoundEvent(p.Time,
                            positions[s], s.OutsideVolume, filename, s.SampleSet, s.SampleSet,
                            0, s.Hitsound == Hitsound.Whistle, s.Hitsound == Hitsound.Finish, s.Hitsound == Hitsound.Clap));
                    } else {
                        hitsounds.Add(new HitsoundEvent(p.Time,
                            positions[s], s.OutsideVolume, filename, SampleSet.Auto, SampleSet.Auto,
                            0, false, false, false));
                    }
                }
            }

            return hitsounds;
        }

        /// <summary>
        /// Generates 1-to-1 <see cref="IHitsoundEvent"/> of out <see cref="ISamplePackage"/> using provided custom indices.
        /// </summary>
        /// <param name="samplePackages">The SamplePackages to get hitsounds out of</param>
        /// <param name="customIndices">The CustomIndices that fit all the packages</param>
        /// <returns></returns>
        public static List<IHitsoundEvent> GetHitsounds(List<ISamplePackage> samplePackages, 
            List<ICustomIndex> customIndices) {

            List<IHitsoundEvent> hitsounds = new List<IHitsoundEvent>(samplePackages.Count);
            List<ICustomIndex> packageCustomIndices = GetCustomIndices(samplePackages);

            int index = 0;
            while (index < packageCustomIndices.Count) {
                // Find CustomIndex that fits the most packages from here
                ICustomIndex bestCustomIndex = null;
                int bestFits = 0;

                foreach (ICustomIndex ci in customIndices) {
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

        private static int NumSupportedPackages(IReadOnlyList<ICustomIndex> packageCustomIndices, int i, ICustomIndex ci) {
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

        public static ICompleteHitsounds GetCompleteHitsounds(List<ISamplePackage> packages,
            List<ICustomIndex> customIndices = null, bool allowGrowth=false, int firstCustomIndex=1) {

            if (customIndices == null) {
                customIndices = OptimizeCustomIndices(GetCustomIndices(packages));
                GiveCustomIndicesIndices(customIndices, false, firstCustomIndex);
            } else if (allowGrowth) {
                customIndices = OptimizeCustomIndices(customIndices.Concat(GetCustomIndices(packages)).ToList());
                GiveCustomIndicesIndices(customIndices, true, firstCustomIndex);
            }

            var hitsounds = GetHitsounds(packages, customIndices);

            return new CompleteHitsounds(hitsounds, customIndices);
        }
    }
}
