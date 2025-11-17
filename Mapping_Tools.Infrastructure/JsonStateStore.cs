using System.Text.Json;
using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Application.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Infrastructure;

public sealed class JsonStateStore : IStateStore
{
    private readonly KeyedAsyncLock _lock = new();
    private readonly ILogger<JsonStateStore> logger;
    private readonly string basePath;
    private static readonly JsonSerializerOptions jsonOpts = new() { WriteIndented = true };

    public JsonStateStore(ILogger<JsonStateStore> logger, IConfiguration configuration)
    {
        this.logger = logger;
        basePath = configuration[Globals.BasePathKey] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Globals.ApplicationName);

        Directory.CreateDirectory(basePath);
    }

    private string PathFor(string key) => Path.Combine(basePath, $"{key}.json");

    public async Task<T?> LoadAsync<T>(string key, CancellationToken ct = default)
    {
        using var _ = await _lock.AcquireAsync(key, ct).ConfigureAwait(false);

        var path = PathFor(key);
        if (!File.Exists(path)) return default;

        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        var state = await JsonSerializer.DeserializeAsync<T>(fs, options: jsonOpts, cancellationToken: ct).ConfigureAwait(false);

        logger.LogDebug("Loaded state {Type} from {Path}", typeof(T).Name, path);
        return state;
    }

    public async Task SaveAsync<T>(string key, T state, CancellationToken ct = default)
    {
        if (state == null)
        {
            logger.LogWarning("Tried to save null state for key {Key}, skipping", key);
            return;
        }

        using var _ = await _lock.AcquireAsync(key, ct).ConfigureAwait(false);

        var path = PathFor(key);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
        {
            await JsonSerializer.SerializeAsync(fs, state, options: jsonOpts, cancellationToken: ct).ConfigureAwait(false);
            await fs.FlushAsync(ct).ConfigureAwait(false);
        }

        logger.LogDebug("Saved state {Type} to {Path}", typeof(T).Name, path);
    }
}