using System.Text.Json;
using Mapping_Tools.Infrastructure.Updates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Updates;

[TestClass]
public sealed class GithubReleaseMetadataParserTests
{
    [TestMethod]
    public void Parse_WithGitHubReleaseObject_ReturnsTitleAndBody()
    {
        // Arrange
        const string json = "{\"name\":\"Mapping Tools 2.0\",\"body\":\"Bug fixes\"}";

        // Act
        var notes = GithubReleaseMetadataParser.Parse(json);

        // Assert
        notes.Title.Should().Be("Mapping Tools 2.0");
        notes.Body.Should().Be("Bug fixes");
    }

    [TestMethod]
    public void Parse_WithNullResponse_ReturnsEmptyMetadata()
    {
        // Arrange
        const string json = "null";

        // Act
        var notes = GithubReleaseMetadataParser.Parse(json);

        // Assert
        notes.Title.Should().BeNull();
        notes.Body.Should().BeNull();
    }

    [TestMethod]
    public void Parse_WithMalformedPayload_ThrowsJsonException()
    {
        // Arrange
        const string json = "{";

        // Act
        Action act = () => GithubReleaseMetadataParser.Parse(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Parse_WithNonObjectPayload_ThrowsJsonException()
    {
        // Arrange
        const string json = "[]";

        // Act
        Action act = () => GithubReleaseMetadataParser.Parse(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Parse_WithWronglyTypedProperty_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"name\":42}";

        // Act
        Action act = () => GithubReleaseMetadataParser.Parse(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void ParseMany_WithGitHubReleaseArray_ReturnsAllReleasesInSourceOrder()
    {
        // Arrange
        const string json = "[{\"name\":\"Version 2.0\",\"body\":\"New\"},{\"name\":null,\"tag_name\":\"v1.0\",\"body\":\"Old\"}]";

        // Act
        var notes = GithubReleaseMetadataParser.ParseMany(json);

        // Assert
        notes.Select(note => note.Title).Should().Equal("Version 2.0", "v1.0");
        notes.Select(note => note.Body).Should().Equal("New", "Old");
    }
}
