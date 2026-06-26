using System.Drawing.Drawing2D;
using System.Drawing.Text;
using MediaRelic.Domain;

namespace MediaRelic.Ui;

public enum RelicWindowCommand
{
    None,
    Minimize,
    CloseKeepPlaying
}

public sealed class RelicCanvas : Control
{
    private const int PreviewCellW = 4;
    private const int PreviewCellH = 4;
    private const float MinUiScale = 0.75f;
    private const float MaxUiScale = 1.75f;

    private Font _font;
    private Font _smallFont;
    private readonly Brush _bg = new SolidBrush(Color.FromArgb(235, 3, 5, 8));
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

    public RelicCanvas()
    {
        DoubleBuffered = true;
        _font = new Font("Consolas", 12.0f, FontStyle.Regular, GraphicsUnit.Point);
        _smallFont = new Font("Consolas", 3.8f, FontStyle.Regular, GraphicsUnit.Point);
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

    public RelicWindowCommand HitTestWindowCommand(Point point)
    {
        var logical = ToLogical(point);

        if (_minimizeRect.Contains(logical))
            return RelicWindowCommand.Minimize;

        if (_closeRect.Contains(logical))
            return RelicWindowCommand.CloseKeepPlaying;

        return RelicWindowCommand.None;
    }

    public bool IsDragZone(Point point)
    {
        var logical = ToLogical(point);
        return logical.Y >= 10 && logical.Y <= 62 && !_minimizeRect.Contains(logical) && !_closeRect.Contains(logical);
    }

    private Point ToLogical(Point point)
    {
        return new Point(
            (int)Math.Round(point.X / UiScale),
            (int)Math.Round(point.Y / UiScale));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        g.FillRectangle(_bg, ClientRectangle);
        g.ScaleTransform(UiScale, UiScale);

        var logicalWidth = Math.Max(1, (int)Math.Round(ClientSize.Width / UiScale));
        var charW = 10;
        var charH = 18;
        var widthChars = Math.Max(64, (logicalWidth - 28) / charW);
        var y = 10;
        var play = IsPlayingSignal();

        DrawLine(g, 12, y, TopBorder(widthChars), play ? _playRose : _cold, reactive: true); y += charH;

        var title = play
            ? "MEDIA RELIC v0.1  |  PLAY SIGNAL // CYBER ROSE OUTPUT"
            : "MEDIA RELIC v0.1 by Alex Merqury  |  Delirium Interactive";

        DrawLine(g, 12, y, BodyLine(widthChars, title), play ? _playPale : _hot, reactive: true); y += charH;
        DrawWindowGlyphs(g, y - charH);

        DrawLine(g, 12, y, MidBorder(widthChars), play ? _playDark : _dim, reactive: true); y += charH;

        DrawLine(g, 12, y, BodyLine(widthChars, "◈ " + State.DisplayName + "   " + State.PlaylistLabel), play ? _playPale : _text); y += charH;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildTransportLine(widthChars - 4)), play ? _playPink : _text, reactive: play); y += charH;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildPlaySignalLine(widthChars - 4)), play ? _playRose : _dim, reactive: play); y += charH;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildProgress(widthChars - 4)), play ? _playPink : _cold, reactive: true); y += charH;

        var flags = $"SPD {State.Speed:0.00}x   VOL {State.Volume:0}%   LOOP {(State.IsLooping ? "∞" : "·")}   TOP {(State.IsTopMost ? "ON" : "·")}   FX {(State.IsReverbEnabled ? "✦" : "·")}   MODE {State.Mode}   EVENT {State.VisualEvent}";
        DrawLine(g, 12, y, BodyLine(widthChars, flags), play ? _playPale : _text); y += charH;

        DrawLine(g, 12, y, BodyLine(widthChars, "O FILE  P FOLDER  C COVER  SPACE PLAY  ←/→ SEEK  ↑/↓ VOL  A/D SEEK  WHEEL VOL  CTRL+WHEEL SCALE"), _dim, reactive: true); y += charH;
        DrawLine(g, 12, y, MidBorder(widthChars), play ? _playDark : _dim, reactive: true); y += charH;

        var previewX = 22;
        var previewY = y + 2;
        DrawPreview(g, previewX, previewY);
        DrawHudFrame(g, previewX, previewY, State.Preview.Width * PreviewCellW, State.Preview.Height * PreviewCellH);
        DrawHudBus(g, previewX + State.Preview.Width * PreviewCellW + 28, previewY, Math.Max(150, logicalWidth - previewX - State.Preview.Width * PreviewCellW - 48));

        y += State.Preview.Height * PreviewCellH + charH;

        DrawLine(g, 12, y, MidBorder(widthChars), play ? _playDark : _dim, reactive: true); y += charH;

        var statusColor = State.Status.StartsWith("ERR", StringComparison.OrdinalIgnoreCase) ? _bad : play ? _playPale : _hot;
        DrawLine(g, 12, y, BodyLine(widthChars, State.Status), statusColor, reactive: true); y += charH;

        DrawLine(g, 12, y, BottomBorder(widthChars), play ? _playRose : _cold, reactive: true);
    }

    private void DrawWindowGlyphs(Graphics g, int y)
    {
        var logicalWidth = Math.Max(1, (int)Math.Round(ClientSize.Width / UiScale));
        var closeX = Math.Max(48, logicalWidth - 54);
        var minX = Math.Max(16, logicalWidth - 92);

        _minimizeRect = new Rectangle(minX - 6, y - 2, 32, 22);
        _closeRect = new Rectangle(closeX - 6, y - 2, 32, 22);

        using var minBrush = new SolidBrush(ReactiveColor(_dim, 0.10f, 0));
        using var closeBrush = new SolidBrush(ReactiveColor(_bad, 0.16f, 800));

        g.DrawString("▁", _font, minBrush, minX, y);
        g.DrawString("×", _font, closeBrush, closeX, y);
    }

    private void DrawPreview(Graphics g, int x, int y)
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

    private void DrawHudFrame(Graphics g, int x, int y, int width, int height)
    {
        var play = IsPlayingSignal();
        var activity = (float)Math.Max(EventIntensity(), ModeIntensity());
        var baseColor = play ? _playPink : _cold;
        var frame = ReactiveColor(baseColor, play ? 0.32f : 0.28f * activity, 100);
        var weak = Blend(play ? _playDark : _dim, frame, play ? 0.62f : 0.45f);

        using var framePen = new Pen(frame, 1);
        using var weakPen = new Pen(weak, 1);
        using var hotPen = new Pen(ReactiveColor(play ? _playPale : _hot, play ? 0.28f : 0.22f * activity, 300), 1);

        g.DrawRectangle(weakPen, x - 8, y - 8, width + 16, height + 16);

        var corner = play ? 34 : 26;
        g.DrawLine(framePen, x - 14, y - 14, x - 14 + corner, y - 14);
        g.DrawLine(framePen, x - 14, y - 14, x - 14, y - 14 + corner);
        g.DrawLine(framePen, x + width + 14, y - 14, x + width + 14 - corner, y - 14);
        g.DrawLine(framePen, x + width + 14, y - 14, x + width + 14, y - 14 + corner);
        g.DrawLine(framePen, x - 14, y + height + 14, x - 14 + corner, y + height + 14);
        g.DrawLine(framePen, x - 14, y + height + 14, x - 14, y + height + 14 - corner);
        g.DrawLine(framePen, x + width + 14, y + height + 14, x + width + 14 - corner, y + height + 14);
        g.DrawLine(framePen, x + width + 14, y + height + 14, x + width + 14, y + height + 14 - corner);

        if (State.Mode is RelicMode.Loading or RelicMode.ScanningSilence or RelicMode.Exporting)
        {
            var sweep = (int)((Environment.TickCount64 / 9) % Math.Max(1, height));
            g.DrawLine(hotPen, x - 4, y + sweep, x + width + 4, y + sweep);
            g.DrawLine(weakPen, x + sweep % Math.Max(1, width), y - 4, x + sweep % Math.Max(1, width), y + height + 4);
        }
        else if (play)
        {
            var sweep = (int)((Environment.TickCount64 / 18) % Math.Max(1, height));
            g.DrawLine(hotPen, x - 4, y + sweep, x + width + 4, y + sweep);
        }
        else if (activity > 0.02f)
        {
            var sweep = (int)((1.0 - EventIntensity()) * Math.Max(1, height));
            g.DrawLine(hotPen, x - 4, y + sweep, x + width + 4, y + sweep);
        }
    }

    private void DrawHudBus(Graphics g, int x, int y, int width)
    {
        var height = Math.Max(180, State.Preview.Height * PreviewCellH);
        var nodeX = x + 18;
        var labelX = x + 38;
        var lineX = x + 22;
        var play = IsPlayingSignal();

        using var dimPen = new Pen(_dim, 1);
        using var coldPen = new Pen(ReactiveColor(play ? _playPink : _cold, 0.16f, 200), 1);
        using var hotPen = new Pen(ReactiveColor(play ? _playPale : _hot, 0.22f, 500), 1);
        using var badPen = new Pen(ReactiveColor(_bad, 0.22f, 700), 1);

        g.DrawLine(dimPen, lineX, y + 8, lineX, y + height - 8);

        var nodes = new[]
        {
            (Name: "SRC", Active: State.MediaPath is not null, Value: Truncate(State.DisplayName, 20)),
            (Name: "DEC", Active: State.Duration > 0.01, Value: FormatTime(State.Duration)),
            (Name: "PLAY", Active: State.Mode == RelicMode.Playing && !State.IsPaused, Value: State.IsPaused ? "PAUSED" : "ACTIVE"),
            (Name: "FX", Active: State.IsReverbEnabled || Math.Abs(State.Speed - 1.0) > 0.01, Value: $"{State.Speed:0.00}x {(State.IsReverbEnabled ? "REV" : "DRY")}"),
            (Name: "CUT", Active: State.SoundRanges.Count > 0 || State.Mode is RelicMode.ScanningSilence or RelicMode.Exporting, Value: State.SoundRanges.Count.ToString()),
            (Name: "OUT", Active: State.Mode != RelicMode.Empty && State.Mode != RelicMode.Error, Value: State.Mode.ToString().ToUpperInvariant())
        };

        for (var i = 0; i < nodes.Length; i++)
        {
            var yy = y + 18 + i * 34;
            var node = nodes[i];
            var activeColor = State.Mode == RelicMode.Error
                ? _bad
                : node.Name == "PLAY" && play
                    ? _playPink
                    : play && node.Active
                        ? Blend(_cold, _playRose, 0.42f)
                        : _cold;

            var color = node.Active ? ReactiveColor(activeColor, node.Name == "PLAY" ? 0.34f : 0.18f, i * 180) : _dim;

            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, node.Name == "PLAY" && play ? 2 : 1);

            g.FillEllipse(brush, nodeX - 4, yy - 4, 8, 8);
            g.DrawLine(pen, lineX, yy, labelX - 5, yy);
            g.DrawRectangle(pen, labelX - 2, yy - 9, Math.Max(80, Math.Min(width - 48, 150)), 18);
            g.DrawString(node.Name + " :: " + node.Value, _font, brush, labelX + 3, yy - 10);
        }

        if (play)
        {
            using var playBrush = new SolidBrush(ReactiveColor(_playPale, 0.28f, 990));
            g.DrawString("▶ PLAY SIGNAL", _font, playBrush, x, y + height - 58);
            g.DrawString("PALE RED / ROSE", _font, playBrush, x, y + height - 40);
        }

        var eventPower = (float)EventIntensity();
        if (eventPower > 0.01f)
        {
            var yy = y + height - 26;
            var eventColor = ReactiveColor(play ? _playRose : _hot, 0.35f * eventPower, 999);
            using var eventBrush = new SolidBrush(eventColor);
            using var eventPen = new Pen(eventColor, 1);

            g.DrawLine(eventPen, x, yy, x + Math.Min(width - 8, 210), yy);
            g.DrawString("EVENT >> " + State.VisualEvent, _font, eventBrush, x, yy + 4);

            var tick = (Environment.TickCount64 / 80 + State.VisualEventCounter) % 7;
            for (var i = 0; i < 7; i++)
            {
                if (i == tick)
                    g.DrawString("✦", _font, eventBrush, x + 150 + i * 12, yy - 16);
                else
                    g.DrawString("·", _font, eventBrush, x + 150 + i * 12, yy - 16);
            }
        }

        if (State.Mode == RelicMode.Error)
            g.DrawLine(badPen, x, y + height, x + Math.Min(width - 8, 240), y + height);
        else if (State.Mode is RelicMode.Loading or RelicMode.ScanningSilence or RelicMode.Exporting)
            g.DrawLine(hotPen, x, y + height, x + Math.Min(width - 8, 240), y + height);
        else
            g.DrawLine(coldPen, x, y + height, x + Math.Min(width - 8, 240), y + height);
    }

    private void DrawLine(Graphics g, int x, int y, string line, Color color, bool reactive = false)
    {
        var activity = (float)Math.Max(EventIntensity(), ModeIntensity());
        var drawColor = reactive ? ReactiveColor(color, 0.16f * activity, y * 17) : color;
        var drawText = reactive ? MorphDecorative(line, y, activity) : line;

        using var brush = new SolidBrush(drawColor);
        g.DrawString(drawText, _font, brush, x, y);
    }

    private Color ReactiveColor(Color color, float amount, int phaseOffset)
    {
        var activity = Math.Max(EventIntensity(), ModeIntensity());
        if (activity <= 0.001 || amount <= 0.001)
            return color;

        var t = (Environment.TickCount64 + phaseOffset) * 0.004;
        var k = 1.0 + Math.Sin(t) * amount * activity;

        return Color.FromArgb(
            ClampByte(color.R * k),
            ClampByte(color.G * k),
            ClampByte(color.B * k));
    }

    private bool IsPlayingSignal()
    {
        return State.Mode == RelicMode.Playing && !State.IsPaused;
    }

    private double EventIntensity()
    {
        var elapsed = Math.Max(0, Environment.TickCount64 - State.VisualEventTick) / 1000.0;
        return Math.Clamp(1.0 - elapsed / 1.2, 0.0, 1.0);
    }

    private double ModeIntensity()
    {
        if (IsPlayingSignal())
            return 0.58;

        return State.Mode switch
        {
            RelicMode.Loading => 0.85,
            RelicMode.ScanningSilence => 0.95,
            RelicMode.Exporting => 1.0,
            RelicMode.Error => 1.0,
            RelicMode.Playing => 0.22,
            _ => 0.0
        };
    }

    private static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0.0f, 1.0f);
        return Color.FromArgb(
            ClampByte(a.R + (b.R - a.R) * t),
            ClampByte(a.G + (b.G - a.G) * t),
            ClampByte(a.B + (b.B - a.B) * t));
    }

    private static byte ClampByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }

    private string MorphDecorative(string line, int rowSalt, float activity)
    {
        if (activity < 0.03f || line.Length < 16)
            return line;

        var chars = line.ToCharArray();
        var pulseCount = State.Mode is RelicMode.Loading or RelicMode.ScanningSilence or RelicMode.Exporting ? 3 : IsPlayingSignal() ? 2 : 1;

        for (var p = 0; p < pulseCount; p++)
        {
            var index = 2 + (int)((Environment.TickCount64 / (160 + p * 70) + rowSalt + State.VisualEventCounter * 3 + p * 11) % Math.Max(1, line.Length - 4));
            var c = chars[index];
            chars[index] = c switch
            {
                '═' => '─',
                '─' => '═',
                '·' => '✦',
                '✦' => '·',
                ' ' => (index % 7 == 0 ? '·' : ' '),
                _ => c
            };
        }

        return new string(chars);
    }

    private string BuildTransportLine(int contentWidth)
    {
        var mode = State.IsPaused ? "Ⅱ PAUSED" : "▶ PLAYING";
        var left = $"{mode}   {FormatTime(State.Position)} / {FormatTime(State.Duration)}";
        return Fit(left, contentWidth);
    }

    private string BuildPlaySignalLine(int contentWidth)
    {
        if (!IsPlayingSignal())
            return Fit("PLAY SIGNAL: STANDBY", contentWidth);

        return Fit("▶ PLAY BUTTON SIGNAL: ACTIVE  // PALE RED CYBER ROSE OUTPUT", contentWidth);
    }

    private string BuildProgress(int width)
    {
        var barWidth = Math.Max(10, width - 18);
        var t = State.Duration > 0.01 ? State.Position / State.Duration : 0.0;
        t = Math.Clamp(t, 0.0, 1.0);

        var dot = (int)Math.Round(t * (barWidth - 1));
        var chars = Enumerable.Repeat(IsPlayingSignal() ? '═' : '─', barWidth).ToArray();

        for (var i = 0; i < dot; i++)
            chars[i] = '█';

        chars[dot] = IsPlayingSignal() ? '◆' : '●';

        return Fit("⟦" + new string(chars) + "⟧", width);
    }

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0.0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            return "00:00";

        var span = TimeSpan.FromSeconds(seconds);

        if (span.TotalHours >= 1.0)
            return span.ToString(@"hh\:mm\:ss");

        return span.ToString(@"mm\:ss");
    }

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max)
            return value;

        return value[..Math.Max(0, max - 1)] + "…";
    }

    private static string TopBorder(int width) => "╔" + new string('═', width - 2) + "╗";
    private static string MidBorder(int width) => "╟" + new string('─', width - 2) + "╢";
    private static string BottomBorder(int width) => "╚" + new string('═', width - 2) + "╝";

    private static string BodyLine(int width, string content)
    {
        return "║" + Fit(content, width - 2) + "║";
    }

    private static string Fit(string value, int width)
    {
        if (width <= 0)
            return string.Empty;

        if (value.Length > width)
            return value[..Math.Max(0, width - 1)] + "…";

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
