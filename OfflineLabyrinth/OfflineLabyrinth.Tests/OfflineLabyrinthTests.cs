using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class OfflineLabyrinthTests
{
    [Fact]
    public async Task PrintCommandProducesOutput()
    {
        using var lab = new OfflineLabyrinth.OfflineLabyrinth(0);
        using var writer = new StreamWriter(lab.Stream) { NewLine = "\r\n", AutoFlush = true };
        using var reader = new StreamReader(lab.Stream);

        // read the welcome text
        for (int i = 0; i < 20; i++)
        {
            var line = await reader.ReadLineAsync();
            if (line == null || line.StartsWith("9 ")) break;
        }

        await writer.WriteLineAsync("WIDTH 32");
        var response = await reader.ReadLineAsync();
        Assert.StartsWith("2", response);

        await writer.WriteLineAsync("HEIGHT 32");
        response = await reader.ReadLineAsync();
        Assert.StartsWith("2", response);

        await writer.WriteLineAsync("DEPTH 1");
        response = await reader.ReadLineAsync();
        Assert.StartsWith("2", response);

        await writer.WriteLineAsync("START");
        // read progress until READY
        string? lineReady;
        do
        {
            lineReady = await reader.ReadLineAsync();
        } while (lineReady != null && !lineReady.StartsWith("2 READY."));

        Assert.Equal("2 READY.", lineReady);

        await writer.WriteLineAsync("PRINT");
        var printLine = await reader.ReadLineAsync();
        Assert.True(printLine!.StartsWith("3 ")); // position line
    }

    [Fact]
    public async Task LagIsEnforced()
    {
        const int lag = 50;
        using var lab = new OfflineLabyrinth.OfflineLabyrinth(lag);
        using var writer = new StreamWriter(lab.Stream) { NewLine = "\r\n", AutoFlush = true };
        using var reader = new StreamReader(lab.Stream);

        // skip intro
        for (int i = 0; i < 20; i++)
        {
            var line = await reader.ReadLineAsync();
            if (line == null || line.StartsWith("9 ")) break;
        }

        var sw = Stopwatch.StartNew();
        await writer.WriteLineAsync("WIDTH 32");
        await reader.ReadLineAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= lag, $"Expected at least {lag}ms lag but got {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task TooLargeLabyrinthIsRejected()
    {
        using OfflineLabyrinth.OfflineLabyrinth lab = new OfflineLabyrinth.OfflineLabyrinth(0);
        using StreamWriter writer = new StreamWriter(lab.Stream) { NewLine = "\r\n", AutoFlush = true };
        using StreamReader reader = new StreamReader(lab.Stream);

        for (int i = 0; i < 20; i++)
        {
            string? skipLine = await reader.ReadLineAsync();
            if (skipLine == null || skipLine.StartsWith("9 ")) break;
        }

        await writer.WriteLineAsync("WIDTH 4096");
        string? response = await reader.ReadLineAsync();
        Assert.StartsWith("2", response);

        await writer.WriteLineAsync("HEIGHT 4096");
        response = await reader.ReadLineAsync();
        Assert.StartsWith("2", response);

        await writer.WriteLineAsync("DEPTH 2");
        response = await reader.ReadLineAsync();
        Assert.StartsWith("2", response);

        await writer.WriteLineAsync("START");
        await writer.WriteLineAsync("WIDTH 32");
        response = await reader.ReadLineAsync();
        Assert.Equal("5 The labyrinth you have chosen requires 32MB of RAM. (Max=16MB.)", response);
        await reader.ReadLineAsync(); // consume response to WIDTH 32
    }
}
