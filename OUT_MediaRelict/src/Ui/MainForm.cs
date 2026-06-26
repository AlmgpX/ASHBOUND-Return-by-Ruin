using MediaRelic.App;

namespace MediaRelic.Ui;

public sealed class MainForm : Form
{
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

    private readonly RelicApp _app = new();
    private readonly RelicCanvas _canvas = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new();
    private readonly System.Windows.Forms.Timer _previewTimer = new();

    public MainForm()
    {
        Text = "MEDIA RELIC v0.1";
        Width = 980;
        Height = 620;
        MinimumSize = new Size(520, 260);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        TopMost = _app.Config.StartTopMost;
        Opacity = 0.94;
        KeyPreview = true;
        AllowDrop = true;
        BackColor = Color.Black;

        _app.State.IsTopMost = TopMost;
        _canvas.SetUiScale(_app.Config.UiScale);
        _canvas.Dock = DockStyle.Fill;
        _canvas.State = _app.State;
        Controls.Add(_canvas);

        _uiTimer.Interval = 250;
        _uiTimer.Tick += async (_, _) => await PollAsync();
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
        if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Q)
        {
            Close();
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.M:
                WindowState = FormWindowState.Minimized;
                return;

            case Keys.F11:
                ToggleMaximized();
                return;

            case Keys.Oemplus:
            case Keys.Add:
                _canvas.AdjustUiScale(+0.10f);
                _app.State.Status = $"UI SCALE {_canvas.UiScale:0.00}";
                return;

            case Keys.OemMinus:
            case Keys.Subtract:
                _canvas.AdjustUiScale(-0.10f);
                _app.State.Status = $"UI SCALE {_canvas.UiScale:0.00}";
                return;

            case Keys.D0:
            case Keys.NumPad0:
                _canvas.SetUiScale(1.0f);
                _app.State.Status = "UI SCALE 1.00";
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
                await _app.PlayRelativeAsync(+1);
                break;

            case Keys.B:
                await _app.PlayRelativeAsync(-1);
                break;

            case Keys.Space:
                await _app.TogglePauseAsync();
                break;

            case Keys.Left:
            case Keys.A:
                await _app.SeekAsync(-5.0);
                break;

            case Keys.Right:
            case Keys.D:
                await _app.SeekAsync(5.0);
                break;

            case Keys.PageDown:
                await _app.SeekAsync(-30.0);
                break;

            case Keys.PageUp:
                await _app.SeekAsync(30.0);
                break;

            case Keys.Up:
                await _app.SetVolumeAsync(_app.State.Volume + 5.0);
                break;

            case Keys.Down:
                await _app.SetVolumeAsync(_app.State.Volume - 5.0);
                break;

            case Keys.OemOpenBrackets:
                await _app.SetSpeedAsync(_app.State.Speed - 0.1);
                break;

            case Keys.OemCloseBrackets:
                await _app.SetSpeedAsync(_app.State.Speed + 0.1);
                break;

            case Keys.Back:
                await _app.SetSpeedAsync(1.0);
                break;

            case Keys.L:
                await _app.ToggleLoopAsync();
                break;

            case Keys.R:
                await _app.ToggleReverbAsync();
                break;

            case Keys.S:
                await _app.ScanSilenceAsync();
                break;

            case Keys.X:
                await _app.ExportCutsAsync();
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

        await _app.OpenFileAsync(dialog.FileName);
    }

    private async Task OpenFolderDialogAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Open folder as MEDIA RELIC playlist"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await _app.OpenFolderAsync(dialog.SelectedPath);
    }

    private async Task ApplyCoverDialogAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Apply cover image to current track",
            Filter = "Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await _app.ApplyCoverAsync(dialog.FileName);
    }

    private async Task PollAsync()
    {
        await _app.PollAsync();
        _canvas.Invalidate();
    }

    private async Task UpdatePreviewAsync()
    {
        await _app.UpdatePreviewAsync();
        _canvas.Invalidate();
    }

    private void ToggleTopMost()
    {
        TopMost = !TopMost;
        _app.State.IsTopMost = TopMost;
        _app.State.Status = TopMost ? "TOPMOST: ON" : "TOPMOST: OFF";
    }

    private void ToggleMaximized()
    {
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;

        _app.State.Status = WindowState == FormWindowState.Maximized ? "WINDOW: MAXIMIZED" : "WINDOW: NORMAL";
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
            await _app.OpenFolderAsync(first);
        else
            await _app.OpenFileAsync(first);

        _canvas.Invalidate();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        _uiTimer.Stop();
        _previewTimer.Stop();

        await _app.DisposeAsync();

        base.OnFormClosing(e);
    }
}
