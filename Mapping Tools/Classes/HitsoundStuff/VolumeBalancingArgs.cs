namespace Mapping_Tools.Classes.HitsoundStuff
{
    /// <summary>
    /// Arguments that specify how the volume should be
    /// balanced out when combining two or more <see cref="Sample"/> together
    /// </summary>
    public class VolumeBalancingArgs
    {
        /// <summary>
        /// Specifies how frequent it can change the volume of both samples before combining.
        /// </summary>
        public double Roughness { get; set; }
        /// <summary>
        /// Used if both samples are at full volume when combining.
        /// </summary>
        public bool AlwaysFullVolume { get; set; }

        /// <inheritdoc />
        public VolumeBalancingArgs(double roughness, bool alwaysFullVolume) {
            Roughness = roughness;
            AlwaysFullVolume = alwaysFullVolume;
        }
    }
}