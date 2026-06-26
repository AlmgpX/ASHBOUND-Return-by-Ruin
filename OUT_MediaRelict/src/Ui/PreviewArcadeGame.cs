using System.Drawing;
using System.Drawing.Drawing2D;

namespace MediaRelic.Ui;

internal sealed class PreviewArcadeGame
{
    private const string ShipGlyph = "\U0001F6E6";
    private const string ShipFallbackGlyph = "✈";

    private readonly Random _random = new(0xA51B0D);
    private readonly List<ArcadeEntity> _entities = new();
    private readonly List<ArcadeEntity> _bullets = new();
    private readonly List<SideStreak> _sideStreaks = new();
    private readonly Font _shipFont = new("Segoe UI Symbol", 28.0f, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _shipFallbackFont = new("Segoe UI Symbol", 22.0f, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _billboardFont = new("Consolas", 9.0f, FontStyle.Bold, GraphicsUnit.Point);
    private readonly Font _entityFont = new("Consolas", 8.5f, FontStyle.Bold, GraphicsUnit.Point);

    private long _lastTick;
    private double _spawnTimer;
    private double _sideSpawnTimer;
    private double _fireCooldown;
    private double _damageFlash;
    private double _boostFlash;
    private double _trackProgress;
    private double _lastBeat;
    private int _inputDx;
    private int _inputDy;
    private bool _fireHeld;
    private bool _boostHeld;
    private double _playerX = 0.5;
    private double _playerY = 0.82;
    private int _hp = 5;
    private int _score;
    private int _combo;

    public bool IsForced { get; private set; }

    public void ToggleForced()
    {
        IsForced = !IsForced;
    }

    public void SetInput(int dx, int dy, bool fire, bool boost)
    {
        _inputDx = Math.Clamp(dx, -1, 1);
        _inputDy = Math.Clamp(dy, -1, 1);
        _fireHeld = fire;
        _boostHeld = boost;
    }

    public void PulseFire()
    {
        _fireHeld = true;
        _fireCooldown = Math.Min(_fireCooldown, 0.01);
    }

    public void ResetRun()
    {
        _entities.Clear();
        _bullets.Clear();
        _sideStreaks.Clear();
        _playerX = 0.5;
        _playerY = 0.82;
        _spawnTimer = 0.0;
        _sideSpawnTimer = 0.0;
        _fireCooldown = 0.0;
        _damageFlash = 0.0;
        _boostFlash = 0.0;
        _trackProgress = 0.0;
        _lastBeat = 0.0;
        _hp = 5;
        _score = 0;
        _combo = 0;
        _lastTick = 0;
    }

    public void Update(double position, double duration, bool playing, double beat)
    {
        var now = Environment.TickCount64;
        if (_lastTick == 0)
            _lastTick = now;

        var dt = Math.Clamp((now - _lastTick) / 1000.0, 0.0, 0.05);
        _lastTick = now;

        if (!playing)
            dt *= 0.25;

        var progress = duration > 0.01 ? Math.Clamp(position / duration, 0.0, 1.0) : 0.0;
        _trackProgress = progress;
        _lastBeat = beat;

        var difficulty = 0.2 + progress * 1.65 + beat * 0.35;
        var boost = _boostHeld ? 1.75 : 1.0;
        var playerSpeed = (0.62 + progress * 0.20) * boost;

        if (_hp <= 0)
        {
            _damageFlash += dt;
            if (_damageFlash > 1.2)
                ResetRun();
            return;
        }

        _playerX = Math.Clamp(_playerX + _inputDx * playerSpeed * dt, 0.12, 0.88);
        _playerY = Math.Clamp(_playerY + _inputDy * playerSpeed * dt, 0.18, 0.90);

        _fireCooldown = Math.Max(0.0, _fireCooldown - dt);
        _damageFlash = Math.Max(0.0, _damageFlash - dt * 2.4);
        _boostFlash = Math.Max(0.0, _boostFlash - dt * 2.0);

        if (_boostHeld)
            _boostFlash = Math.Max(_boostFlash, 0.35);

        if ((_fireHeld || _boostHeld) && _fireCooldown <= 0.0)
        {
            Shoot(_boostHeld);
            _fireCooldown = _boostHeld ? 0.065 : 0.105;
        }

        UpdateBullets(dt);
        UpdateEntities(dt, difficulty, beat);
        UpdateSideStreaks(dt, difficulty, beat);
        SpawnEntities(dt, difficulty, progress, beat);
        SpawnSideStreaks(dt, difficulty, beat);
        Collide();
    }

    public void Draw(Graphics g, Rectangle bounds, Font font, Color rose, Color pale, Color cold, Color dim, Color hot, Color bad)
    {
        g.SmoothingMode = SmoothingMode.None;

        using var bg = new SolidBrush(Color.FromArgb(222, 2, 4, 7));
        using var coldBrush = new SolidBrush(cold);
        using var paleBrush = new SolidBrush(pale);
        using var roseBrush = new SolidBrush(rose);
        using var dimBrush = new SolidBrush(Color.FromArgb(165, dim));
        using var hotBrush = new SolidBrush(hot);
        using var badBrush = new SolidBrush(bad);

        g.FillRectangle(bg, bounds);

        var time = Environment.TickCount64 / 1000.0;
        var cellW = Math.Max(4, bounds.Width / 96);
        var cellH = Math.Max(4, bounds.Height / 96);

        DrawMovingTrack(g, bounds, rose, pale, cold, dim, hot, time);
        DrawBillboards(g, bounds, roseBrush, coldBrush, dimBrush, hotBrush, time);
        DrawSideObjects(g, bounds, cellW, cellH, roseBrush, dimBrush, hotBrush);
        DrawEntities(g, bounds, roseBrush, dimBrush, hotBrush);
        DrawBullets(g, bounds, paleBrush, hotBrush);
        DrawPlayer(g, bounds, rose, pale, hot, bad, paleBrush, hotBrush, badBrush);
        DrawOverlayHud(g, bounds, roseBrush, hotBrush, badBrush, dimBrush);

        if (_hp <= 0)
        {
            using var overlay = new SolidBrush(Color.FromArgb(150, Color.Black));
            g.FillRectangle(overlay, bounds);
            DrawText(g, _billboardFont, bounds.Left + bounds.Width / 2 - 72, bounds.Top + bounds.Height / 2 - 10, "SIGNAL LOST", badBrush);
        }

        if ((int)(time * 8) % 13 == 0)
        {
            using var tearPen = new Pen(Color.FromArgb(90, rose), 1);
            var y = bounds.Top + (int)((time * 71) % Math.Max(1, bounds.Height));
            g.DrawLine(tearPen, bounds.Left, y, bounds.Right, y);
        }
    }

    private void DrawMovingTrack(Graphics g, Rectangle bounds, Color rose, Color pale, Color cold, Color dim, Color hot, double time)
    {
        var centerX = bounds.Left + bounds.Width * 0.5f;
        var horizonY = bounds.Top + bounds.Height * 0.24f;
        var bottomY = bounds.Bottom - 9.0f;
        var farHalf = bounds.Width * 0.18f;
        var nearHalf = bounds.Width * 0.50f;
        var speed = 0.62 + _trackProgress * 1.65 + _lastBeat * 0.65;
        var scroll = (time * speed) % 1.0;

        using var horizonPen = new Pen(Color.FromArgb(74, cold), 1);
        using var railPen = new Pen(Color.FromArgb(112, rose), 1);
        using var railHotPen = new Pen(Color.FromArgb(135, pale), 1);
        using var gridPen = new Pen(Color.FromArgb(58, cold), 1);
        using var pulsePen = new Pen(Color.FromArgb(80 + (int)(_lastBeat * 90), hot), 1);

        var leftNear = centerX - nearHalf;
        var rightNear = centerX + nearHalf;
        var leftFar = centerX - farHalf;
        var rightFar = centerX + farHalf;

        g.DrawLine(horizonPen, leftFar, horizonY, rightFar, horizonY);
        g.DrawLine(railHotPen, leftFar, horizonY, leftNear, bottomY);
        g.DrawLine(railHotPen, rightFar, horizonY, rightNear, bottomY);

        for (var lane = -3; lane <= 3; lane++)
        {
            var laneNorm = lane / 3.0f;
            var farX = centerX + laneNorm * farHalf * 0.82f;
            var nearX = centerX + laneNorm * nearHalf * 0.92f;
            g.DrawLine(lane == 0 ? railPen : gridPen, farX, horizonY, nearX, bottomY);
        }

        for (var i = 0; i < 18; i++)
        {
            var raw = (i + scroll) / 18.0;
            var t = Math.Clamp(raw, 0.0, 1.0);
            var curve = Math.Pow(t, 1.85);
            var y = Lerp(horizonY, bottomY, curve);
            var half = Lerp(farHalf, nearHalf, Math.Pow(t, 1.28));
            var alpha = Math.Clamp((int)(30 + t * 115), 26, 145);

            using var pen = new Pen(Color.FromArgb(alpha, i % 3 == 0 ? pale : cold), t > 0.78 ? 2 : 1);
            g.DrawLine(pen, (float)(centerX - half), (float)y, (float)(centerX + half), (float)y);

            if (t > 0.68 && i % 2 == 0)
            {
                using var sidePen = new Pen(Color.FromArgb(alpha / 2, dim), 1);
                g.DrawLine(sidePen, (float)(centerX - half), (float)y, (float)(centerX - half - 18 * t), (float)(y + 11 * t));
                g.DrawLine(sidePen, (float)(centerX + half), (float)y, (float)(centerX + half + 18 * t), (float)(y + 11 * t));
            }
        }

        for (var lane = -2; lane <= 2; lane++)
        {
            var laneNorm = lane / 2.75f;
            for (var i = 0; i < 12; i++)
            {
                var t1 = (i + scroll) / 12.0;
                var t2 = Math.Min(1.0, t1 + 0.055);
                if (t1 < 0.02 || t1 > 0.98)
                    continue;

                var c1 = Math.Pow(t1, 1.72);
                var c2 = Math.Pow(t2, 1.72);
                var y1 = Lerp(horizonY, bottomY, c1);
                var y2 = Lerp(horizonY, bottomY, c2);
                var half1 = Lerp(farHalf, nearHalf, Math.Pow(t1, 1.23));
                var half2 = Lerp(farHalf, nearHalf, Math.Pow(t2, 1.23));
                var x1 = centerX + laneNorm * half1;
                var x2 = centerX + laneNorm * half2;
                g.DrawLine(lane == 0 ? pulsePen : gridPen, (float)x1, (float)y1, (float)x2, (float)y2);
            }
        }
    }

    private void DrawBillboards(Graphics g, Rectangle bounds, Brush roseBrush, Brush coldBrush, Brush dimBrush, Brush hotBrush, double time)
    {
        var signs = new[]
        {
            "SECTOR 07",
            "SYNC LOCK",
            "VECTOR 88",
            "GATE A-9",
            "AKIRA BUS",
            "CARMACK 3D",
            "RUNWAY 404",
            "NO COVER"
        };

        for (var i = 0; i < 3; i++)
        {
            var t = ((time * (0.18 + i * 0.07) + i * 0.31) % 1.0);
            var y = bounds.Top + (int)Lerp(bounds.Height * 0.24, bounds.Height * 0.84, Math.Pow(t, 1.85));
            var side = i % 2 == 0 ? -1 : 1;
            var x = side < 0
                ? bounds.Left + (int)Lerp(bounds.Width * 0.09, bounds.Width * 0.02, t)
                : bounds.Right - (int)Lerp(bounds.Width * 0.36, bounds.Width * 0.46, t);
            var label = signs[(i + (int)(time * 0.5)) % signs.Length];
            var brush = i == 1 ? hotBrush : i == 2 ? coldBrush : roseBrush;
            var rect = new Rectangle(x - 4, y - 2, Math.Max(72, label.Length * 8 + 8), 18);

            using var pen = new Pen(i == 1 ? Color.FromArgb(130, Color.Goldenrod) : Color.FromArgb(110, Color.DeepPink), 1);
            using var fill = new SolidBrush(Color.FromArgb(42, Color.Black));
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
            DrawText(g, _billboardFont, x, y - 1, label, brush);
        }
    }

    private void DrawSideObjects(Graphics g, Rectangle bounds, int cellW, int cellH, Brush roseBrush, Brush dimBrush, Brush hotBrush)
    {
        foreach (var side in _sideStreaks)
        {
            var sx = bounds.Left + (int)(side.X * bounds.Width);
            var sy = bounds.Top + (int)(side.Y * bounds.Height);
            var scale = 1.0 + side.Y * 2.7;
            var brush = side.Kind == 3 ? hotBrush : side.Side < 0 ? dimBrush : roseBrush;
            var sprite = GetSideSprite(side.Kind);

            DrawSprite(g, _entityFont, sprite, sx, sy, (int)Math.Clamp(scale, 1.0, 4.0), side.Side, brush);
        }
    }

    private void DrawEntities(Graphics g, Rectangle bounds, Brush roseBrush, Brush dimBrush, Brush hotBrush)
    {
        foreach (var entity in _entities)
        {
            var x = bounds.Left + (int)(entity.X * bounds.Width);
            var y = bounds.Top + (int)(entity.Y * bounds.Height);
            var brush = entity.Kind == ArcadeEntityKind.Enemy ? roseBrush : entity.Kind == ArcadeEntityKind.Wall ? hotBrush : dimBrush;

            if (entity.Kind == ArcadeEntityKind.Enemy)
                DrawSprite(g, _entityFont, new[] { "╱╳╲", " Ж ", "╲╳╱" }, x, y, 1, 1, brush);
            else if (entity.Kind == ArcadeEntityKind.Wall)
                DrawSprite(g, _entityFont, new[] { "▓▓▓", "╬╬╬" }, x, y, 1, 1, brush);
            else
                DrawText(g, _entityFont, x, y, entity.Glyph, brush);
        }
    }

    private void DrawBullets(Graphics g, Rectangle bounds, Brush paleBrush, Brush hotBrush)
    {
        foreach (var bullet in _bullets)
        {
            var x = bounds.Left + (int)(bullet.X * bounds.Width);
            var y = bounds.Top + (int)(bullet.Y * bounds.Height);
            DrawText(g, _entityFont, x, y, bullet.Glyph, bullet.Vx == 0 ? paleBrush : hotBrush);
        }
    }

    private void DrawPlayer(Graphics g, Rectangle bounds, Color rose, Color pale, Color hot, Color bad, Brush paleBrush, Brush hotBrush, Brush badBrush)
    {
        var px = bounds.Left + (int)(_playerX * bounds.Width);
        var py = bounds.Top + (int)(_playerY * bounds.Height);
        var damage = _damageFlash > 0.01;
        var boost = _boostFlash > 0.01;
        var mainColor = damage ? bad : boost ? hot : pale;

        using var glow = new SolidBrush(Color.FromArgb(boost ? 150 : 90, rose));
        using var shadow = new SolidBrush(Color.FromArgb(160, Color.Black));
        using var main = new SolidBrush(mainColor);
        using var trail = new SolidBrush(Color.FromArgb(boost ? 180 : 95, boost ? hot : rose));

        g.DrawString(ShipFallbackGlyph, _shipFallbackFont, shadow, px - 18, py - 17, StringFormat.GenericTypographic);
        g.DrawString(ShipGlyph, _shipFont, glow, px - 19, py - 25, StringFormat.GenericTypographic);
        g.DrawString(ShipGlyph, _shipFont, main, px - 21, py - 27, StringFormat.GenericTypographic);

        DrawText(g, _entityFont, px - 10, py + 9, boost ? "▓▓▓" : "▒▒", trail);
        DrawText(g, _entityFont, px - 6, py + 18, boost ? "▒▒" : "░", trail);

        if (_fireHeld)
            DrawText(g, _entityFont, px - 2, py - 26, "┃", hotBrush);
    }

    private void DrawOverlayHud(Graphics g, Rectangle bounds, Brush roseBrush, Brush hotBrush, Brush badBrush, Brush dimBrush)
    {
        DrawText(g, _billboardFont, bounds.Left + 7, bounds.Top + 4, "SYNC RUN // NUMPAD", dimBrush);
        DrawText(g, _billboardFont, bounds.Left + 7, bounds.Top + 22, $"HP {_hp}  SCORE {_score:00000}  COMBO x{Math.Max(1, _combo)}", _hp <= 1 ? badBrush : hotBrush);
        DrawText(g, _billboardFont, bounds.Right - 118, bounds.Top + 4, $"DIF {_trackProgress * 100:00}%", roseBrush);
    }

    private static string[] GetSideSprite(int kind)
    {
        return kind switch
        {
            0 => new[] { "╱╲", "██", "██", "██" },
            1 => new[] { "▗▄", "██", "▀█" },
            2 => new[] { "╔╬╗", " ║ ", " ║ ", "╚╩╝" },
            3 => new[] { " ▲ ", "███", " █ " },
            _ => new[] { "╱", "╱", "╱" }
        };
    }

    private static void DrawSprite(Graphics g, Font font, string[] sprite, int centerX, int y, int scale, int side, Brush brush)
    {
        var glyphW = Math.Max(5, 5 * scale);
        var glyphH = Math.Max(7, 7 * scale);
        var maxWidth = sprite.Max(s => s.Length);
        var startX = centerX - maxWidth * glyphW / 2;

        if (side < 0)
            startX = centerX;
        else if (side > 0)
            startX = centerX - maxWidth * glyphW;

        for (var row = 0; row < sprite.Length; row++)
        {
            for (var col = 0; col < sprite[row].Length; col++)
            {
                var c = sprite[row][col];
                if (c == ' ')
                    continue;

                g.DrawString(c.ToString(), font, brush, startX + col * glyphW, y + row * glyphH, StringFormat.GenericTypographic);
            }
        }
    }

    private void Shoot(bool spread)
    {
        _bullets.Add(new ArcadeEntity(_playerX, _playerY - 0.08, 0.0, -1.80, ArcadeEntityKind.Bullet, "┃"));

        if (spread)
        {
            _bullets.Add(new ArcadeEntity(_playerX - 0.030, _playerY - 0.06, -0.24, -1.58, ArcadeEntityKind.Bullet, "╱"));
            _bullets.Add(new ArcadeEntity(_playerX + 0.030, _playerY - 0.06, 0.24, -1.58, ArcadeEntityKind.Bullet, "╲"));
        }
    }

    private void UpdateBullets(double dt)
    {
        for (var i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.X += b.Vx * dt;
            b.Y += b.Vy * dt;

            if (b.Y < -0.08 || b.X < -0.1 || b.X > 1.1)
                _bullets.RemoveAt(i);
        }
    }

    private void UpdateEntities(double dt, double difficulty, double beat)
    {
        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            var e = _entities[i];
            e.Y += e.Vy * dt * (1.0 + beat * 0.18);
            e.X += e.Vx * dt;
            e.Wobble += dt;

            if (e.Kind == ArcadeEntityKind.Enemy)
                e.X += Math.Sin(e.Wobble * 5.0 + e.Seed) * dt * 0.05 * difficulty;

            if (e.Y > 1.08 || e.X < -0.1 || e.X > 1.1)
                _entities.RemoveAt(i);
        }
    }

    private void UpdateSideStreaks(double dt, double difficulty, double beat)
    {
        for (var i = _sideStreaks.Count - 1; i >= 0; i--)
        {
            var s = _sideStreaks[i];
            s.Y += dt * (0.75 + difficulty * 0.86 + beat * 0.58);
            s.X += s.Side * dt * (0.18 + s.Y * 1.08);

            if (s.Y > 1.1 || s.X < -0.2 || s.X > 1.2)
                _sideStreaks.RemoveAt(i);
        }
    }

    private void SpawnEntities(double dt, double difficulty, double progress, double beat)
    {
        _spawnTimer -= dt * (1.0 + beat * 0.55);
        if (_spawnTimer > 0.0)
            return;

        var minInterval = Lerp(0.82, 0.15, progress);
        _spawnTimer = Math.Max(0.09, minInterval / Math.Max(0.5, difficulty));

        var roll = _random.NextDouble();
        var x = 0.16 + _random.NextDouble() * 0.68;
        var speed = Lerp(0.22, 1.25, progress) * (0.85 + _random.NextDouble() * 0.45);

        if (roll < 0.52)
            _entities.Add(new ArcadeEntity(x, -0.04, (_random.NextDouble() - 0.5) * 0.08, speed, ArcadeEntityKind.Obstacle, _random.NextDouble() < 0.5 ? "◆" : "╬"));
        else if (roll < 0.88)
            _entities.Add(new ArcadeEntity(x, -0.04, (_random.NextDouble() - 0.5) * 0.16, speed * 0.82, ArcadeEntityKind.Enemy, _random.NextDouble() < 0.5 ? "Ж" : "◇"));
        else
        {
            var wallX = _random.NextDouble() < 0.5 ? 0.28 : 0.68;
            for (var i = 0; i < 4; i++)
                _entities.Add(new ArcadeEntity(wallX + i * 0.035, -0.05 - i * 0.035, 0.0, speed * 0.94, ArcadeEntityKind.Wall, "▓"));
        }
    }

    private void SpawnSideStreaks(double dt, double difficulty, double beat)
    {
        _sideSpawnTimer -= dt * (1.0 + beat * 0.8);
        if (_sideSpawnTimer > 0.0)
            return;

        _sideSpawnTimer = Math.Max(0.022, 0.13 / Math.Max(0.7, difficulty));
        var side = _random.NextDouble() < 0.5 ? -1 : 1;
        var x = side < 0 ? 0.02 + _random.NextDouble() * 0.10 : 0.88 + _random.NextDouble() * 0.10;
        _sideStreaks.Add(new SideStreak(x, -0.08, side, _random.Next(0, 5)));
    }

    private void Collide()
    {
        for (var b = _bullets.Count - 1; b >= 0; b--)
        {
            for (var e = _entities.Count - 1; e >= 0; e--)
            {
                if (_entities[e].Kind == ArcadeEntityKind.Wall)
                    continue;

                if (DistanceSq(_bullets[b].X, _bullets[b].Y, _entities[e].X, _entities[e].Y) > 0.0018)
                    continue;

                _entities.RemoveAt(e);
                _bullets.RemoveAt(b);
                _combo++;
                _score += 25 * Math.Max(1, _combo);
                break;
            }
        }

        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            if (DistanceSq(_playerX, _playerY, _entities[i].X, _entities[i].Y) > 0.004)
                continue;

            _entities.RemoveAt(i);
            _hp--;
            _combo = 0;
            _damageFlash = 0.42;
        }
    }

    private static void DrawText(Graphics g, Font font, int x, int y, string text, Brush brush)
    {
        g.DrawString(text, font, brush, x, y, StringFormat.GenericTypographic);
    }

    private static double DistanceSq(double ax, double ay, double bx, double by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return dx * dx + dy * dy;
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * Math.Clamp(t, 0.0, 1.0);
    }

    private sealed class ArcadeEntity
    {
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Wobble;
        public readonly int Seed;
        public readonly ArcadeEntityKind Kind;
        public readonly string Glyph;

        public ArcadeEntity(double x, double y, double vx, double vy, ArcadeEntityKind kind, string glyph)
        {
            X = x;
            Y = y;
            Vx = vx;
            Vy = vy;
            Kind = kind;
            Glyph = glyph;
            Seed = unchecked((int)(x * 100000 + y * 1000 + Environment.TickCount64));
        }
    }

    private sealed class SideStreak
    {
        public double X;
        public double Y;
        public readonly int Side;
        public readonly int Kind;

        public SideStreak(double x, double y, int side, int kind)
        {
            X = x;
            Y = y;
            Side = side;
            Kind = kind;
        }
    }

    private enum ArcadeEntityKind
    {
        Obstacle,
        Enemy,
        Wall,
        Bullet
    }
}
