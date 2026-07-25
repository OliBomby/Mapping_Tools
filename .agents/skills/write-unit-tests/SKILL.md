---
name: write-unit-tests
description: "Write and migrate .NET unit tests using the Mapping Tools test conventions: descriptive Method_[Scenario_]Expectation names, explicit Arrange/Act/Assert sections, and Fluent Assertions. Use when adding, editing, reviewing, or migrating C# unit tests in this repository."
---

# Write Unit Tests

Follow these conventions for every unit test created or modified.

## Name Tests

Name each test method:

```text
<MethodUnderTest>_<ScenarioOrPrecondition>_<ExpectedOutcome>
```

Use a concrete arrange condition in the middle segment and the asserted behavior in the final segment. Keep the production method's spelling and casing in the first segment.

Omit `<ScenarioOrPrecondition>` when the method and expected outcome fully describe a simple test and no meaningful scenario or precondition distinguishes it:

```text
<MethodUnderTest>_<ExpectedOutcome>
```

Do not omit the scenario merely to shorten a name. Include it whenever the tested result depends on particular input, state, configuration, or an error condition.

Examples:

```csharp
RunTool_WithoutBeatmap_ThrowsNotFoundException
Parse_ValidInput_ReturnsExpectedResult
Save_WithExistingFile_OverwritesContents
GetVersion_ReturnsCurrentVersion
```

Do not use generic names such as `Test1`, `Works`, or `RunToolTest`.

## Structure Tests

Include these exact standalone comments in this order in every test method:

```csharp
// Arrange
// Act
// Assert
```

Put setup and inputs under `// Arrange`, the single behavior under test under `// Act`, and verification under `// Assert`. Keep the Act section focused on one logical operation.

For exception tests, capture the operation in the Act section so throwing it and checking it remain separate:

```csharp
[Fact]
public void RunTool_WithoutBeatmap_ThrowsNotFoundException()
{
    // Arrange
    var sut = CreateTool(beatmap: null);

    // Act
    Action act = () => sut.RunTool();

    // Assert
    act.Should().Throw<NotFoundException>();
}
```

Use the equivalent `Func<Task>` pattern for asynchronous exceptions.

## Assert Fluently

Use Fluent Assertions for all assertions. Add `using FluentAssertions;` when implicit or global imports do not already provide it.

Prefer expressions such as:

```csharp
result.Should().Be(expected);
items.Should().Equal(expectedItems);
value.Should().NotBeNull();
act.Should().Throw<InvalidOperationException>();
await act.Should().ThrowAsync<InvalidOperationException>();
```

Do not introduce framework-native assertions such as `Assert.Equal`, `Assert.Throws`, or `CollectionAssert`. Preserve the project's existing test runner, attributes, fixtures, and parameterized-test mechanism.

## Add or Migrate Tests

1. Inspect the test project and nearby tests to identify the existing test framework, setup pattern, and package availability.
2. Add a compatible Fluent Assertions package reference when the target test project does not already have one.
3. Rename test methods to the required pattern without changing what they test.
4. Separate each test into the three required commented sections.
5. Replace native or alternative assertion APIs with semantically equivalent Fluent Assertions.
6. Preserve coverage and behavior. Do not weaken assertions merely to make a migration pass.
7. Build and run the affected test project. Fix compilation errors and unintended test failures before finishing.
