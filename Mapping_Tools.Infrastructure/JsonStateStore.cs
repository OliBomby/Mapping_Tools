using System.Text.Json;
using Mapping_Tools.Application.Persistence;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Infrastructure;

public sealed class JsonStateStore : IStateStore {
    private readonly ILogger<JsonStateStore> logger;
    private readonly string basePath;
    private static readonly JsonSerializerOptions jsonOpts = new() { WriteIndented = true };

    public JsonStateStore(ILogger<JsonStateStore> logger) {
        this.logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        basePath = Path.Combine(appData, "Mapping Tools 2", "Test");
        Directory.CreateDirectory(basePath);
    }

    public async Task<T?> LoadAsync<T>(string key, CancellationToken ct = default)
    {
        var path = Path.Combine(basePath, $"{key}.json");
        if (!File.Exists(path)) return default;
        // Simulate a delay for demonstration purposes
        await Task.Delay(200, ct);
        await using var s = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(s, jsonOpts, ct);
    }

    public async Task SaveAsync<T>(string key, T value, CancellationToken ct = default)
    {
        var path = Path.Combine(basePath, $"{key}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Simulate a delay for demonstration purposes
        await Task.Delay(1000, ct);
        await using var s = File.Create(path);
        await JsonSerializer.SerializeAsync(s, value, jsonOpts, ct);
        logger.LogInformation("Saved state to {Path}", path);
    }
}