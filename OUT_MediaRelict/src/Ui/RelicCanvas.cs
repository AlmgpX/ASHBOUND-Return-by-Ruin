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
    private const int PreviewCellW = 6;
    private const int PreviewCellH = 6;
    private readonly Font _font;
    private readonly Font _smallFont;
    private readonly Brush _bg = new SolidBrush(Color.FromArgb(235, 3, 5, 8));
    private readonly Color _text = Color.FromArgb(190, 220, 220);
    private readonly Color _dim = Color.FromArgb(90, 115, 125);
    private readonly Color _hot = Color.FromArgb(245, 202, 112);
    private readonly Color _cold = Color.FromArgb(116, 224, 220);
    private readonly Color _bad = Color.FromArgb(225, 105, 126);

    private Rectangle _minimizeRect;
    private Rectangle _closeRect;

    public RelicState State { get; set; } = new();

    public RelicCanvas()
    {
        DoubleBuffered = true;
        _font = new Font("Consolas", 12.0f, FontStyle.Regular, GraphicsUnit.Point);
        _smallFont = new Font("Consolas", 8.0f, FontStyle.Regular, GraphicsUnit.Point);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public RelicWindowCommand HitTestWindowCommand(Point point)
    {
        if (_minimizeRect.Contains(point))
            return RelicWindowCommand.Minimize;

        if (_closeRect.Contains(point))
            return RelicWindowCommand.CloseKeepPlaying;

        return RelicWindowCommand.None;
    }

    public bool IsDragZone(Point point)
    {
        return point.Y >= 10 && point.Y <= 62 && !_minimizeRect.Contains(point) && !_closeRect.Contains(point);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        g.FillRectangle(_bg, ClientRectangle);

        var charW = 10;
        var charH = 18;
        var widthChars = Math.Max(64, (ClientSize.Width - 28) / charW);
        var y = 10;

        DrawLine(g, 12, y, TopBorder(widthChars), _cold); y += charH;

        var title = "MEDIA RELIC v0.1 by Alex Merqury  |  Delirium Interactive";
        DrawLine(g, 12, y, BodyLine(widthChars, title), _hot);
        DrawWindowGlyphs(g, y);
        y += charH;

        DrawLine(g, 12, y, MidBorder(widthChars), _dim); y += charH;

        DrawLine(g, 12, y, BodyLine(widthChars, "◈ " + State.DisplayName + "   " + State.PlaylistLabel), _text); y += charH;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildTransportLine(widthChars - 4)), _text); y += charH;
        DrawLine(g, 12, y, BodyLine(widthChars, BuildProgress(widthChars - 4)), _cold); y += charH;

        var flags = $"SPD {State.Speed:0.00}x   VOL {State.Volume:0}%   LOOP {(State.IsLooping ? "∞" : "·")}   REVERB {(State.IsReverbEnabled ? "✦" : "·")}   CUTS {State.SoundRanges.Count}";
        DrawLine(g, 12, y, BodyLine(widthChars, flags), _text); y += charH;

        DrawLine(g, 12, y, BodyLine(widthChars, "O FILE  P FOLDER  C COVER  SPACE PLAY  ←→ SEEK  N NEXT  B BACK  S SCAN  X EXPORT  ESC EXIT"), _dim); y += charH;
        DrawLine(g, 12, y, MidBorder(widthChars), _dim); y += charH;

        DrawPreview(g, 22, y + 2);
        y += State.Preview.Height * PreviewCellH + charH;

        DrawLine(g, 12, y, MidBorder(widthChars), _dim); y += charH;

        var statusColor = State.Status.StartsWith("ERR", StringComparison.OrdinalIgnoreCase) ? _bad : _hot;
        DrawLine(g, 12, y, BodyLine(widthChars, State.Status), statusColor); y += charH;

        DrawLine(g, 12, y, BottomBorder(widthChars), _cold);
    }

    private void DrawWindowGlyphs(Graphics g, int y)
    {
        var closeX = Math.Max(48, ClientSize.Width - 54);
        var minX = Math.Max(16, ClientSize.Width - 92);

        _minimizeRect = new Rectangle(minX - 6, y - 2, 32, 22);
        _closeRect = new Rectangle(closeX - 6, y - 2, 32, 22);

        using var minBrush = new SolidBrush(_dim);
        using var closeBrush = new SolidBrush(_bad);

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

                using var bg = new SolidBrush(Color.FromArgb(cell.BgR, cell.BgG, cell.BgB));
                g.FillRectangle(bg, px, py, PreviewCellW + 1, PreviewCellH + 1);

                if (cell.Rune == ' ')
                    continue;

                using var fg = new SolidBrush(Color.FromArgb(cell.R, cell.G, cell.B));
                g.DrawString(cell.Rune.ToString(), _smallFont, fg, px, py - 1, format);
            }
        }
    }

    private void DrawLine(Graphics g, int x, int y, string line, Color color)
    {
        using var brush = new SolidBrush(color);
        g.DrawString(line, _font, brush, x, y);
    }

    private string BuildTransportLine(int contentWidth)
    {
        var mode = State.IsPaused ? "Ⅱ PAUSED" : "▶ PLAYING";
        var left = $"{mode}   {FormatTime(State.Position)} / {FormatTime(State.Duration)}";
        return Fit(left, contentWidth);
    }

    private string BuildProgress(int width)
    {
        var barWidth = Math.Max(10, width - 18);
        var t = State.Duration > 0.01 ? State.Position / State.Duration : 0.0;
        t = Math.Clamp(t, 0.0, 1.0);

        var dot = (int)Math.Round(t * (barWidth - 1));
        var chars = Enumerable.Repeat('─', barWidth).ToArray();

        for (var i = 0; i < dot; i++)
            chars[i] = '═';

        chars[dot] = '●';

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
