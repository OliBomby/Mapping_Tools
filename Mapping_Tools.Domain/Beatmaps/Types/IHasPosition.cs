using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Types;

public interface IHasPosition {
    Vector2 Pos { get; }
}