using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Services.Undo;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Services;

internal sealed class PatternGalleryCollectionMove : IProjectUndoExternalChange
{
    private readonly IPatternGalleryFileService files;
    private readonly PatternGalleryCollectionPaths before;
    private readonly PatternGalleryCollectionPaths after;

    public PatternGalleryCollectionMove(
        IPatternGalleryFileService files,
        PatternGalleryCollectionPaths before,
        PatternGalleryCollectionPaths after)
    {
        this.files = files;
        this.before = before;
        this.after = after;
    }

    public void Undo()
    {
        files.RenameCollection(after, Path.GetFileName(before.Collection));
    }

    public void Redo()
    {
        files.RenameCollection(before, Path.GetFileName(after.Collection));
    }
}
