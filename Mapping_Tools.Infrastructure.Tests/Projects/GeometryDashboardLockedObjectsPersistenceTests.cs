using FluentAssertions;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class GeometryDashboardLockedObjectsPersistenceTests
{
    [TestMethod]
    public void DeserializeAndSerialize_LegacyLockedVirtualObjects_PreservesGeometryAndStableTypes()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GeometryDashboard", "locked-virtual-objects.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        RelevantObjectCollection objects = serializer.Deserialize<RelevantObjectCollection>(File.ReadAllText(fixture));
        string json = serializer.Serialize(objects);

        // Assert
        objects[typeof(RelevantPoint)].Should().HaveCount(10);
        objects[typeof(RelevantCircle)].Should().HaveCount(2);
        objects.Values.SelectMany(values => values).Should().OnlyContain(value => value.IsLocked);
        ((RelevantPoint)objects[typeof(RelevantPoint)][0]).Child.Should().Be(new Mapping_Tools.Core.Classes.MathUtil.Vector2(342, 98));
        json.Should().Contain("Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects.RelevantPoint, Mapping Tools");
    }
}
