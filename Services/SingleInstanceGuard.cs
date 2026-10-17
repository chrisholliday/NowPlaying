using System.Threading;

namespace NowPlaying.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    public bool IsOwner { get; }

    public SingleInstanceGuard(string name)
    {
        _mutex = new Mutex(initiallyOwned: true, name, out bool createdNew);
        IsOwner = createdNew;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (IsOwner)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}