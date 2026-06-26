using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using MediaRelic.Domain;

namespace MediaRelic.Ui;

public sealed class ArcadeRelicCanvas : Control
{
    private const int PreviewCellW = 4;
    private const int PreviewCellH = 4;
    private const float MinUiScale = 0.75f;
    private const float MaxUiScale = 1.75f;

    private readonly Font _font = new("Consolas", 12.0f, FontStyle.Regular, GraphicsUnit.Point);
    private readonly Font _smallFont = new("Consolas", 3.8f, FontStyle.Regular, GraphicsUnit.Point);
    private readonly Brush _bg = new SolidBrush(Color.FromArgb(235, 3, 5, 8));
    private readonly PreviewArcadeGame _arcade = new();
    private readonly List<(RelicHudCommand Command, Rectangle Rect)> _hudButtons = new();

    private readonly Color _text = Color.FromArgb(190, 220, 220);
    private readonly Color _dim = Color.FromArgb(90, 115, 125);
    private readonly Color _hot = Color.FromArgb(245, 202, 112);
    private readonly Color _cold = Color.FromArgb(116, 224, 220);
    private readonly Color _bad = Color.FromArgb(225, 105, 126);
    private readonly Color _playPink = Color.FromArgb(255, 142, 184);
    private readonly Color _playRose = Color.FromArgb(255, 92, 142);
    private readonly Color _playPale = Color.FromArgb(255, 188, 207);
    private readonly Color _playDark = Color.FromArgb(120, 45, 72);

    private Rectangle _minimizeRect;
    private Rectangle _closeRect;

    public RelicState State { get; set; } = new();
    public float UiScale { get; private set; } = 1.0f;

    public ArcadeRelicCanvas()
    {
        DoubleBuffered = true;
        TabStop = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetUiScale(float scale)
    {
        UiScale = Math.Clamp(scale, MinUiScale, MaxUiScale);
        Invalidate();
    }

    public void AdjustUiScale(float delta)
    {
        SetUiScale(UiScale + delta);
    }

    public void SetArcadeInput(int dx, int dy, bool fire, bool boost)
    {
        _arcade.SetInput(dx, dy, fire, boost);
    }

    public void PulseArcadeFire()
    {
        _arcade.PulseFire();
    }

    public void ToggleArcadeMode()
    {
        _arcade.ToggleForced();
        State.Status = _arcade.IsForced ? "ARCADE SYNC RUN: FORCED" : "ARCADE SYNC RUN: AUTO";
        MarkVisualEvent("GAME MODE");
        Invalidate();
    }

    public RelicWindowCommand HitTestWindowCommand(Point point)
    {
        var logical = ToLogical(point);

        if (_minimizeRect.Contains(logical))
            return RelicWindowCommand.Minimize;

        if (_closeRect.Contains(logical))
            return RelicWindowCommand.CloseKeepPlaying;

        return RelicWindowCommand.None;
    }

    public RelicHudCommand HitTestHudCommand(Point point)
    {
        var logical = ToLogical(point);

        foreach (var button in _hudButtons)
        {
            if (button.Rect.Contains(logical))
                return button.Command;
        }

        return RelicHudCommand.None;
    }

    public bool IsDragZone(Point point)
    {
        var logical = ToLogical(point);
        return logical.Y >= 10 && logical.Y <= 62 && !_minimizeRect.Contains(logical) && !_closeRect.Contains(logical);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        g.FillRectangle(_bg, ClientRectangle);
        g.ScaleTransform(UiScale, UiScale);

        var logicalWidth = Math.Max(1, (int)Math.Round(ClientSize.Width / UiScale));
        var logicalHeight = Math.Max(1, (int)Math.Round(ClientSize.Height / UiScale));
        var widthChars = Math.Max(64, (logicalWidth - 28) / 10);
        var play = IsPlayingSignal();
        var y = 10;

        DrawLine(g, 12, y, TopBorder(widthChars), play ? _playRose : _cold, true); y += 18;
        DrawLine(g, 12, y, BodyLine(widthChars, play ? "MEDIA RELIC v0.1  |  ARCADE SYNC RUN // CYBER ROSE" : "MEDIA RELIC v0.1 by Alex Merqury  |  Delirium Interactive"), play ? _playPale : _hot, true);
        DrawWindowGlyphs(g, y); y += 18;
        DrawLine(g, 12, y, MidBorder(widthChars), play ? _playDark : _dim, true); y += 18;

        DrawLine(g, 12, y, BodyLine(widthChars, "◈ " + State.DisplayName + "   " + State.PlaylistLabel), play ? _playPale : _text); y += 18;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildTransportLine(widthChars - 4)), play ? _playPink : _text, play); y += 18;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildProgress(widthChars - 4)), play ? _playPink : _cold, true); y += 18;

        var gameMode = ShouldUseArcadePreview() ? (_arcade.IsForced ? "FORCED" : "AUTO") : "COVER";
        var flags = $"SPD {State.Speed:0.00}x   VOL {State.Volume:0}%   LOOP {(State.IsLooping ? "∞ ON" : "· OFF")}   TOP {(State.IsTopMost ? "ON" : "·")}   GAME {gameMode}   EVENT {State.VisualEvent}";
        DrawLine(g, 12, y, BodyLine(widthChars, flags), play ? _playPale : _text); y += 18;

        y += DrawTransportButtons(g, 24, y + 2, logicalWidth - 48) + 8;
        DrawLine(g, 12, y, BodyLine(widthChars, "G GAME/COVER  NUMPAD 8/2/4/6 DRIVE  7/9/1/3 DIAG  5/0 FIRE  NUM+ BOOST"), _dim, true); y += 18;
        DrawLine(g, 12, y, MidBorder(widthChars), play ? _playDark : _dim, true); y += 18;

        var beat = BeatPulse();
        var danceX = play ? (int)Math.Round(Math.Sin((State.Position + LocalFrameOffset()) * 11.0) * beat * 3.0) : 0;
        var danceY = play ? (int)Math.Round(Math.Cos((State.Position + LocalFrameOffset()) * 9.0) * beat * 2.0) : 0;
        var previewX = 22 + danceX;
        var previewY = y + 2 + danceY;
        var previewWidth = State.Preview.Width * PreviewCellW;
        var previewHeight = State.Preview.Height * PreviewCellH;

        DrawPreviewOrArcade(g, new Rectangle(previewX, previewY, previewWidth, previewHeight));
        DrawHudFrame(g, previewX, previewY, previewWidth, previewHeight);
        DrawHudBus(g, previewX + previewWidth + 28, previewY, Math.Max(150, logicalWidth - previewX - previewWidth - 48));

        y += previewHeight + 18;
        DrawLine(g, 12, y, MidBorder(widthChars), play ? _playDark : _dim, true); y += 18;
        DrawLine(g, 12, y, BodyLine(widthChars, State.Status), State.Status.StartsWith("ERR", StringComparison.OrdinalIgnoreCase) ? _bad : play ? _playPale : _hot, true); y += 18;
        DrawLine(g, 12, y, BottomBorder(widthChars), play ? _playRose : _cold, true);

        DrawVhsGlitchLayer(g, logicalWidth, logicalHeight);
    }

    private void DrawWindowGlyphs(Graphics g, int y)
    {
        var logicalWidth = Math.Max(1, (int)Math.Round(ClientSize.Width / UiScale));
        var minX = Math.Max(16, logicalWidth - 92);
        var closeX = Math.Max(48, logicalWidth - 54);

        _minimizeRect = new Rectangle(minX - 6, y - 2, 32, 22);
        _closeRect = new Rectangle(closeX - 6, y - 2, 32, 22);

        using var minBrush = new SolidBrush(ReactiveColor(_dim, 0.10, 0));
        using var closeBrush = new SolidBrush(ReactiveColor(_bad, 0.16, 800));
        g.DrawString("_", _font, minBrush, minX, y);
        g.DrawString("x", _font, closeBrush, closeX, y);
    }

    private int DrawTransportButtons(Graphics g, int x, int y, int maxWidth)
    {
        _hudButtons.Clear();
        var buttons = new[]
        {
            (Command: RelicHudCommand.Previous, Label: "PREV"),
            (Command: RelicHudCommand.SeekBack, Label: "-5"),
            (Command: RelicHudCommand.PlayPause, Label: State.IsPaused ? "PLAY" : "PAUSE"),
            (Command: RelicHudCommand.Restart, Label: "RESTART"),
            (Command: RelicHudCommand.Loop, Label: State.IsLooping ? "LOOP INF" : "LOOP ."),
            (Command: RelicHudCommand.SeekForward, Label: "+5"),
            (Command: RelicHudCommand.Next, Label: "NEXT"),
            (Command: RelicHudCommand.VolumeDown, Label: "VOL -"),
            (Command: RelicHudCommand.VolumeUp, Label: "VOL +")
        };

        var cursorX = x;
        var cursorY = y;
        var buttonH = 24;
        var gap = 6;
        var rows = 1;

        foreach (var button in buttons)
        {
            var buttonW = Math.Clamp(button.Label.Length * 10 + 18, 44, 118);
            if (cursorX + buttonW > x + maxWidth && cursorX > x)
            {
                cursorX = x;
                cursorY += buttonH + gap;
                rows++;
            }

            var rect = new Rectangle(cursorX, cursorY, buttonW, buttonH);
            _hudButtons.Add((button.Command, rect));
            DrawHudButton(g, rect, button.Command, button.Label);
            cursorX += buttonW + gap;
        }

        return rows * buttonH + (rows - 1) * gap;
    }

    private void DrawHudButton(Graphics g, Rectangle rect, RelicHudCommand command, string label)
    {
        var active = command switch
        {
            RelicHudCommand.PlayPause => IsPlayingSignal(),
            RelicHudCommand.Loop => State.IsLooping,
            _ => false
        };
        var baseColor = active ? _playPink : _dim;
        using var fill = new SolidBrush(Color.FromArgb(active ? 98 : 28, baseColor));
        using var pen = new Pen(active ? ReactiveColor(baseColor, 0.40, rect.X) : baseColor, active ? 2 : 1);
        using var brush = new SolidBrush(active ? _playPale : _text);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect);
        g.DrawString(label, _font, brush, rect.X + 7, rect.Y + 3);
    }

    private void DrawPreviewOrArcade(Graphics g, Rectangle bounds)
    {
        if (ShouldUseArcadePreview())
        {
            _arcade.Update(State.Position, State.Duration, IsPlayingSignal(), BeatPulse());
            _arcade.Draw(g, bounds, _smallFont, _playRose, _playPale, _cold, _dim, _hot, _bad);
            return;
        }

        DrawGlyphPreview(g, bounds.X, bounds.Y);
    }

    private void DrawGlyphPreview(Graphics g, int x, int y)
    {
        var frame = State.Preview;
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.MeasureTrailingSpaces,
            Trimming = StringTrimming.None
        };

        for (var row = 0; row < frame.Height; row++)
        {
            for (var col = 0; col < frame.Width; col++)
            {
                var cell = frame.At(col, row);
                var px = x + col * PreviewCellW;
                var py = y + row * PreviewCellH;

                if (cell.Rune == ' ')
                {
                    using var bg = new SolidBrush(Color.FromArgb(cell.BgR, cell.BgG, cell.BgB));
                    g.FillRectangle(bg, px, py, PreviewCellW + 1, PreviewCellH + 1);
                    continue;
                }

                using var fg = new SolidBrush(Color.FromArgb(cell.R, cell.G, cell.B));
                g.DrawString(cell.Rune.ToString(), _smallFont, fg, px, py - 1, format);
            }
        }
    }

    private bool ShouldUseArcadePreview()
    {
        return State.MediaPath is not null && (_arcade.IsForced || FrameLooksEmpty(State.Preview));
    }

    private static bool FrameLooksEmpty(GlyphFrame frame)
    {
        if (frame.Cells.Length == 0)
            return true;

        var step = Math.Max(1, frame.Cells.Length / 1024);
        var visible = 0;
        var sampled = 0;
        for (var i = 0; i < frame.Cells.Length; i += step)
        {
            sampled++;
            if (frame.Cells[i].Rune != ' ')
                visible++;
        }

        return sampled == 0 || visible < Math.Max(4, sampled / 80);
    }

    private void DrawHudFrame(Graphics g, int x, int y, int width, int height)
    {
        var play = IsPlayingSignal();
        var beat = BeatPulse();
        var baseColor = play ? _playPink : _cold;
        using var framePen = new Pen(ReactiveColor(baseColor, 0.30 + beat * 0.16, 100), 1 + (play && beat > 0.72 ? 1 : 0));
        using var weakPen = new Pen(Blend(_dim, baseColor, play ? 0.65 : 0.35), 1);
        g.DrawRectangle(weakPen, x - 8, y - 8, width + 16, height + 16);
        var corner = play ? 34 + (int)(beat * 8.0) : 26;
        g.DrawLine(framePen, x - 14, y - 14, x - 14 + corner, y - 14);
        g.DrawLine(framePen, x - 14, y - 14, x - 14, y - 14 + corner);
        g.DrawLine(framePen, x + width + 14, y - 14, x + width + 14 - corner, y - 14);
        g.DrawLine(framePen, x + width + 14, y - 14, x + width + 14, y - 14 + corner);
        g.DrawLine(framePen, x - 14, y + height + 14, x - 14 + corner, y + height + 14);
        g.DrawLine(framePen, x - 14, y + height + 14, x - 14, y + height + 14 - corner);
        g.DrawLine(framePen, x + width + 14, y + height + 14, x + width + 14 - corner, y + height + 14);
        g.DrawLine(framePen, x + width + 14, y + height + 14, x + width + 14, y + height + 14 - corner);
    }

    private void DrawHudBus(Graphics g, int x, int y, int width)
    {
        var height = Math.Max(180, State.Preview.Height * PreviewCellH);
        var nodes = new[]
        {
            (Name: "SRC", Active: State.MediaPath is not null, Value: Truncate(State.DisplayName, 20)),
            (Name: "DEC", Active: State.Duration > 0.01, Value: FormatTime(State.Duration)),
            (Name: "PLAY", Active: IsPlayingSignal(), Value: State.IsPaused ? "PAUSED" : "ACTIVE"),
            (Name: "GAME", Active: ShouldUseArcadePreview(), Value: ShouldUseArcadePreview() ? "SYNC RUN" : "COVER"),
            (Name: "OUT", Active: State.Mode != RelicMode.Empty && State.Mode != RelicMode.Error, Value: State.Mode.ToString().ToUpperInvariant())
        };

        using var dimPen = new Pen(_dim, 1);
        g.DrawLine(dimPen, x + 22, y + 8, x + 22, y + height - 8);

        for (var i = 0; i < nodes.Length; i++)
        {
            var yy = y + 18 + i * 34;
            var node = nodes[i];
            var color = node.Active ? ReactiveColor(node.Name == "PLAY" ? _playPink : _cold, 0.22, i * 180) : _dim;
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, node.Name == "PLAY" && node.Active ? 2 : 1);
            g.FillEllipse(brush, x + 14, yy - 4, 8, 8);
            g.DrawLine(pen, x + 22, yy, x + 33, yy);
            g.DrawRectangle(pen, x + 36, yy - 9, Math.Max(80, Math.Min(width - 48, 150)), 18);
            g.DrawString(node.Name + " :: " + node.Value, _font, brush, x + 41, yy - 10);
        }
    }

    private void DrawVhsGlitchLayer(Graphics g, int width, int height)
    {
        var burst = VhsBurstIntensity();
        var activity = Math.Max(burst, EventIntensity() * 0.45);
        if (activity < 0.07)
            return;

        var seed = unchecked((int)(Environment.TickCount64 / 70 + State.VisualEventCounter * 7919 + (long)(State.Position * 1000.0)));
        if (burst > 0.12)
        {
            using var rosePen = new Pen(Color.FromArgb(Math.Clamp((int)(10 + activity * 28), 8, 42), _playRose), 1);
            var offset = Math.Abs(seed % 7);
            for (var yy = offset; yy < height; yy += 9)
                g.DrawLine(rosePen, 0, yy, ClientSize.Width, yy);
        }

        if (burst > 0.32)
        {
            using var rose = new SolidBrush(Color.FromArgb(Math.Clamp((int)(28 + activity * 70), 24, 112), _playRose));
            var yy = Math.Abs(seed % Math.Max(1, height - 18));
            g.FillRectangle(rose, Math.Abs(seed % 32), yy, width, 2 + Math.Abs(seed % 12));
        }
    }

    private void DrawLine(Graphics g, int x, int y, string line, Color color, bool reactive = false)
    {
        var drawColor = reactive ? ReactiveColor(color, 0.16 * Math.Max(EventIntensity(), ModeIntensity()) + BeatPulse() * 0.08, y * 17) : color;
        using var brush = new SolidBrush(drawColor);
        g.DrawString(reactive ? MorphDecorative(line, y) : line, _font, brush, x, y);
    }

    private string MorphDecorative(string line, int rowSalt)
    {
        if (line.Length < 16 || Math.Max(EventIntensity(), ModeIntensity()) < 0.03)
            return line;

        var chars = line.ToCharArray();
        var pulses = IsPlayingSignal() ? 2 + (BeatPulse() > 0.66 ? 1 : 0) : 1;
        for (var p = 0; p < pulses; p++)
        {
            var index = 2 + (int)((Environment.TickCount64 / (160 + p * 70) + rowSalt + State.VisualEventCounter * 3 + p * 11) % Math.Max(1, line.Length - 4));
            chars[index] = chars[index] switch
            {
                '=' => '-',
                '-' => '=',
                ' ' => index % 7 == 0 ? '.' : ' ',
                _ => chars[index]
            };
        }
        return new string(chars);
    }

    private Color ReactiveColor(Color color, double amount, int phaseOffset)
    {
        var activity = Math.Max(EventIntensity(), ModeIntensity());
        if (activity <= 0.001 || amount <= 0.001)
            return color;
        var k = 1.0 + Math.Sin((Environment.TickCount64 + phaseOffset) * 0.004) * amount * activity;
        return Color.FromArgb(ClampByte(color.R * k), ClampByte(color.G * k), ClampByte(color.B * k));
    }

    private bool IsPlayingSignal() => State.Mode == RelicMode.Playing && !State.IsPaused;

    private double BeatPulse()
    {
        if (!IsPlayingSignal())
            return 0.0;
        var t = (Math.Max(0.0, State.Position) + LocalFrameOffset()) * Math.Max(0.25, State.Speed);
        var primary = 0.5 + 0.5 * Math.Sin(t * Math.PI * 4.0);
        var secondary = 0.5 + 0.5 * Math.Sin(t * Math.PI * 8.0 + 0.7);
        return Math.Pow(Math.Clamp(primary * 0.72 + secondary * 0.28, 0.0, 1.0), 2.6);
    }

    private static double LocalFrameOffset() => (Environment.TickCount64 % 250L) / 1000.0;

    private double VhsBurstIntensity()
    {
        if (!IsPlayingSignal())
            return EventIntensity() * 0.22;
        var phase = (Environment.TickCount64 / 80) % 96;
        var burst = phase switch { < 5 => 0.72, >= 31 and <= 33 => 0.42, >= 61 and <= 62 => 0.34, >= 88 and <= 90 => 0.88, _ => 0.0 };
        return burst <= 0.001 ? 0.0 : Math.Clamp(burst * (0.55 + BeatPulse() * 0.45), 0.0, 1.0);
    }

    private double EventIntensity()
    {
        var elapsed = Math.Max(0, Environment.TickCount64 - State.VisualEventTick) / 1000.0;
        return Math.Clamp(1.0 - elapsed / 1.2, 0.0, 1.0);
    }

    private double ModeIntensity()
    {
        if (IsPlayingSignal())
            return 0.58 + BeatPulse() * 0.32;
        return State.Mode switch { RelicMode.Loading => 0.85, RelicMode.ScanningSilence => 0.95, RelicMode.Exporting => 1.0, RelicMode.Error => 1.0, RelicMode.Playing => 0.22, _ => 0.0 };
    }

    private void MarkVisualEvent(string name)
    {
        State.VisualEvent = name;
        State.VisualEventTick = Environment.TickCount64;
        State.VisualEventCounter++;
    }

    private string BuildTransportLine(int contentWidth)
    {
        var mode = State.IsPaused ? "II PAUSED" : "> PLAYING";
        return Fit($"{mode}   {FormatTime(State.Position)} / {FormatTime(State.Duration)}", contentWidth);
    }

    private string BuildProgress(int width)
    {
        var barWidth = Math.Max(10, width - 18);
        var t = State.Duration > 0.01 ? Math.Clamp(State.Position / State.Duration, 0.0, 1.0) : 0.0;
        var dot = Math.Clamp((int)Math.Round(t * (barWidth - 1)), 0, barWidth - 1);
        var chars = Enumerable.Repeat(IsPlayingSignal() ? '=' : '-', barWidth).ToArray();
        for (var i = 0; i < dot; i++) chars[i] = BeatPulse() > 0.72 ? '#' : '*';
        chars[dot] = IsPlayingSignal() ? '>' : 'o';
        return Fit("[" + new string(chars) + "]", width);
    }

    private static Color Blend(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return Color.FromArgb(ClampByte(a.R + (b.R - a.R) * t), ClampByte(a.G + (b.G - a.G) * t), ClampByte(a.B + (b.B - a.B) * t));
    }

    private static byte ClampByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0.0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return "00:00";
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1.0 ? span.ToString(@"hh\:mm\:ss") : span.ToString(@"mm\:ss");
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..Math.Max(0, max - 1)] + "...";
    private static string TopBorder(int width) => "+" + new string('=', width - 2) + "+";
    private static string MidBorder(int width) => "+" + new string('-', width - 2) + "+";
    private static string BottomBorder(int width) => "+" + new string('=', width - 2) + "+";
    private static string BodyLine(int width, string content) => "|" + Fit(content, width - 2) + "|";

    private static string Fit(string value, int width)
    {
        if (width <= 0) return string.Empty;
        if (value.Length > width) return value[..Math.Max(0, width - 3)] + "...";
        return value.PadRight(width);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _font.Dispose();
            _smallFont.Dispose();
            _bg.Dispose();
        }
        base.Dispose(disposing);
    }
}
