using System.Collections.Concurrent;
using System.Reactive.Disposables;

namespace Mapping_Tools.Infrastructure;

public sealed class KeyedAsyncLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(string key, CancellationToken ct)
    {
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct).ConfigureAwait(false);
        return Disposable.Create(sem, s => s.Release());
    }
}