using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OfflineLabyrinth;

public class OfflineLabyrinth : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly LagStream _clientStream;
    private readonly Task _serverTask;

    public OfflineLabyrinth(int lagMilliseconds)
    {
        // channels for communication
        var clientIn = Channel.CreateUnbounded<byte>();
        var clientOutQueue = Channel.CreateUnbounded<byte[]>();
        var serverIn = Channel.CreateUnbounded<byte>();
        var serverOutQueue = Channel.CreateUnbounded<byte[]>();

        // start lag processing tasks
        _ = ProcessOutgoing(clientOutQueue.Reader, serverIn.Writer, lagMilliseconds, _cts.Token);
        _ = ProcessOutgoing(serverOutQueue.Reader, clientIn.Writer, lagMilliseconds, _cts.Token);

        _clientStream = new LagStream(clientIn.Reader, clientOutQueue.Writer, _cts.Token);
        var serverStream = new LagStream(serverIn.Reader, serverOutQueue.Writer, _cts.Token);

        // run server session
        _serverTask = Task.Run(() => Session.Run(serverStream, _cts.Token));
    }

    public Stream Stream => _clientStream;

    private static async Task ProcessOutgoing(ChannelReader<byte[]> source, ChannelWriter<byte> target, int lag, CancellationToken token)
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
    private readonly ChannelReader<byte> _reader;
    private readonly ChannelWriter<byte[]> _writer;
    private readonly CancellationToken _token;

    public LagStream(ChannelReader<byte> reader, ChannelWriter<byte[]> writer, CancellationToken token)
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

