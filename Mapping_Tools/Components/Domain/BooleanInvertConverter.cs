namespace Mapping_Tools.Components.Domain
{
    public sealed class BooleanInvertConverter : BooleanConverter<bool>
    {
        public BooleanInvertConverter() :
            base(false, true)
        { }
    }
}
