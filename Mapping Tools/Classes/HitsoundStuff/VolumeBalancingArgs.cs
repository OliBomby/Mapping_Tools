namespace Mapping_Tools.Classes.HitsoundStuff
{
    public class VolumeBalancingArgs
    {
        public double Roughness { get; set; }
        public bool AllwaysFullVolume { get; set; }

        public VolumeBalancingArgs(double roughness, bool allwaysFullVolume) {
            Roughness = roughness;
            AllwaysFullVolume = allwaysFullVolume;
        }
    }
}