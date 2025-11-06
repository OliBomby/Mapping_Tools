namespace Mapping_Tools.Domain.Beatmaps.Contexts;

public interface IContext {
    /// <summary>
    /// Makes a deep copy of this context.
    /// </summary>
    /// <returns>The deep copy of this context.</returns>
    public IContext Copy();
}