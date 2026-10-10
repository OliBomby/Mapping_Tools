using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Desktop.Services.Undo;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Services;

internal sealed class PatternGalleryFileUndoChange(PatternGalleryCollectionChange change) : IProjectUndoExternalChange
{
    public long EstimatedBytes => change.EstimatedBytes;

    public void Undo() => change.Undo();

    public void Redo() => change.Redo();
}
