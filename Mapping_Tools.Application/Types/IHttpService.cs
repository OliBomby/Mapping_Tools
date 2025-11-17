namespace Mapping_Tools.Application.Types;

public interface IHttpService
{
    Task<string> GetStringAsync(string url);
}