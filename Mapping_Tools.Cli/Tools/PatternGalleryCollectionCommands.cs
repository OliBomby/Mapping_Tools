using System.CommandLine;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Spectre.Console;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Defines Pattern Gallery collection commands and their operation handlers.</summary>
internal static class PatternGalleryCollectionCommands
{
    internal const string GROUP = "collection";
    internal const string LIST = "list";
    internal const string CREATE = "create";
    internal const string RENAME = "rename";
    internal const string DELETE = "delete";
    internal const string IMPORT = "import";
    internal const string EXPORT = "export";
    internal const string MERGE = "merge";
    internal const string RESTORE = "restore";

    /// <summary>Creates the collection command group and registers each operation directly beneath it.</summary>
    /// <param name="tool">The owning Pattern Gallery tool command.</param>
    /// <param name="console">Renders help for the group command.</param>
    /// <returns>The configured collection command group.</returns>
    internal static Command Create(ToolCommand tool, IAnsiConsole console)
    {
        Command group = new(GROUP);
        group.SetAction(result => new HelpWriter(console).Invoke(result));

        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, LIST, ListAsync));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, CREATE, CreateAsync,
            CliOptions.Collection.Name, CliOptions.Name.Name));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, RENAME, RenameAsync,
            CliOptions.Collection.Name, CliOptions.Name.Name, CliOptions.FolderName.Name));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, DELETE, DeleteAsync,
            CliOptions.Collection.Name));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, IMPORT, (context, _) => ImportAsync(context, false),
            CliOptions.Source.Name, CliOptions.Collection.Name, CliOptions.Merge.Name));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, MERGE, (context, _) => ImportAsync(context, true),
            CliOptions.Source.Name, CliOptions.Collection.Name));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, EXPORT, ExportAsync,
            CliOptions.Collection.Name, CliOptions.Output.Name));
        group.Subcommands.Add(PatternGalleryCommand.Operation(tool, RESTORE, RestoreAsync,
            CliOptions.Collection.Name));

        return group;
    }

    private static Task<int> ListAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var files = context.Get<IPatternGalleryFileService>();

        foreach (string id in files.EnumerateCollections(context.Library))
        {
            var paths = files.Resolve(context.Library, new PatternGalleryCollectionMetadata { CollectionFolderName = id });
            string name = context.Files.FileExists(paths.ProjectFile)
                ? PatternGalleryCommand.Read(context, paths).CollectionName
                : id;
            context.Output.WriteLine($"{id}\t{name}");
        }

        return Task.FromResult(0);
    }

    private static async Task<int> CreateAsync(ToolContext context, PatternGalleryServiceOptions configuration)
    {
        var files = context.Get<IPatternGalleryFileService>();
        string id = context.RequireOption(CliOptions.Collection);
        var paths = files.Resolve(context.Library, new PatternGalleryCollectionMetadata { CollectionFolderName = id });

        if (files.CollectionExists(paths))
            throw new IOException(ApplicationText.Format(CliStrings.CollectionConflict, id));

        configuration.FileHandler.CollectionFolderName = id;
        configuration.CollectionName = context.GetOption(CliOptions.Name) ?? id;
        files.EnsureCollection(paths);
        await PatternGalleryCommand.SaveAsync(context, paths, configuration);
        context.Report(paths.Collection);

        return 0;
    }

    private static async Task<int> RenameAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (files, paths, collection) = ReadSelected(context);
        collection.CollectionName = context.RequireOption(CliOptions.Name);

        if (context.GetOption(CliOptions.FolderName) is { } folder)
        {
            paths = files.RenameCollection(paths, folder);
            collection.FileHandler.CollectionFolderName = folder;
        }

        await PatternGalleryCommand.SaveAsync(context, paths, collection);

        return 0;
    }

    private static Task<int> DeleteAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (files, paths, _) = ReadSelected(context);
        files.DeleteCollection(paths);
        context.Report(paths.Collection);

        return Task.FromResult(0);
    }

    private static async Task<int> ImportAsync(ToolContext context, bool forceMerge)
    {
        var files = context.Get<IPatternGalleryFileService>();
        var gallery = context.Get<IPatternGalleryService>();
        var archives = context.Get<IPatternGalleryArchiveService>();
        var archive = await archives.ReadAsync(context.RequireOption(CliOptions.Source), context.CancellationToken);
        var imported = PatternGalleryCommand.Deserialize(archive.ProjectJson);
        string id = context.GetOption(CliOptions.Collection) ?? archive.CollectionFolderName;
        imported.FileHandler.CollectionFolderName = id;

        var paths = files.Resolve(context.Library, imported.FileHandler);
        bool merge = forceMerge || CliOptions.Merge.GetValue(context.ParseResult);

        if (merge)
        {
            var existing = PatternGalleryCommand.Read(context, paths);
            gallery.MergeCollection(existing, imported, archive.PatternFiles, paths);
            await PatternGalleryCommand.SaveAsync(context, paths, existing);
        }
        else
        {
            if (files.CollectionExists(paths))
                throw new IOException(ApplicationText.Format(CliStrings.CollectionConflict, id));

            files.EnsureCollection(paths);

            foreach (var pattern in archive.PatternFiles)
                files.WritePatternBytes(files.GetPatternPath(paths, pattern.FileName), pattern.Content);

            await PatternGalleryCommand.SaveAsync(context, paths, imported);
        }

        context.Report(paths.Collection);

        return 0;
    }

    private static async Task<int> ExportAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (files, paths, collection) = ReadSelected(context);
        var archives = context.Get<IPatternGalleryArchiveService>();
        string output = context.RequireOption(CliOptions.Output);
        context.CheckOutput(output);

        var patternFiles = files.EnumeratePatternFiles(paths)
            .Select(name => new PatternGalleryArchiveFile(
                name, files.ReadPatternBytes(files.GetPatternPath(paths, name))))
            .ToArray();

        await archives.ExportAsync(
            output,
            collection.FileHandler.CollectionFolderName,
            "project.json",
            PatternGalleryCommand.Serialize(collection),
            patternFiles,
            context.CancellationToken,
            overwrite: CliOptions.Overwrite.GetValue(context.ParseResult));

        context.ReportFile(Path.GetFullPath(output));
        context.Report(output);

        return 0;
    }

    private static async Task<int> RestoreAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (_, paths, collection) = ReadSelected(context);
        var gallery = context.Get<IPatternGalleryService>();
        context.Report(await gallery.RestoreAsync(collection, paths, context.CancellationToken));
        await PatternGalleryCommand.SaveAsync(context, paths, collection);

        return 0;
    }

    /// <summary>Resolves the requested collection from an explicit option or the loaded project.</summary>
    /// <param name="context">The current operation context.</param>
    /// <returns>The collection file service, resolved paths, and loaded project.</returns>
    internal static (IPatternGalleryFileService Files, PatternGalleryCollectionPaths Paths, PatternGalleryServiceOptions Collection)
        ReadSelected(ToolContext context)
    {
        var files = context.Get<IPatternGalleryFileService>();
        string id = context.GetOption(CliOptions.Collection)
            ?? (context.HasOption(CliOptions.Project)
                ? ((PatternGalleryServiceOptions)context.Project!).FileHandler.CollectionFolderName
                : null)
            ?? throw new ArgumentException(CliStrings.CollectionRequired);
        var paths = files.Resolve(context.Library, new PatternGalleryCollectionMetadata { CollectionFolderName = id });
        var collection = PatternGalleryCommand.Read(context, paths);
        paths = files.Resolve(context.Library, collection.FileHandler);

        return (files, paths, collection);
    }
}
