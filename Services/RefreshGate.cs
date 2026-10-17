using System;
using System.Threading;
using System.Threading.Tasks;

namespace NowPlaying.Services;

internal sealed class RefreshGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _disposed;

    public async Task<bool> WaitAsync(TimeSpan timeout)
    {
        if (Volatile.Read(ref _disposed) != 0 || !await _semaphore.WaitAsync(timeout))
        {
            return false;
        }

        if (Volatile.Read(ref _disposed) == 0)
        {
            return true;
        }

        _semaphore.Release();
        return false;
    }

    public void Release() => _semaphore.Release();

    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
}