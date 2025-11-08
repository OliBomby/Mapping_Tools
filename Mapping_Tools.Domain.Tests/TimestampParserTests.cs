namespace Mapping_Tools.Domain.Tests;

[TestFixture]
public class TimestampParserTests
{
    // Helpers
    private static long Ms(int days = 0, int hours = 0, int minutes = 0, int seconds = 0, int millis = 0)
        => (long)days * 24 * 60 * 60 * 1000
         + (long)hours * 60 * 60 * 1000
         + (long)minutes * 60 * 1000
         + (long)seconds * 1000
         + millis;

    [Test]
    public void Parse_Simple_NoRefs()
    {
        (TimeSpan t, List<HitObjectReference> refs) = TimestampParser.ParseTimestamp("00:00:891");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(891));
        Assert.That(refs, Is.Empty);
    }

    [Test]
    public void Parse_Simple_WithComboIndex()
    {
        (TimeSpan t, List<HitObjectReference> refs) = TimestampParser.ParseTimestamp("00:00:891 (1)");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(891));
        Assert.That(refs, Is.EqualTo(new List<HitObjectReference>
        {
            new(ComboIndex: 1, Time: -1, ColumnIndex: -1)
        }));
    }

    [Test]
    public void Parse_Min60_WithPairRef_And_TrailingDash()
    {
        (TimeSpan t, List<HitObjectReference> refs) = TimestampParser.ParseTimestamp("60:00:074 (2,4) - ");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(Ms(minutes: 60, millis: 74)));
        Assert.That(refs, Is.EqualTo(new List<HitObjectReference>
        {
            new(ComboIndex: 2, Time: -1, ColumnIndex: -1),
            new(ComboIndex: 4, Time: -1, ColumnIndex: -1),
        }));
    }

    [Test]
    public void Parse_Min60_NoRefs_WithDashOrWords_Ignored()
    {
        (TimeSpan t1, List<HitObjectReference> r1) = TimestampParser.ParseTimestamp("60:00:074 - ");
        (TimeSpan t2, List<HitObjectReference> r2) = TimestampParser.ParseTimestamp("60:00:074 - words");

        Assert.That(t1.TotalMilliseconds, Is.EqualTo(Ms(minutes: 60, millis: 74)));
        Assert.That(r1, Is.Empty);

        Assert.That(t2.TotalMilliseconds, Is.EqualTo(Ms(minutes: 60, millis: 74)));
        Assert.That(r2, Is.Empty);
    }

    [Test]
    public void Parse_NegativeParts_AreSupported()
    {
        (TimeSpan t, List<HitObjectReference> refs) = TimestampParser.ParseTimestamp("00:-01:-230 (1) - ");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(-1230)); // -1s - 230ms
        Assert.That(refs, Is.EqualTo(new List<HitObjectReference>
        {
            new(ComboIndex: 1, Time: -1, ColumnIndex: -1)
        }));
    }

    [Test]
    public void Parse_MultiplePairReferences()
    {
        const string input = "00:57:031 (57031|2,57411|1,57790|0,58170|2,58170|3) - ";
        (TimeSpan t, List<HitObjectReference> refs) = TimestampParser.ParseTimestamp(input);

        Assert.That(t.TotalMilliseconds, Is.EqualTo(57031));

        var expected = new List<HitObjectReference>
        {
            new(-1, 57031, 2),
            new(-1, 57411, 1),
            new(-1, 57790, 0),
            new(-1, 58170, 2),
            new(-1, 58170, 3),
        };
        Assert.That(refs, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_FourParts_AsHoursMinutesSecondsMillis()
    {
        (TimeSpan t, _) = TimestampParser.ParseTimestamp("01:02:03:004");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(Ms(hours: 1, minutes: 2, seconds: 3, millis: 4)));
    }

    [Test]
    public void Parse_FiveParts_AsDaysHoursMinutesSecondsMillis()
    {
        (TimeSpan t, _) = TimestampParser.ParseTimestamp("1:02:03:04:005");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(Ms(days: 1, hours: 2, minutes: 3, seconds: 4, millis: 5)));
    }

    [Test]
    public void Parse_TrimsWhitespace_And_AllowsSpaces()
    {
        (TimeSpan t, List<HitObjectReference> refs) = TimestampParser.ParseTimestamp("   00:00:891   (  1  ,  2  )   -  stuff ");
        Assert.That(t.TotalMilliseconds, Is.EqualTo(891));
        Assert.That(refs, Is.EqualTo(new List<HitObjectReference>
        {
            new(1, -1, -1),
            new(2, -1, -1),
        }));
    }

    // ================= Unhappy flows / error cases =================

    [Test]
    public void Parse_Null_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp(null!), Throws.ArgumentException);
    }

    [Test]
    public void Parse_MalformedText_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp("not a timestamp"), Throws.ArgumentException);
    }

    [Test]
    public void Parse_TooFewParts_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp("00:10"), Throws.ArgumentException);
    }

    [Test]
    public void Parse_TooManyParts_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp("1:2:3:4:5:6"), Throws.ArgumentException);
    }

    [Test]
    public void Parse_InvalidReferenceToken_Throws()
    {
        // 'x' is not an int
        Assert.That(() => TimestampParser.ParseTimestamp("00:00:100 (x|2)"), Throws.ArgumentException);
    }

    [Test]
    public void Parse_EmptyReferenceItemBetweenCommas_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp("00:00:100 (1,,2)"), Throws.ArgumentException);
    }

    [Test]
    public void Parse_PartiallyMissingPipeValue_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp("00:00:100 (123|)"), Throws.ArgumentException);
    }

    [Test]
    public void Parse_ReferenceWithExtraSymbols_Throws()
    {
        Assert.That(() => TimestampParser.ParseTimestamp("00:00:100 (1|2|3)"), Throws.ArgumentException);
    }
}
