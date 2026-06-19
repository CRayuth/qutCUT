namespace qutCUT.Utilities;

// Async semaphore — mirrors the Swift version exactly
public sealed class AsyncSemaphore(int initialCount = 1)
{
    private readonly SemaphoreSlim _inner = new(initialCount, int.MaxValue);

    public Task WaitAsync(CancellationToken ct = default) => _inner.WaitAsync(ct);

    public void Release() => _inner.Release();

    public async Task<T> WithLock<T>(Func<Task<T>> fn, CancellationToken ct = default)
    {
        await _inner.WaitAsync(ct);
        try { return await fn(); }
        finally { _inner.Release(); }
    }

    public async Task WithLock(Func<Task> fn, CancellationToken ct = default)
    {
        await _inner.WaitAsync(ct);
        try { await fn(); }
        finally { _inner.Release(); }
    }
}
