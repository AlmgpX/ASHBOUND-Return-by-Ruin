using MediaRelic.Domain;
using MediaRelic.Infra;
using MediaRelic.Playlist;

namespace MediaRelic.App;

public sealed class RelicApp : IAsyncDisposable
{
    private static readonly string[] MediaExtensions =
    {
        ".wav", ".mp3", ".ogg", ".flac", ".m4a", ".aac",
        ".mp4", ".mkv", ".webm", ".avi", ".mov", ".wmv"
    };

    private readonly RelicLogger _logger;
    private readonly string? _mpvPath;
    private readonly string? _ffmpegPath;
    private readonly string? _ffprobePath;

    private MpvController? _mpv;
    private FfmpegService? _ffmpeg;
    private UnicodeFrameSampler? _sampler;

    private bool _isPolling;
    private bool _isSampling;
    private bool _autoAdvancing;

    public RelicState State { get; } = new();
    public RelicConfig Config { get; }

    public RelicApp()
    {
        Config = RelicConfig.Load();
        _logger = new RelicLogger();

        State.Preview = GlyphFrame.Empty(Config.PreviewMaxWidth, Config.PreviewMaxHeight);

        _mpvPath = ToolLocator.Find("mpv.exe");
        _ffmpegPath = ToolLocator.Find("ffmpeg.exe");
        _ffprobePath = ToolLocator.Find("ffprobe.exe");

        _logger.Info($"Tool paths: mpv={_mpvPath ?? "missing"}; ffmpeg={_ffmpegPath ?? "missing"}; ffprobe={_ffprobePath ?? "missing"}");

        if (_mpvPath is null || _ffmpegPath is null)
        {
            SetError("ERR: PUT mpv.exe + ffmpeg.exe INTO /tools OR PATH");
            return;
        }

        _mpv = new MpvController(_mpvPath);
        _ffmpeg = new FfmpegService(_ffmpegPath, _ffprobePath);
        _sampler = new UnicodeFrameSampler(_ffmpegPath, _ffprobePath);

        State.Mode = RelicMode.Ready;
        State.Status = "READY. DROP MEDIA, PRESS O, OR PRESS P FOR FOLDER";
    }

    public async Task OpenFileAsync(string path)
    {
        State.Playlist.Clear();
        State.PlaylistIndex = -1;
        await LoadMediaAsync(path);
    }

    public async Task OpenFolderAsync(string folderPath)
    {
        if (_ffmpeg is null)
        {
            SetError("ERR: ffmpeg/ffprobe missing");
            return;
        }

        try
        {
            State.Mode = RelicMode.Loading;
            State.Status = "SCANNING FOLDER, FILTERING TRASH < 15s...";

            var scanner = new PlaylistScanner(_ffmpeg, Config.MinPlaylistDurationSeconds, MediaExtensions);

            using var cts = LongCts();
            var scan = await scanner.ScanFolderAsync(folderPath, cts.Token);

            State.Playlist = scan.Items
                .Where(item => item.IsValid)
                .Select(item => item.Path)
                .ToList();

            State.PlaylistIndex = State.Playlist.Count > 0 ? 0 : -1;

            _logger.Info($"Folder scan: {folderPath}; accepted={scan.AcceptedCount}; short={scan.SkippedShortCount}; broken={scan.SkippedBrokenCount}");

            if (State.Playlist.Count == 0)
            {
                State.Mode = RelicMode.Ready;
                State.Status = $"NO PLAYABLE FILES. SKIPPED SHORT {scan.SkippedShortCount}, BROKEN {scan.SkippedBrokenCount}";
                return;
            }

            await LoadMediaAsync(State.Playlist[0]);
            State.Status = $"FOLDER READY: {State.Playlist.Count} TRACKS, SKIPPED <15s {scan.SkippedShortCount}, BROKEN {scan.SkippedBrokenCount}";
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task ApplyCoverAsync(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(State.MediaPath))
        {
            State.Status = "NO TRACK LOADED";
            return;
        }

        try
        {
            var coverPath = CoverService.ApplySidecarCover(State.MediaPath, imagePath);

            _sampler?.InvalidateForPath(State.MediaPath);
            State.Preview = GlyphFrame.Empty(Config.PreviewMaxWidth, Config.PreviewMaxHeight);
            State.Status = "COVER APPLIED: " + Path.GetFileName(coverPath);

            _logger.Info($"Cover applied: {coverPath}");

            await UpdatePreviewAsync();
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task TogglePauseAsync()
    {
        if (_mpv is null || State.MediaPath is null)
            return;

        try
        {
            using var cts = ShortCts();
            await _mpv.TogglePauseAsync(cts.Token);
            await PollAsync();
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task SeekAsync(double seconds)
    {
        if (_mpv is null || State.MediaPath is null)
            return;

        try
        {
            using var cts = ShortCts();
            await _mpv.SeekRelativeAsync(seconds, cts.Token);
            await PollAsync();
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task SetSpeedAsync(double speed)
    {
        if (_mpv is null)
            return;

        speed = Math.Clamp(speed, 0.25, 4.0);

        try
        {
            using var cts = ShortCts();
            await _mpv.SetSpeedAsync(speed, cts.Token);
            State.Speed = speed;
            State.Status = $"SPEED {speed:0.00}x";
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task SetVolumeAsync(double volume)
    {
        if (_mpv is null)
            return;

        volume = Math.Clamp(volume, 0.0, 130.0);

        try
        {
            using var cts = ShortCts();
            await _mpv.SetVolumeAsync(volume, cts.Token);
            State.Volume = volume;
            State.Status = $"VOLUME {volume:0}%";
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task ToggleLoopAsync()
    {
        if (_mpv is null)
            return;

        try
        {
            State.IsLooping = !State.IsLooping;

            using var cts = ShortCts();
            await _mpv.SetLoopAsync(State.IsLooping, cts.Token);

            State.Status = State.IsLooping ? "LOOP: ∞" : "LOOP: OFF";
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task ToggleReverbAsync()
    {
        if (_mpv is null)
            return;

        try
        {
            State.IsReverbEnabled = !State.IsReverbEnabled;

            using var cts = ShortCts();
            await _mpv.SetReverbAsync(State.IsReverbEnabled, cts.Token);

            State.Status = State.IsReverbEnabled ? "REVERB: ON" : "REVERB: OFF";
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task ScanSilenceAsync()
    {
        if (_ffmpeg is null || State.MediaPath is null)
            return;

        try
        {
            State.Mode = RelicMode.ScanningSilence;
            State.Status = "SCANNING SILENCE...";

            using var cts = LongCts();

            var duration = State.Duration > 0.01
                ? State.Duration
                : await _ffmpeg.ProbeDurationAsync(State.MediaPath, cts.Token);

            State.Duration = duration;

            var silences = await _ffmpeg.DetectSilenceAsync(
                State.MediaPath,
                noiseDb: Config.SilenceNoiseDb,
                minDuration: Config.SilenceMinDurationSeconds,
                cts.Token);

            State.SoundRanges = SegmentBuilder.BuildSoundRanges(
                silences,
                duration,
                Config.MinExportSegmentDurationSeconds);

            State.Status = $"SILENCE SCAN DONE: {silences.Count} SILENCES, {State.SoundRanges.Count} CUTS";
            State.Mode = State.IsPaused ? RelicMode.Paused : RelicMode.Playing;

            _logger.Info($"Silence scan: silences={silences.Count}; cuts={State.SoundRanges.Count}; media={State.MediaPath}");
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task ExportCutsAsync()
    {
        if (_ffmpeg is null || State.MediaPath is null)
            return;

        try
        {
            if (State.SoundRanges.Count == 0)
                await ScanSilenceAsync();

            if (State.SoundRanges.Count == 0)
            {
                State.Status = "NO CUTS TO EXPORT";
                return;
            }

            State.Mode = RelicMode.Exporting;

            var dir = Path.Combine(
                Path.GetDirectoryName(State.MediaPath) ?? Environment.CurrentDirectory,
                "MediaRelic_Cuts",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            var progress = new Progress<string>(message => State.Status = message);

            using var cts = LongCts();

            await _ffmpeg.ExportSegmentsAsync(
                State.MediaPath,
                State.SoundRanges,
                dir,
                progress,
                cts.Token);

            State.Status = "EXPORT DONE: " + dir;
            State.Mode = State.IsPaused ? RelicMode.Paused : RelicMode.Playing;

            _logger.Info($"Export done: {dir}");
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    public async Task PlayRelativeAsync(int offset)
    {
        if (!State.HasPlaylist)
        {
            State.Status = "NO PLAYLIST";
            return;
        }

        var count = State.Playlist.Count;
        var next = (State.PlaylistIndex + offset) % count;

        if (next < 0)
            next += count;

        State.PlaylistIndex = next;
        await LoadMediaAsync(State.Playlist[next]);
    }

    public async Task PollAsync()
    {
        if (_isPolling || _mpv is null || State.MediaPath is null)
            return;

        _isPolling = true;

        try
        {
            using var cts = ShortCts();

            State.Position = await _mpv.GetDoublePropertyAsync("time-pos", cts.Token);

            var duration = await _mpv.GetDoublePropertyAsync("duration", cts.Token);
            if (duration > 0.01)
                State.Duration = duration;

            State.IsPaused = await _mpv.GetBoolPropertyAsync("pause", cts.Token);
            State.Mode = State.IsPaused ? RelicMode.Paused : RelicMode.Playing;

            await MaybeAutoAdvanceAsync();
        }
        catch
        {
            // One failed poll should not collapse the UI. Tiny mercy in a stupid universe.
        }
        finally
        {
            _isPolling = false;
        }
    }

    public async Task UpdatePreviewAsync()
    {
        if (_isSampling || _sampler is null || State.MediaPath is null)
            return;

        _isSampling = true;

        try
        {
            using var cts = LongCts();
            State.Preview = await _sampler.SampleAsync(
                State.MediaPath,
                State.Position,
                Config.PreviewMaxWidth,
                Config.PreviewMaxHeight,
                cts.Token);
        }
        catch (Exception ex)
        {
            _logger.Error("Preview failed", ex);
        }
        finally
        {
            _isSampling = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_mpv is not null)
            await _mpv.DisposeAsync();
    }

    private async Task LoadMediaAsync(string path)
    {
        if (_mpv is null || _ffmpeg is null)
        {
            SetError("ERR: mpv/ffmpeg missing");
            return;
        }

        try
        {
            State.Mode = RelicMode.Loading;
            State.MediaPath = path;
            State.Position = 0;
            State.Duration = 0;
            State.Speed = 1.0;
            State.Volume = Config.DefaultVolume;
            State.IsLooping = false;
            State.IsReverbEnabled = false;
            State.SoundRanges.Clear();
            State.Preview = GlyphFrame.Empty(Config.PreviewMaxWidth, Config.PreviewMaxHeight);
            State.Status = "LOADING " + Path.GetFileName(path);

            using var cts = ShortCts();

            await _mpv.EnsureStartedAsync(cts.Token);
            await _mpv.LoadFileAsync(path, cts.Token);

            if (Math.Abs(Config.DefaultVolume - 100.0) > 0.01)
                await _mpv.SetVolumeAsync(Config.DefaultVolume, cts.Token);

            State.Duration = await _ffmpeg.ProbeDurationAsync(path, cts.Token);
            State.Mode = RelicMode.Playing;
            State.Status = "LOADED";

            _logger.Info($"Loaded media: {path}; duration={State.Duration:0.000}");
        }
        catch (Exception ex)
        {
            SetError("ERR: " + ex.Message, ex);
        }
    }

    private async Task MaybeAutoAdvanceAsync()
    {
        if (_autoAdvancing || State.IsLooping || !State.HasPlaylist)
            return;

        if (State.Duration < Config.MinPlaylistDurationSeconds)
            return;

        if (State.Position < State.Duration - 0.35)
            return;

        _autoAdvancing = true;

        try
        {
            await PlayRelativeAsync(+1);
        }
        finally
        {
            _autoAdvancing = false;
        }
    }

    private void SetError(string message, Exception? exception = null)
    {
        State.Mode = RelicMode.Error;
        State.Status = message;
        _logger.Error(message, exception);
    }

    private static CancellationTokenSource ShortCts()
    {
        return new CancellationTokenSource(TimeSpan.FromSeconds(4));
    }

    private static CancellationTokenSource LongCts()
    {
        return new CancellationTokenSource(TimeSpan.FromMinutes(10));
    }
}
