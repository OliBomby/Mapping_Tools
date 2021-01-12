using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public interface ISamplePackage {
        /// <summary>
        /// The time in milliseconds.
        /// </summary>
        double Time { get; }

        /// <summary>
        /// The samples.
        /// </summary>
        ISet<ISample> Samples { get; }

        /// <summary>
        /// The maximum outside volume of all the <see cref="Samples"/>.
        /// Can be used as greenline volume.
        /// </summary>
        /// <returns></returns>
        double GetMaxOutsideVolume();

        /// <summary>
        /// Sets the outside volume of all <see cref="Samples"/>.
        /// </summary>
        /// <param name="outsideVolume">The volume to set it to</param>
        void SetAllOutsideVolume(double outsideVolume);

        /// <summary>
        /// Gets the sample set for the hitnormal
        /// using the priority of the <see cref="Samples"/>.
        /// </summary>
        /// <returns></returns>
        SampleSet GetSampleSet();

        /// <summary>
        /// Gets the sample set for the additions
        /// using the priority of the <see cref="Samples"/>.
        /// </summary>
        /// <returns></returns>
        SampleSet GetAdditions();

        /// <summary>
        /// Generates a custom index with all the samples
        /// required for this package.
        /// </summary>
        /// <returns></returns>
        ICustomIndex GetCustomIndex();

        /// <summary>
        /// Generates a new <see cref="IHitsoundEvent"/>
        /// with the sample sets and hitsounds of this package.
        /// </summary>
        /// <param name="index">The index for the custom index</param>
        /// <returns></returns>
        IHitsoundEvent GetHitsound(int index);
    }
}