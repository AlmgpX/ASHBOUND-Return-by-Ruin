using MediaRelic.Domain;
using MediaRelic.Infra;

namespace MediaRelic.Ui;

public sealed class MainForm : Form
{
    private const int PreviewWidth = 96;
    private const int PreviewHeight = 36;
    private const double MinimumPlaylistDurationSeconds = 15.0;

    private static readonly string[] MediaExtensions =
    {
        ".wav", ".mp3", ".ogg", ".flac", ".m4a", ".aac",
        ".mp4", ".mkv", ".webm", ".avi", ".mov", ".wmv"
    };

    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    private readonly RelicState _state = new();
    private readonly RelicCanvas _canvas = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new();
    private readonly System.Windows.Forms.Timer _previewTimer = new();

    private readonly string? _mpvPath;
    private readonly string? _ffmpegPath;
    private readonly string? _ffprobePath;

    private MpvController? _mpv;
    private FfmpegService? _ffmpeg;
    private UnicodeFrameSampler? _sampler;

    private bool _isPolling;
    private bool _isSampling;
    private bool _stopPlaybackOnClose = true;
    private bool _autoAdvancing;

    public MainForm()
    {
        Text = "MEDIA RELIC v0.1";
        Width = 980;
        Height = 620;
        MinimumSize = new Size(720, 460);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        Opacity = 0.94;
        KeyPreview = true;
        AllowDrop = true;
        BackColor = Color.Black;

        _canvas.Dock = DockStyle.Fill;
        _canvas.State = _state;
        Controls.Add(_canvas);

        _mpvPath = ToolLocator.Find("mpv.exe");
        _ffmpegPath = ToolLocator.Find("ffmpeg.exe");
        _ffprobePath = ToolLocator.Find("ffprobe.exe");

        if (_mpvPath is null || _ffmpegPath is null)
        {
            _state.Status = "ERR: PUT mpv.exe + ffmpeg.exe INTO /tools OR PATH";
        }
        else
        {
            _mpv = new MpvController(_mpvPath);
            _ffmpeg = new FfmpegService(_ffmpegPath, _ffprobePath);
            _sampler = new UnicodeFrameSampler(_ffmpegPath, _ffprobePath);
            _state.Status = "READY. DROP MEDIA, PRESS O, OR PRESS P FOR FOLDER";
        }

        _uiTimer.Interval = 250;
        _uiTimer.Tick += async (_, _) => await PollMpvAsync();
        _uiTimer.Start();

        _previewTimer.Interval = 850;
        _previewTimer.Tick += async (_, _) => await UpdatePreviewAsync();
        _previewTimer.Start();

        DragEnter += OnDragEnter;
        DragDrop += async (_, e) => await OnDragDropAsync(e);
        KeyDown += async (_, e) => await OnKeyDownAsync(e);

        _canvas.MouseDown += OnCanvasMouseDown;
        _canvas.MouseDoubleClick += (_, _) => ToggleTopMost();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg != WmNcHitTest || (int)m.Result != HtClient)
            return;

        var point = PointToClient(new Point(
            unchecked((short)(long)m.LParam),
            unchecked((short)((long)m.LParam >> 16))));

        var grip = Math.Max(8, (int)Math.Round(8 * DeviceDpi / 96.0));

        var left = point.X <= grip;
        var right = point.X >= ClientSize.Width - grip;
        var top = point.Y <= grip;
        var bottom = point.Y >= ClientSize.Height - grip;

        if (left && top) m.Result = HtTopLeft;
        else if (right && top) m.Result = HtTopRight;
        else if (left && bottom) m.Result = HtBottomLeft;
        else if (right && bottom) m.Result = HtBottomRight;
        else if (left) m.Result = HtLeft;
        else if (right) m.Result = HtRight;
        else if (top) m.Result = HtTop;
        else if (bottom) m.Result = HtBottom;
    }

    private async Task OnKeyDownAsync(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            _stopPlaybackOnClose = true;
            Close();
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Q:
                _stopPlaybackOnClose = true;
                Close();
                return;

            case Keys.M:
                WindowState = FormWindowState.Minimized;
                return;

            case Keys.O:
            case Keys.F:
                await OpenDialogAsync();
                break;

            case Keys.P:
                await OpenFolderDialogAsync();
                break;

            case Keys.C:
                await ApplyCoverDialogAsync();
                break;

            case Keys.N:
                await PlayRelativeAsync(+1);
                break;

            case Keys.B:
                await PlayRelativeAsync(-1);
                break;

            case Keys.Space:
                await TogglePauseAsync();
                break;

            case Keys.Left:
                await SeekAsync(-5.0);
                break;

            case Keys.Right:
                await SeekAsync(5.0);
                break;

            case Keys.Up:
                await SetVolumeAsync(_state.Volume + 5.0);
                break;

            case Keys.Down:
                await SetVolumeAsync(_state.Volume - 5.0);
                break;

            case Keys.OemOpenBrackets:
                await SetSpeedAsync(_state.Speed - 0.1);
                break;

            case Keys.OemCloseBrackets:
                await SetSpeedAsync(_state.Speed + 0.1);
                break;

            case Keys.Back:
                await SetSpeedAsync(1.0);
                break;

            case Keys.L:
                await ToggleLoopAsync();
                break;

            case Keys.R:
                await ToggleReverbAsync();
                break;

            case Keys.S:
                await ScanSilenceAsync();
                break;

            case Keys.X:
                await ExportCutsAsync();
                break;

            case Keys.T:
                ToggleTopMost();
                break;
        }

        _canvas.Invalidate();
    }

    private void OnCanvasMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        var command = _canvas.HitTestWindowCommand(e.Location);

        if (command == RelicWindowCommand.Minimize)
        {
            WindowState = FormWindowState.Minimized;
            return;
        }

        if (command == RelicWindowCommand.CloseKeepPlaying)
        {
            _stopPlaybackOnClose = true;
            Close();
            return;
        }

        if (_canvas.IsDragZone(e.Location))
            NativeDrag.MoveWindow(Handle);
    }

    private async Task OpenDialogAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open media",
            Filter = "Media|*.wav;*.mp3;*.ogg;*.flac;*.m4a;*.aac;*.mp4;*.mkv;*.webm;*.avi;*.mov;*.wmv|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _state.Playlist.Clear();
        _state.PlaylistIndex = -1;

        await LoadMediaAsync(dialog.FileName);
    }

    private async Task OpenFolderDialogAsync()
    {
        if (_ffmpeg is null)
        {
            _state.Status = "ERR: ffmpeg/ffprobe missing";
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "Open folder as MEDIA RELIC playlist"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await LoadFolderAsync(dialog.SelectedPath);
    }


    private async Task ApplyCoverDialogAsync()
    {
        if (string.IsNullOrWhiteSpace(_state.MediaPath))
        {
            _state.Status = "NO TRACK LOADED";
            _canvas.Invalidate();
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Apply cover image to current track",
            Filter = "Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var coverPath = CoverService.ApplySidecarCover(_state.MediaPath, dialog.FileName);

            _sampler?.InvalidateForPath(_state.MediaPath);
            _state.Preview = GlyphFrame.Empty(PreviewWidth, PreviewHeight);
            _state.Status = "COVER APPLIED: " + Path.GetFileName(coverPath);

            await UpdatePreviewAsync();
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
        finally
        {
            _canvas.Invalidate();
        }
    }

    private async Task LoadFolderAsync(string folderPath)
    {
        if (_ffmpeg is null)
            return;

        try
        {
            _state.Status = "SCANNING FOLDER, FILTERING TRASH < 15s...";
            _canvas.Invalidate();

            var files = Directory
                .EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(IsSupportedMediaPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var accepted = new List<string>();
            var skippedShort = 0;
            var skippedBroken = 0;

            using var cts = LongCts();

            foreach (var file in files)
            {
                double duration;

                try
                {
                    duration = await _ffmpeg.ProbeDurationAsync(file, cts.Token);
                }
                catch
                {
                    skippedBroken++;
                    continue;
                }

                if (duration >= MinimumPlaylistDurationSeconds)
                    accepted.Add(file);
                else
                    skippedShort++;
            }

            _state.Playlist = accepted;
            _state.PlaylistIndex = accepted.Count > 0 ? 0 : -1;

            if (accepted.Count == 0)
            {
                _state.Status = $"NO PLAYABLE FILES. SKIPPED SHORT {skippedShort}, BROKEN {skippedBroken}";
                _canvas.Invalidate();
                return;
            }

            await LoadMediaAsync(accepted[0]);

            _state.Status = $"FOLDER READY: {accepted.Count} TRACKS, SKIPPED <15s {skippedShort}, BROKEN {skippedBroken}";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
        finally
        {
            _canvas.Invalidate();
        }
    }

    private async Task PlayRelativeAsync(int offset)
    {
        if (!_state.HasPlaylist)
        {
            _state.Status = "NO PLAYLIST";
            return;
        }

        var count = _state.Playlist.Count;
        var next = (_state.PlaylistIndex + offset) % count;

        if (next < 0)
            next += count;

        _state.PlaylistIndex = next;
        await LoadMediaAsync(_state.Playlist[next]);
    }

    private async Task LoadMediaAsync(string path)
    {
        if (_mpv is null || _ffmpeg is null)
        {
            _state.Status = "ERR: mpv/ffmpeg missing";
            return;
        }

        try
        {
            _state.MediaPath = path;
            _state.Position = 0;
            _state.Duration = 0;
            _state.Speed = 1.0;
            _state.Volume = 100.0;
            _state.IsLooping = false;
            _state.IsReverbEnabled = false;
            _state.SoundRanges.Clear();
            _state.Preview = GlyphFrame.Empty(PreviewWidth, PreviewHeight);
            _state.Status = "LOADING " + Path.GetFileName(path);

            using var cts = ShortCts();

            await _mpv.EnsureStartedAsync(cts.Token);
            await _mpv.LoadFileAsync(path, cts.Token);

            _state.Duration = await _ffmpeg.ProbeDurationAsync(path, cts.Token);

            _state.Status = "LOADED";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
        finally
        {
            _canvas.Invalidate();
        }
    }

    private async Task TogglePauseAsync()
    {
        if (_mpv is null || _state.MediaPath is null)
            return;

        try
        {
            using var cts = ShortCts();
            await _mpv.TogglePauseAsync(cts.Token);
            await PollMpvAsync();
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
    }

    private async Task SeekAsync(double seconds)
    {
        if (_mpv is null || _state.MediaPath is null)
            return;

        try
        {
            using var cts = ShortCts();
            await _mpv.SeekRelativeAsync(seconds, cts.Token);
            await PollMpvAsync();
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
    }

    private async Task SetSpeedAsync(double speed)
    {
        if (_mpv is null)
            return;

        speed = Math.Clamp(speed, 0.25, 4.0);

        try
        {
            using var cts = ShortCts();
            await _mpv.SetSpeedAsync(speed, cts.Token);
            _state.Speed = speed;
            _state.Status = $"SPEED {speed:0.00}x";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
    }

    private async Task SetVolumeAsync(double volume)
    {
        if (_mpv is null)
            return;

        volume = Math.Clamp(volume, 0.0, 130.0);

        try
        {
            using var cts = ShortCts();
            await _mpv.SetVolumeAsync(volume, cts.Token);
            _state.Volume = volume;
            _state.Status = $"VOLUME {volume:0}%";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
    }

    private async Task ToggleLoopAsync()
    {
        if (_mpv is null)
            return;

        try
        {
            _state.IsLooping = !_state.IsLooping;

            using var cts = ShortCts();
            await _mpv.SetLoopAsync(_state.IsLooping, cts.Token);

            _state.Status = _state.IsLooping ? "LOOP: ∞" : "LOOP: OFF";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
    }

    private async Task ToggleReverbAsync()
    {
        if (_mpv is null)
            return;

        try
        {
            _state.IsReverbEnabled = !_state.IsReverbEnabled;

            using var cts = ShortCts();
            await _mpv.SetReverbAsync(_state.IsReverbEnabled, cts.Token);

            _state.Status = _state.IsReverbEnabled ? "REVERB: ON" : "REVERB: OFF";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
    }

    private async Task ScanSilenceAsync()
    {
        if (_ffmpeg is null || _state.MediaPath is null)
            return;

        try
        {
            _state.Status = "SCANNING SILENCE...";
            _canvas.Invalidate();

            using var cts = LongCts();

            var duration = _state.Duration > 0.01
                ? _state.Duration
                : await _ffmpeg.ProbeDurationAsync(_state.MediaPath, cts.Token);

            _state.Duration = duration;

            var silences = await _ffmpeg.DetectSilenceAsync(
                _state.MediaPath,
                noiseDb: -38.0,
                minDuration: 0.35,
                cts.Token);

            _state.SoundRanges = SegmentBuilder.BuildSoundRanges(
                silences,
                duration,
                minSegmentDuration: 0.08);

            _state.Status = $"SILENCE SCAN DONE: {silences.Count} SILENCES, {_state.SoundRanges.Count} CUTS";
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
        finally
        {
            _canvas.Invalidate();
        }
    }

    private async Task ExportCutsAsync()
    {
        if (_ffmpeg is null || _state.MediaPath is null)
            return;

        try
        {
            if (_state.SoundRanges.Count == 0)
                await ScanSilenceAsync();

            if (_state.SoundRanges.Count == 0)
            {
                _state.Status = "NO CUTS TO EXPORT";
                return;
            }

            var dir = Path.Combine(
                Path.GetDirectoryName(_state.MediaPath) ?? Environment.CurrentDirectory,
                "MediaRelic_Cuts",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            var progress = new Progress<string>(message =>
            {
                _state.Status = message;
                _canvas.Invalidate();
            });

            using var cts = LongCts();

            await _ffmpeg.ExportSegmentsAsync(
                _state.MediaPath,
                _state.SoundRanges,
                dir,
                progress,
                cts.Token);

            _state.Status = "EXPORT DONE: " + dir;
        }
        catch (Exception ex)
        {
            _state.Status = "ERR: " + ex.Message;
        }
        finally
        {
            _canvas.Invalidate();
        }
    }

    private async Task PollMpvAsync()
    {
        if (_isPolling || _mpv is null || _state.MediaPath is null)
            return;

        _isPolling = true;

        try
        {
            using var cts = ShortCts();

            _state.Position = await _mpv.GetDoublePropertyAsync("time-pos", cts.Token);

            var duration = await _mpv.GetDoublePropertyAsync("duration", cts.Token);
            if (duration > 0.01)
                _state.Duration = duration;

            _state.IsPaused = await _mpv.GetBoolPropertyAsync("pause", cts.Token);

            await MaybeAutoAdvanceAsync();
        }
        catch
        {
            // One failed poll should not collapse the UI. Tiny mercy in a stupid universe.
        }
        finally
        {
            _isPolling = false;
            _canvas.Invalidate();
        }
    }

    private async Task MaybeAutoAdvanceAsync()
    {
        if (_autoAdvancing || _state.IsLooping || !_state.HasPlaylist)
            return;

        if (_state.Duration < MinimumPlaylistDurationSeconds)
            return;

        if (_state.Position < _state.Duration - 0.35)
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

    private async Task UpdatePreviewAsync()
    {
        if (_isSampling || _sampler is null || _state.MediaPath is null)
            return;

        _isSampling = true;

        try
        {
            using var cts = LongCts();
            _state.Preview = await _sampler.SampleAsync(
                _state.MediaPath,
                _state.Position,
                PreviewWidth,
                PreviewHeight,
                cts.Token);
        }
        catch
        {
            // Preview is ornamental. Playback matters more than decorative cave paintings.
        }
        finally
        {
            _isSampling = false;
            _canvas.Invalidate();
        }
    }

    private void ToggleTopMost()
    {
        TopMost = !TopMost;
        _state.IsTopMost = TopMost;
        _state.Status = TopMost ? "TOPMOST: ON" : "TOPMOST: OFF";
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private async Task OnDragDropAsync(DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;

        var first = files[0];

        if (Directory.Exists(first))
            await LoadFolderAsync(first);
        else
        {
            _state.Playlist.Clear();
            _state.PlaylistIndex = -1;
            await LoadMediaAsync(first);
        }
    }

    private static bool IsSupportedMediaPath(string path)
    {
        var ext = Path.GetExtension(path);
        return MediaExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private static CancellationTokenSource ShortCts()
    {
        return new CancellationTokenSource(TimeSpan.FromSeconds(4));
    }

    private static CancellationTokenSource LongCts()
    {
        return new CancellationTokenSource(TimeSpan.FromMinutes(10));
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        _uiTimer.Stop();
        _previewTimer.Stop();

        if (_stopPlaybackOnClose && _mpv is not null)
            await _mpv.DisposeAsync();

        base.OnFormClosing(e);
    }
}
