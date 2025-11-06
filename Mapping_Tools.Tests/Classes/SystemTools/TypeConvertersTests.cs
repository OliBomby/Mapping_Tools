using Mapping_Tools.Classes.SystemTools;
using NUnit.Framework;

namespace Mapping_Tools.Tests.Classes.SystemTools;

[TestFixture]
public class TypeConvertersTests {
    [Test]
    public void TimestampParserTest() {
        var test1 = TypeConverters.ParseOsuTimestamp("00:00:891 (1) - ");
        Assert.That(test1.TotalMilliseconds, Is.EqualTo(891));

        var test2 = TypeConverters.ParseOsuTimestamp("60:00:074 (2,4) - ");
        Assert.That(test2.TotalMilliseconds, Is.EqualTo(3600074));

        var test3 = TypeConverters.ParseOsuTimestamp("60:00:074 - ");
        Assert.That(test3.TotalMilliseconds, Is.EqualTo(3600074));

        var test4 = TypeConverters.ParseOsuTimestamp("00:-01:-230 (1) - ");
        Assert.That(test4.TotalMilliseconds, Is.EqualTo(-1230));
    }
}