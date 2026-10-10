using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Replays the recorded files and directory existence of one collection edit.</summary>
public sealed class PatternGalleryCollectionChange
{
    private readonly IPatternGalleryFileService files;
    private readonly PatternGalleryCollectionPaths paths;
    private readonly IReadOnlyList<PatternGalleryFileChange> changes;
    private readonly bool existedBefore;
    private readonly bool existedAfter;

    internal PatternGalleryCollectionChange(
        IPatternGalleryFileService files,
        PatternGalleryCollectionPaths paths,
        IReadOnlyList<PatternGalleryFileChange> changes,
        bool existedBefore,
        bool existedAfter)
    {
        this.files = files;
        this.paths = paths;
        this.changes = changes;
        this.existedBefore = existedBefore;
        this.existedAfter = existedAfter;
    }

    /// <summary>Gets the bytes retained to replay this edit.</summary>
    public long EstimatedBytes => changes.Sum(change =>
        (long)(change.Before?.Length ?? 0) + (change.After?.Length ?? 0));

    /// <summary>Restores the files and collection directory that existed before the edit.</summary>
    public void Undo()
    {
        Restore(true);
    }

    /// <summary>Restores the files and collection directory produced by the edit.</summary>
    public void Redo()
    {
        Restore(false);
    }

    private void Restore(bool undo)
    {
        bool shouldExist = undo ? existedBefore : existedAfter;
        if (!shouldExist)
        {
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var expected = changes
                .Where(change => (undo ? change.After : change.Before) is not null)
                .Select(change => change.RelativePath)
                .ToHashSet(comparer);
            if (!expected.SetEquals(files.EnumerateCollectionFiles(paths)))
                throw new IOException("The collection contains files that changed outside Mapping Tools.");
        }

        files.ApplyCollectionFileChanges(paths, changes, undo);
        if (shouldExist) files.EnsureCollection(paths);
        else files.RemoveEmptyCollection(paths);
    }
}
