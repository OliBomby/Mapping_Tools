using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools_Core.BeatmapHelper.TimelineStuff {
    /// <summary>
    /// Represents an event on the timeline. This is part of a hit object.
    /// </summary>
    public abstract class TimelineObject : ContextableBase {
        [CanBeNull]
        public HitObject Origin { get; set; }

        /// <summary>
        /// The absolute time of this timeline object in milliseconds.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// The hitsounds of this timeline object.
        /// </summary>
        [NotNull]
        public HitSampleInfo Hitsounds { get; set; }

        /// <summary>
        /// Whether this timeline object can play a hitsound.
        /// </summary>
        public abstract bool HasHitsound { get; }

        /// <summary>
        /// Whether this timeline object can use <see cref="HitSampleInfo.CustomIndex"/> or <see cref="HitSampleInfo.Filename"/> to determine its hitsound. 
        /// </summary>
        public abstract bool CanCustoms { get; }

        /// <summary>
        /// Whether this timeline object attempts to use the <see cref="HitSampleInfo.Filename"/> to determine its hitsound.
        /// </summary>
        public bool UsesFilename => !string.IsNullOrEmpty(Hitsounds.Filename) && CanCustoms;

        /// <summary>
        /// The actual sampleset used by this timeline object. Includes <see cref="TimingContext"/>.
        /// </summary>
        public SampleSet FenoSampleSet => Hitsounds.SampleSet == SampleSet.Auto
            ? GetContext<TimingContext>().HitsoundTimingPoint.SampleSet
            : Hitsounds.SampleSet;

        /// <summary>
        /// The actual additions sampleset used by this timeline object. Includes <see cref="TimingContext"/>.
        /// </summary>
        public SampleSet FenoAdditionSet => Hitsounds.AdditionSet == SampleSet.Auto ? FenoSampleSet : Hitsounds.AdditionSet;

        /// <summary>
        /// The actual custom index used by this timeline object. Includes <see cref="TimingContext"/>.
        /// </summary>
        public int FenoCustomIndex => Hitsounds.CustomIndex == 0 || !CanCustoms
            ? GetContext<TimingContext>().HitsoundTimingPoint.SampleIndex
            : Hitsounds.CustomIndex;

        /// <summary>
        /// The actual sample volume used by this timeline object. Includes <see cref="TimingContext"/>.
        /// </summary>
        public double FenoSampleVolume => Math.Abs(Hitsounds.Volume) < Precision.DOUBLE_EPSILON? GetContext<TimingContext>().HitsoundTimingPoint.Volume : Hitsounds.Volume;

        /// <summary>
        /// Generates a new <see cref="TimelineObject"/>.
        /// </summary>
        /// <param name="time">The absolute time of this timeline object in milliseconds.</param>
        /// <param name="hitsounds">The hitsounds of this timeline object.</param>
        protected TimelineObject(double time, [NotNull] HitSampleInfo hitsounds) {
            Time = time;
            Hitsounds = hitsounds.Clone();
        }

        /// <summary>
        /// Grabs the first active hitsound or the default hitsound.
        /// This will only get one type of hitsound so if there are multiple, they will be ignored.
        /// </summary>
        /// <returns>The first hitsound of this timeline object.</returns>
        public Hitsound GetHitsound() {
            if (Hitsounds.Normal) {
                return Hitsound.Normal;
            }
            if (Hitsounds.Whistle) {
                return Hitsound.Whistle;
            }
            if (Hitsounds.Finish) {
                return Hitsound.Finish;
            }
            if (Hitsounds.Clap) {
                return Hitsound.Clap;
            }
            return Hitsound.Normal;
        }

        /// <summary>
        /// Resets the <see cref="Hitsounds"/>.
        /// </summary>
        public void ResetHitsounds() {
            Hitsounds = new HitSampleInfo();
        }

        /// <summary>
        /// Checks if the selected timeline object does play a Normal hitsound.
        /// </summary>
        /// <param name="mode">The gamemode this timeline object is played in.</param>
        /// <returns>Whether this timeline object plays a Normal hitsound.</returns>
        public bool PlaysNormal(GameMode mode) {
            return mode != GameMode.Mania || Hitsounds.Normal || !(Hitsounds.Whistle || Hitsounds.Finish || Hitsounds.Clap);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public IEnumerable<Tuple<SampleSet, Hitsound, int>> GetPlayingHitsounds(GameMode mode = GameMode.Standard) {
            if (PlaysNormal(mode))
                yield return new Tuple<SampleSet, Hitsound, int>(FenoSampleSet, Hitsound.Normal, FenoCustomIndex);
            if (Hitsounds.Whistle)
                yield return new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex);
            if (Hitsounds.Finish)
                yield return new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex);
            if (Hitsounds.Clap)
                yield return new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex);
        }

        /// <summary>
        /// Grabs the playing filenames of this timeline object.
        /// </summary>
        /// <param name="mode">The osu! <see cref="GameMode"/></param>
        /// <param name="includeDefaults"></param>
        /// <returns></returns>
        public IEnumerable<string> GetPlayingFilenames(GameMode mode = GameMode.Standard, bool includeDefaults = true) {
            if (UsesFilename) {
                yield return Hitsounds.Filename;
            } else if (includeDefaults || FenoCustomIndex != 0) {
                foreach (var (sampleSet, hitsound, index) in GetPlayingHitsounds(mode)) {
                    yield return GetFileName(sampleSet, hitsound, index, mode);
                }
            }
        }

        /// <summary>
        /// Grabs the playing filenames of this timeline object and uses <see cref="firstSamples"/> to get only the first sample that makes the same sound.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="mapDir"></param>
        /// <param name="firstSamples"></param>
        /// <param name="includeDefaults"></param>
        /// <returns></returns>
        public IEnumerable<string> GetFirstPlayingFilenames(GameMode mode, string mapDir, Dictionary<string, string> firstSamples, bool includeDefaults=true) {
            if (UsesFilename) {
                string samplePath = Path.Combine(mapDir, Hitsounds.Filename);
                string fullPathExtLess = Path.Combine(
                    Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                    Path.GetFileNameWithoutExtension(samplePath));

                // Get the first occurence of this sound to not get duplicated
                if (firstSamples.Keys.Contains(fullPathExtLess)) {
                    yield return Path.GetFileName(firstSamples[fullPathExtLess]);
                }
            } else if (includeDefaults || FenoCustomIndex != 0) {
                foreach (var (sampleSet, hitsound, index) in GetPlayingHitsounds(mode)) {
                    var filename = GetFirstIdenticalFilename(sampleSet, hitsound, index, mode, mapDir, firstSamples, includeDefaults);
                    if (filename != null)
                        yield return filename;
                }
            }
        }

        private string GetFirstIdenticalFilename(SampleSet sampleSet, Hitsound hitsound, int index, GameMode mode, string mapDir, Dictionary<string, string> firstSamples, bool includeDefaults) {
            string filename = GetFileName(sampleSet, hitsound, index, mode);
            string samplePath = Path.Combine(mapDir, filename);
            string fullPathExtLess = Path.Combine(
                Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                Path.GetFileNameWithoutExtension(samplePath));

            // Get the first occurence of this sound to not get duplicated
            if (firstSamples.Keys.Contains(fullPathExtLess)) {
                if (!UsesFilename) {
                    return Path.GetFileName(firstSamples[fullPathExtLess]);
                }
            } else {
                // Sample doesn't exist
                if (!UsesFilename && includeDefaults) {
                    return GetFileName(sampleSet, hitsound, 0, mode);
                }
            }

            return null;
        }

        /// <summary>
        /// Assigns the hitsounds of this timeline object to the <see cref="Origin"/>.
        /// </summary>
        public abstract void HitoundsToOrigin();

        /// <summary>
        /// Grabs the playing file name of the object.
        /// </summary>
        /// <param name="sampleSet"></param>
        /// <param name="hitsound"></param>
        /// <param name="index"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static string GetFileName(SampleSet sampleSet, Hitsound hitsound, int index, GameMode mode) {
            string taiko = mode == GameMode.Taiko ? "taiko-" : "";
            return index switch {
                0 => $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}-default",
                1 => $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}",
                _ => $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}{index}"
            };
        }

        public TimelineObject Copy() {
            return (TimelineObject) MemberwiseClone();
        }

        public override string ToString() {
            return $"{Time}, {Hitsounds}";
        }
    }
}
