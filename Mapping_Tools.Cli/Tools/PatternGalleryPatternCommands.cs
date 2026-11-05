using System.CommandLine;
using System.Globalization;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tools;

/// <summary>Defines Pattern Gallery pattern commands and their operation handlers.</summary>
internal static class PatternGalleryPatternCommands
{
    internal const string LIST = "list";
    internal const string SHOW = "show";
    internal const string IMPORT = "import";
    internal const string RENAME = "rename";
    internal const string DELETE = "delete";
    internal const string EXPORT = "export";

    /// <summary>Registers pattern operations directly on the Pattern Gallery tool command.</summary>
    /// <param name="tool">The owning Pattern Gallery tool command.</param>
    internal static void Register(ToolCommand tool)
    {
        tool.Subcommands.Add(PatternGalleryCommand.Operation(tool, LIST, ListAsync,
            CliOptions.Collection.Name));
        tool.Subcommands.Add(PatternGalleryCommand.Operation(tool, SHOW, ShowAsync,
            CliOptions.Collection.Name, CliOptions.Pattern.Name, CliOptions.AllPatterns.Name));

        Command import = PatternGalleryCommand.Operation(tool, IMPORT, ImportAsync,
            CliOptions.Collection.Name,
            CliOptions.Name.Name,
            CliOptions.ObjectCode.Name,
            CliOptions.TimingCode.Name,
            CliOptions.GlobalSv.Name,
            CliOptions.GameMode.Name,
            CliOptions.Source.Name,
            CliOptions.Selection.Name,
            CliOptions.TimeCode.Name,
            CliOptions.StartTime.Name,
            CliOptions.EndTime.Name,
            CliOptions.Group.Name);
        CommandOptions.AddTargets(import);
        tool.Subcommands.Add(import);

        tool.Subcommands.Add(PatternGalleryCommand.Operation(tool, RENAME, RenameAsync,
            CliOptions.Collection.Name, CliOptions.Pattern.Name, CliOptions.AllPatterns.Name,
            CliOptions.Name.Name, CliOptions.Group.Name));
        tool.Subcommands.Add(PatternGalleryCommand.Operation(tool, DELETE, DeleteAsync,
            CliOptions.Collection.Name, CliOptions.Pattern.Name, CliOptions.AllPatterns.Name));

        Command export = PatternGalleryCommand.Operation(tool, EXPORT, ExportAsync,
            CliOptions.Collection.Name, CliOptions.Pattern.Name, CliOptions.AllPatterns.Name);
        CommandOptions.AddToolOptions(export, tool.Schema, tool.CliOptionDescriptions.Keys.ToHashSet(StringComparer.Ordinal));
        CommandOptions.AddTargets(export);
        tool.Subcommands.Add(export);
    }

    private static Task<int> ListAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (_, _, collection) = PatternGalleryCollectionCommands.ReadSelected(context);

        foreach (var pattern in collection.Patterns)
            context.Output.WriteLine($"{pattern.FileName}\t{pattern.Name}\t{pattern.ObjectCount}");

        return Task.FromResult(0);
    }

    private static Task<int> ShowAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (_, _, collection) = PatternGalleryCollectionCommands.ReadSelected(context);

        foreach (var pattern in Select(context, collection))
            context.Report(pattern);

        return Task.FromResult(0);
    }

    private static async Task<int> ImportAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (_, paths, collection) = PatternGalleryCollectionCommands.ReadSelected(context);
        var gallery = context.Get<IPatternGalleryService>();
        string name = context.RequireOption(CliOptions.Name);
        PatternGalleryPattern added;

        if (context.GetOption(CliOptions.ObjectCode) is { } code)
        {
            added = await gallery.ImportCodeAsync(
                name,
                context.Files.ReadAllText(code),
                context.GetOption(CliOptions.TimingCode) is { } timing ? context.Files.ReadAllText(timing) : "",
                context.GetOption(CliOptions.GlobalSv) is { } sv ? double.Parse(sv, CultureInfo.InvariantCulture) : 1.4,
                context.GetOption(CliOptions.GameMode) is { } gameMode ? Enum.Parse<GameMode>(gameMode, true) : GameMode.Standard,
                collection,
                paths,
                context.CancellationToken);
        }
        else
        {
            string source = context.GetOption(CliOptions.Source) ?? await context.OnePathAsync();
            var selection = context.Selection();

            if (selection == HitObjectSelectionMode.Bookmarked)
                throw new ArgumentException(CliStrings.GalleryBookmarks);

            added = selection == HitObjectSelectionMode.Selected
                ? await gallery.ImportSelectedAsync(source, name, paths, context.CancellationToken)
                : await gallery.ImportFileAsync(
                    source,
                    name,
                    selection == HitObjectSelectionMode.Time ? context.TimeCode : null,
                    context.GetOption(CliOptions.StartTime) is { } start
                        ? double.Parse(start, CultureInfo.InvariantCulture)
                        : double.NegativeInfinity,
                    context.GetOption(CliOptions.EndTime) is { } end
                        ? double.Parse(end, CultureInfo.InvariantCulture)
                        : double.PositiveInfinity,
                    paths,
                    context.CancellationToken);
        }

        if (context.GetOption(CliOptions.Group) is { } group)
            added.Group = group;

        if (!collection.Patterns.Contains(added))
            collection.Patterns.Add(added);

        context.Report(added);
        await PatternGalleryCommand.SaveAsync(context, paths, collection);

        return 0;
    }

    private static async Task<int> RenameAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (_, paths, collection) = PatternGalleryCollectionCommands.ReadSelected(context);

        foreach (var pattern in Select(context, collection))
        {
            pattern.Name = context.RequireOption(CliOptions.Name);

            if (context.GetOption(CliOptions.Group) is { } group)
                pattern.Group = group;
        }

        await PatternGalleryCommand.SaveAsync(context, paths, collection);

        return 0;
    }

    private static async Task<int> DeleteAsync(ToolContext context, PatternGalleryServiceOptions _)
    {
        var (_, paths, collection) = PatternGalleryCollectionCommands.ReadSelected(context);
        var removed = Select(context, collection);
        var gallery = context.Get<IPatternGalleryService>();
        await gallery.DeleteAsync(removed, paths, context.CancellationToken);

        foreach (var pattern in removed)
            collection.Patterns.Remove(pattern);

        await PatternGalleryCommand.SaveAsync(context, paths, collection);

        return 0;
    }

    private static async Task<int> ExportAsync(ToolContext context, PatternGalleryServiceOptions configuration)
    {
        var (_, paths, collection) = PatternGalleryCollectionCommands.ReadSelected(context);
        var selected = Select(context, collection);
        var gallery = context.Get<IPatternGalleryService>();
        configuration.Patterns = collection.Patterns;

        context.Report(await gallery.ExportAsync(
            await context.OnePathAsync(),
            selected,
            configuration,
            paths,
            CliOptions.ReloadEditor.GetValue(context.ParseResult),
            cancellationToken: context.CancellationToken));
        await PatternGalleryCommand.SaveAsync(context, paths, collection);

        return 0;
    }

    private static List<PatternGalleryPattern> Select(ToolContext context, PatternGalleryServiceOptions collection)
    {
        if (CliOptions.AllPatterns.GetValue(context.ParseResult)
            || (!context.HasOption(CliOptions.Pattern)
                && context.Document[CliDocumentKeys.ALL_PATTERNS]?.Value<bool>() == true))
            return collection.Patterns.ToList();

        var ids = context.OptionValues(CliOptions.Pattern);

        if (ids.Count == 0)
            ids = context.Document[CliDocumentKeys.PATTERNS]?.Values<string>().OfType<string>().ToList() ?? [];

        if (ids.Count == 0)
            throw new ArgumentException(CliStrings.PatternRequired);

        return ids.Select(id => collection.Patterns.SingleOrDefault(pattern => pattern.FileName == id)
            ?? throw new ArgumentException(ApplicationText.Format(CliStrings.NotFound, id))).ToList();
    }
}
