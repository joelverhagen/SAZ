namespace Knapcode.SAZ;

/// <summary>
/// A stream wrapper that gates every read (sync and async) with a provided <see cref="SemaphoreSlim"/>.
/// Writing and seeking are not supported.
/// </summary>
public class GatedReadStream : Stream
{
    private readonly Stream _inner;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public GatedReadStream(Stream inner, SemaphoreSlim semaphore)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
        if (!inner.CanRead)
        {
            throw new ArgumentException("Inner stream must be readable.", nameof(inner));
        }
    }

    public override bool CanRead => !_disposed && _inner.CanRead;
    public override bool CanSeek => false; // Explicitly unsupported
    public override bool CanWrite => false; // Explicitly unsupported
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        _semaphore.Wait();
        try
        {
            return _inner.Read(buffer, offset, count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        _semaphore.Wait();
        try
        {
            return _inner.Read(buffer);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override int ReadByte()
    {
        ThrowIfDisposed();
        _semaphore.Wait();
        try
        {
            return _inner.ReadByte();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotImplementedException();
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _inner.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
