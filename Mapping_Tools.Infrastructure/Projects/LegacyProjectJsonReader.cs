namespace Mapping_Tools.Infrastructure.Projects;

internal sealed class LegacyProjectJsonReader
{
    private readonly LegacyProjectJsonSerializer serializer = new();

    internal TProject Read<TProject>(string json)
    {
        return serializer.Deserialize<TProject>(json);
    }
}
