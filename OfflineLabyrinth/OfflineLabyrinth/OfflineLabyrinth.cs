using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OfflineLabyrinth;

public class OfflineLabyrinth : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly LagStream _clientStream;
    private readonly Task _serverTask;

    public OfflineLabyrinth(int lagMilliseconds)
    {
        // channels for communication
        var clientIn = new SimpleChannel<byte>();
        var clientOutQueue = new SimpleChannel<byte[]>();
        var serverIn = new SimpleChannel<byte>();
        var serverOutQueue = new SimpleChannel<byte[]>();

        // start lag processing tasks
        _ = ProcessOutgoing(clientOutQueue.ReaderEnd, serverIn.WriterEnd, lagMilliseconds, _cts.Token);
        _ = ProcessOutgoing(serverOutQueue.ReaderEnd, clientIn.WriterEnd, lagMilliseconds, _cts.Token);

        _clientStream = new LagStream(clientIn.ReaderEnd, clientOutQueue.WriterEnd, _cts.Token);
        var serverStream = new LagStream(serverIn.ReaderEnd, serverOutQueue.WriterEnd, _cts.Token);

        // run server session
        _serverTask = Task.Run(() => Session.Run(serverStream, _cts.Token));
    }

    public Stream Stream => _clientStream;

    private static async Task ProcessOutgoing(SimpleChannel<byte[]>.Reader source, SimpleChannel<byte>.Writer target, int lag, CancellationToken token)
    {
        await foreach (var msg in source.ReadAllAsync(token))
        {
            await Task.Delay(lag, token);
            foreach (var b in msg)
                await target.WriteAsync(b, token);
        }
        target.Complete();
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _serverTask.Wait(); } catch { }
    }
}

internal class LagStream : Stream
{
    private readonly SimpleChannel<byte>.Reader _reader;
    private readonly SimpleChannel<byte[]>.Writer _writer;
    private readonly CancellationToken _token;

    public LagStream(SimpleChannel<byte>.Reader reader, SimpleChannel<byte[]>.Writer writer, CancellationToken token)
    {
        _reader = reader;
        _writer = writer;
        _token = token;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer, offset, count, _token).GetAwaiter().GetResult();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count, _token).GetAwaiter().GetResult();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int i = 0;
        while (i < buffer.Length)
        {
            var b = await _reader.ReadAsync(_token);
            buffer.Span[i++] = b;
            if (_reader.ReaderCount == 0) break;
        }
        return i;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var memory = new Memory<byte>(buffer, offset, count);
        return await ReadAsync(memory, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var data = buffer.ToArray();
        await _writer.WriteAsync(data, _token);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
    }
}

internal class SimpleChannel<T>
{
    private readonly Queue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private bool _completed;
    private readonly Reader _readerEnd;
    private readonly Writer _writerEnd;

    public Reader ReaderEnd { get { return _readerEnd; } }
    public Writer WriterEnd { get { return _writerEnd; } }

    public SimpleChannel()
    {
        _readerEnd = new Reader(this);
        _writerEnd = new Writer(this);
    }

    public class Reader
    {
        private readonly SimpleChannel<T> _parent;
        internal Reader(SimpleChannel<T> parent) { _parent = parent; }

        public int ReaderCount { get { lock (_parent._queue) return _parent._queue.Count; } }

        public async ValueTask<T> ReadAsync(CancellationToken token = default)
        {
            await _parent._signal.WaitAsync(token);
            lock (_parent._queue)
            {
                if (_parent._queue.Count > 0)
                    return _parent._queue.Dequeue();
                if (_parent._completed)
                    throw new InvalidOperationException();
            }
            return await ReadAsync(token);
        }

        public async IAsyncEnumerable<T> ReadAllAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                T item;
                try
                {
                    item = await ReadAsync(token);
                }
                catch (InvalidOperationException)
                {
                    yield break;
                }
                yield return item;
            }
        }
    }

    public class Writer
    {
        private readonly SimpleChannel<T> _parent;
        internal Writer(SimpleChannel<T> parent) { _parent = parent; }

        public ValueTask WriteAsync(T item, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            lock (_parent._queue)
                _parent._queue.Enqueue(item);
            _parent._signal.Release();
            return ValueTask.CompletedTask;
        }

        public void Complete()
        {
            _parent._completed = true;
            _parent._signal.Release();
        }
    }
}

