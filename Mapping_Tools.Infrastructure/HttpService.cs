using Mapping_Tools.Application.Types;

namespace Mapping_Tools.Infrastructure;

public class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;

    public HttpService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("user-agent", "Mapping Tools");
    }
    
    public async Task<string> GetStringAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}