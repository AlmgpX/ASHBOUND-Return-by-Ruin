using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Text.Json;

namespace MediaRelic.Infra;

public sealed class MpvController : IAsyncDisposable
{
    private readonly string _mpvPath;
    private readonly string _pipeName;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();

    private Process? _process;
    private NamedPipeClientStream? _pipe;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private CancellationTokenSource? _readCts;
    private int _nextRequestId;

    public MpvController(string mpvPath)
    {
        _mpvPath = mpvPath;
        _pipeName = "media_relic_" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false } && _pipe is { IsConnected: true })
            return;

        await StopAsync();

        var start = new ProcessStartInfo
        {
            FileName = _mpvPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        start.ArgumentList.Add("--idle=yes");
        start.ArgumentList.Add("--no-config");
        start.ArgumentList.Add("--terminal=no");
        start.ArgumentList.Add("--force-window=no");
        start.ArgumentList.Add("--vid=no");
        start.ArgumentList.Add("--audio-display=no");
        start.ArgumentList.Add("--input-ipc-server=\\\\.\\pipe\\" + _pipeName);

        _process = Process.Start(start) ?? throw new InvalidOperationException("mpv did not start.");

        await ConnectPipeAsync(cancellationToken);

        _readCts = new CancellationTokenSource();
        _ = Task.Run(() => ReadLoopAsync(_readCts.Token), CancellationToken.None);
    }

    public async Task LoadFileAsync(string path, CancellationToken cancellationToken)
    {
        await CommandAsync(cancellationToken, "loadfile", path, "replace");
        await SetVolumeAsync(100.0, cancellationToken);
        await SetSpeedAsync(1.0, cancellationToken);
        await SetLoopAsync(false, cancellationToken);
        await SetReverbAsync(false, cancellationToken);
    }

    public Task TogglePauseAsync(CancellationToken cancellationToken)
    {
        return CommandAsync(cancellationToken, "cycle", "pause");
    }

    public Task SetPauseAsync(bool pause, CancellationToken cancellationToken)
    {
        return CommandAsync(cancellationToken, "set_property", "pause", pause);
    }

    public Task SeekRelativeAsync(double seconds, CancellationToken cancellationToken)
    {
        return CommandAsync(cancellationToken, "seek", seconds, "relative", "exact");
    }

    public Task SetSpeedAsync(double speed, CancellationToken cancellationToken)
    {
        speed = Math.Clamp(speed, 0.25, 4.0);
        return CommandAsync(cancellationToken, "set_property", "speed", speed);
    }

    public Task SetVolumeAsync(double volume, CancellationToken cancellationToken)
    {
        volume = Math.Clamp(volume, 0.0, 130.0);
        return CommandAsync(cancellationToken, "set_property", "volume", volume);
    }

    public Task SetLoopAsync(bool enabled, CancellationToken cancellationToken)
    {
        return CommandAsync(cancellationToken, "set_property", "loop-file", enabled ? "inf" : "no");
    }

    public Task SetReverbAsync(bool enabled, CancellationToken cancellationToken)
    {
        if (!enabled)
            return CommandAsync(cancellationToken, "af", "clr", "");

        const string filter = "@relic_reverb:lavfi=[aecho=0.82:0.88:70|140:0.35|0.19]";
        return CommandAsync(cancellationToken, "af", "set", filter);
    }

    public async Task<double> GetDoublePropertyAsync(string name, CancellationToken cancellationToken)
    {
        var response = await CommandWithResponseAsync(cancellationToken, "get_property", name);

        if (!response.TryGetProperty("data", out var data))
            return 0.0;

        return data.ValueKind switch
        {
            JsonValueKind.Number when data.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(
                data.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value) => value,
            _ => 0.0
        };
    }

    public async Task<bool> GetBoolPropertyAsync(string name, CancellationToken cancellationToken)
    {
        var response = await CommandWithResponseAsync(cancellationToken, "get_property", name);

        if (!response.TryGetProperty("data", out var data))
            return false;

        return data.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => string.Equals(data.GetString(), "yes", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(data.GetString(), "true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private async Task ConnectPipeAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _pipe = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                await _pipe.ConnectAsync(250, cancellationToken);

                _writer = new StreamWriter(_pipe) { AutoFlush = true };
                _reader = new StreamReader(_pipe);

                return;
            }
            catch
            {
                _pipe?.Dispose();
                _pipe = null;
                await Task.Delay(80, cancellationToken);
            }
        }

        throw new TimeoutException("Could not connect to mpv IPC pipe.");
    }

    private async Task CommandAsync(CancellationToken cancellationToken, params object[] command)
    {
        await CommandWithResponseAsync(cancellationToken, command);
    }

    private async Task<JsonElement> CommandWithResponseAsync(CancellationToken cancellationToken, params object[] command)
    {
        if (_writer is null)
            throw new InvalidOperationException("mpv pipe is not connected.");

        var id = Interlocked.Increment(ref _nextRequestId);
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pending[id] = tcs;

        var packet = JsonSerializer.Serialize(new
        {
            command,
            request_id = id
        });

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(packet.AsMemory(), cancellationToken);
            await _writer.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }

        try
        {
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        if (_reader is null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;

            try
            {
                line = await _reader.ReadLineAsync(cancellationToken);
            }
            catch
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (!root.TryGetProperty("request_id", out var idElement))
                    continue;

                var id = idElement.GetInt32();

                if (_pending.TryRemove(id, out var tcs))
                    tcs.TrySetResult(root.Clone());
            }
            catch
            {
                // mpv can send events too. We do not make a religion out of every line.
            }
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _readCts?.Cancel();
        }
        catch
        {
        }

        _readCts?.Dispose();
        _readCts = null;

        _reader?.Dispose();
        _reader = null;

        _writer?.Dispose();
        _writer = null;

        _pipe?.Dispose();
        _pipe = null;

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch
            {
            }
        }

        _process?.Dispose();
        _process = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _writeLock.Dispose();
    }
}
