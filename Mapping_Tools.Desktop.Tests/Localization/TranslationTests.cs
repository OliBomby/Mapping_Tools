using Mapping_Tools.Application.Settings.Models;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Localization;

[TestClass]
[DoNotParallelize]
public sealed class TranslationTests
{
    [TestMethod]
    public void SetLanguage_Dutch_DoesNotChangeFormattingOrParsingCultures()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        var defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        InvariantDoubleConverter converter = new();

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            string translated = ApplicationText.Format(DesktopStrings.Shell_MapsTotal, 1234);
            object displayed = converter.Convert(1.25, typeof(string), null, CultureInfo.GetCultureInfo("nl-NL"));
            object parsed = converter.ConvertBack("1.25", typeof(double), null, CultureInfo.GetCultureInfo("nl-NL"));

            // Assert
            translated.Should().Be("(1234) beatmaps in totaal");
            displayed.Should().Be("1.25");
            parsed.Should().Be(1.25);
            CultureInfo.CurrentCulture.Should().BeSameAs(culture);
            CultureInfo.CurrentUICulture.Should().BeSameAs(uiCulture);
            CultureInfo.DefaultThreadCurrentCulture.Should().BeSameAs(defaultCulture);
            CultureInfo.DefaultThreadCurrentUICulture.Should().BeSameAs(defaultUiCulture);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_OpenPreferences_UpdatesExistingViewWithoutReplacingIt()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        PreferencesView view = new();
        using var host = HeadlessViewHost.Show(view);
        TextBlock heading = view.GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == "Preferences");

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            heading.Text.Should().Be("Voorkeuren");
            host.Window.Content.Should().BeSameAs(view);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_TranslatedTextConverter_ReevaluatesItsExistingBinding()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        TextBlock text = new() { DataContext = EditorReloadMode.Disabled };
        var extension = new TranslatedBindingExtension { Converter = ToolChoiceConverter.Instance };
        text.Bind(TextBlock.TextProperty, (Avalonia.Data.MultiBinding)extension.ProvideValue(null!));
        using var host = HeadlessViewHost.Show(text);

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            text.Text.Should().Be("Uitgeschakeld");
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_ApplicationResourceBinding_UpdatesGetterFromSpecifiedClass()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        TextBlock text = new();
        var extension = new TrExtension(nameof(ApplicationStrings.Exception_InvalidInput))
        {
            ResourceType = typeof(ApplicationStrings),
        };
        text.Bind(TextBlock.TextProperty, (Avalonia.Data.Binding)extension.ProvideValue(null!));
        using var host = HeadlessViewHost.Show(text);
        string? english = text.Text;

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            text.Text.Should().Be(ApplicationStrings.Exception_InvalidInput).And.NotBe(english);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_UnsupportedLanguage_FallsBackToEnglish()
    {
        // Arrange
        string? previous = TranslationManager.Language;

        try
        {
            // Act
            TranslationManager.SetLanguage("sv-SE");
            string text = DesktopStrings.Shell_Preferences;

            // Assert
            text.Should().Be("Preferences");
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_FrenchRegion_UsesFrenchResources()
    {
        // Arrange
        string? previous = TranslationManager.Language;

        try
        {
            // Act
            TranslationManager.SetLanguage("fr-FR");

            // Assert
            TranslationManager.Language.Should().Be("fr");
            DesktopStrings.Shell_Preferences.Should().Be("Préférences");
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_DutchRegion_UsesDutchWhilePreservingToolNames()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        var definition = SlideratorToolDefinition.Definition;
        TranslationManager.SetLanguage("en");
        string englishDescription = definition.Description;

        try
        {
            // Act
            TranslationManager.SetLanguage("nl-BE");

            // Assert
            definition.DisplayName.Should().Be("Sliderator");
            definition.Id.Should().Be("sliderator");
            definition.Description.Should().NotBe(englishDescription);
            DesktopStrings.Shell_Preferences.Should().Be("Voorkeuren");
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SetLanguage_LiveBindingNotification_GeneratedGettersAlreadyUseSelectedLanguage()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        string? observed = null;
        void observe(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(LocalizationState.LanguageVersion)) observed = DesktopStrings.Shell_Preferences;
        }
        LocalizationState.Instance.PropertyChanged += observe;

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");

            // Assert
            observed.Should().Be("Voorkeuren");
        }
        finally
        {
            LocalizationState.Instance.PropertyChanged -= observe;
            TranslationManager.SetLanguage(previous);
        }
    }

    [DataTestMethod]
    [DataRow("en")]
    [DataRow("nl")]
    public void SetLanguage_AllResourceEntries_GeneratedGettersReturnSelectedTranslations(string language)
    {
        // Arrange
        string? previous = TranslationManager.Language;
        string repository = typeof(TranslationTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "MappingToolsRepositoryRoot").Value!;
        (string Project, string File, Type ResourceType)[] catalogs =
        [
            ("Mapping_Tools.Application", "Application", typeof(ApplicationStrings)),
            ("Mapping_Tools.Desktop", "Desktop", typeof(DesktopStrings)),
        ];
        TranslationManager.SetLanguage(language == "en" ? "nl" : "en");

        try
        {
            // Act
            TranslationManager.SetLanguage(language);
            var actual = catalogs.Select(catalog => catalog.ResourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(property => property.PropertyType == typeof(string))
                .ToDictionary(property => property.Name, property => ((string?)property.GetValue(null))?.ReplaceLineEndings("\n")))
                .ToArray();

            // Assert
            for (int index = 0; index < catalogs.Length; index++)
            {
                var catalog = catalogs[index];
                string suffix = language == "en" ? ".resx" : ".nl.resx";
                string path = Path.Combine(repository, catalog.Project, "Localization", catalog.File + suffix);
                // XML parsing and MSBuild resource generation use different newline conventions on Windows.
                var expected = XDocument.Load(path).Root!.Elements("data")
                    .ToDictionary(entry => entry.Attribute("name")!.Value, entry => entry.Element("value")!.Value.ReplaceLineEndings("\n"));
                actual[index].Should().BeEquivalentTo(expected);
            }
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void Resources_AllEnglishEntries_HaveDutchTextContextAndMatchingPlaceholders()
    {
        // Arrange
        string repository = typeof(TranslationTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "MappingToolsRepositoryRoot").Value!;
        string[] projects = ["Mapping_Tools.Application", "Mapping_Tools.Desktop", "Mapping_Tools.SamplePlugin"];

        // Act
        string[] baselines = projects.SelectMany(project => Directory.GetFiles(Path.Combine(repository, project, "Localization"), "*.resx"))
            .Where(path => !Path.GetFileNameWithoutExtension(path).Contains('.')).ToArray();

        // Assert
        baselines.Should().HaveCount(projects.Length);
        foreach (string project in projects)
        {
            string basename = project["Mapping_Tools.".Length..];
            Directory.GetFiles(Path.Combine(repository, project, "Localization"), "*.resx")
                .Select(Path.GetFileName).Should().Contain(basename + ".resx", basename + ".nl.resx");
        }
        foreach (string baseline in baselines)
        {
            string dutch = Path.ChangeExtension(baseline, ".nl.resx");
            File.Exists(dutch).Should().BeTrue($"{baseline} needs a Dutch catalog");
            var englishEntries = XDocument.Load(baseline).Root!.Elements("data").ToArray();
            var dutchEntries = XDocument.Load(dutch).Root!.Elements("data").ToArray();
            englishEntries.Select(entry => entry.Attribute("name")!.Value).Should().OnlyHaveUniqueItems();
            dutchEntries.Select(entry => entry.Attribute("name")!.Value).Should().OnlyHaveUniqueItems();
            dutchEntries.Select(entry => entry.Attribute("name")!.Value).Should().BeEquivalentTo(englishEntries.Select(entry => entry.Attribute("name")!.Value));
            var byKey = dutchEntries.ToDictionary(entry => entry.Attribute("name")!.Value);
            foreach (var english in englishEntries)
            {
                string key = english.Attribute("name")!.Value;
                string source = english.Element("value")!.Value;
                string translated = byKey[key].Element("value")!.Value;
                source.Should().NotBeNullOrWhiteSpace(key);
                translated.Should().NotBeNullOrWhiteSpace(key);
                (english.Element("comment")?.Value).Should().NotBeNullOrWhiteSpace($"{key} needs translator context");
                (byKey[key].Element("comment")?.Value).Should().Be(english.Element("comment")!.Value, $"{key} must carry the same context in both catalogs");
                Placeholders(translated).Should().BeEquivalentTo(Placeholders(source), $"{key} must preserve numbered placeholders");
            }
        }
    }

    [TestMethod]
    public void Resources_ReferencedTextKeys_ExistInTheirEnglishCatalogs()
    {
        // Arrange
        string repository = typeof(TranslationTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "MappingToolsRepositoryRoot").Value!;
        string[] projects = ["Mapping_Tools.Application", "Mapping_Tools.Desktop", "Mapping_Tools.SamplePlugin"];
        var keys = projects.SelectMany(project => Directory.GetFiles(Path.Combine(repository, project, "Localization"), "*.resx"))
            .Where(path => !Path.GetFileNameWithoutExtension(path).Contains('.'))
            .SelectMany(path => XDocument.Load(path).Root!.Elements("data").Select(entry => entry.Attribute("name")!.Value))
            .ToHashSet(StringComparer.Ordinal);
        var markup = new Regex(@"\{loc:Tr\s+(\w+)(?:\}|,)");

        // Act
        var references = projects.SelectMany(project => Directory.EnumerateFiles(Path.Combine(repository, project), "*", SearchOption.AllDirectories))
            .Where(path => Path.GetExtension(path) is ".cs" or ".axaml")
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                           && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .SelectMany(path => markup.Matches(File.ReadAllText(path))
                .Select(match => (Path: path, Key: match.Groups[1].Value))).ToArray();

        // Assert
        references.Should().NotBeEmpty();
        foreach (var reference in references)
            keys.Should().Contain(reference.Key, $"{reference.Path} references this translation key");
    }

    private static IEnumerable<string> Placeholders(string text)
    {
        return Regex.Matches(text, @"(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})").Select(match => match.Groups[1].Value);
    }
}
