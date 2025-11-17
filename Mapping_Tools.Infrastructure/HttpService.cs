using Mapping_Tools.Application.Types;

namespace Mapping_Tools.Infrastructure;

public class HttpService : IHttpService
{
    private readonly HttpClient httpClient;

    public HttpService()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("user-agent", "Mapping Tools");
    }
    
    public async Task<string> GetStringAsync(string url)
    {
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}