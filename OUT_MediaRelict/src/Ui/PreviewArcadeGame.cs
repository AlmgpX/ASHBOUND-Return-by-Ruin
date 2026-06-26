using System.Drawing;
using System.Drawing.Drawing2D;

namespace MediaRelic.Ui;

internal sealed class PreviewArcadeGame
{
    private readonly Random _random = new(0xA51B0D);
    private readonly List<ArcadeEntity> _entities = new();
    private readonly List<ArcadeEntity> _bullets = new();
    private readonly List<SideStreak> _sideStreaks = new();

    private long _lastTick;
    private double _spawnTimer;
    private double _sideSpawnTimer;
    private double _fireCooldown;
    private double _damageFlash;
    private double _boostFlash;
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

        using var bg = new SolidBrush(Color.FromArgb(214, 2, 4, 7));
        using var gridPen = new Pen(Color.FromArgb(36, cold), 1);
        using var rosePen = new Pen(Color.FromArgb(72, rose), 1);
        using var coldBrush = new SolidBrush(cold);
        using var paleBrush = new SolidBrush(pale);
        using var roseBrush = new SolidBrush(rose);
        using var dimBrush = new SolidBrush(Color.FromArgb(150, dim));
        using var hotBrush = new SolidBrush(hot);
        using var badBrush = new SolidBrush(bad);

        g.FillRectangle(bg, bounds);

        var cellW = Math.Max(4, bounds.Width / 96);
        var cellH = Math.Max(4, bounds.Height / 96);
        var time = Environment.TickCount64 / 1000.0;
        var horizonY = bounds.Top + bounds.Height * 0.15f;
        var centerX = bounds.Left + bounds.Width * 0.5f;

        for (var i = 0; i < 13; i++)
        {
            var t = i / 12.0f;
            var y = horizonY + t * t * bounds.Height * 0.88f;
            var half = 14 + t * bounds.Width * 0.55f;
            g.DrawLine(gridPen, centerX - half, y, centerX + half, y);
        }

        for (var i = -4; i <= 4; i++)
        {
            var x = centerX + i * bounds.Width * 0.08f;
            g.DrawLine(rosePen, centerX, horizonY, x, bounds.Bottom);
        }

        DrawText(g, font, bounds.Left + 7, bounds.Top + 4, "SYNC RUN // NUMPAD", dimBrush);
        DrawText(g, font, bounds.Left + 7, bounds.Top + 19, $"HP {_hp}  SCORE {_score:00000}  COMBO x{Math.Max(1, _combo)}", _hp <= 1 ? badBrush : hotBrush);

        foreach (var side in _sideStreaks)
        {
            var sx = bounds.Left + (int)(side.X * bounds.Width);
            var sy = bounds.Top + (int)(side.Y * bounds.Height);
            var scale = 1.0 + side.Y * 2.4;
            var glyph = side.Kind switch
            {
                0 => "▌",
                1 => "▓",
                2 => "╫",
                3 => "▲",
                _ => "╱"
            };

            var brush = side.Side < 0 ? dimBrush : roseBrush;
            DrawText(g, font, sx, sy, glyph, brush);

            if (scale > 2.2)
                DrawText(g, font, sx + side.Side * cellW, sy + cellH, glyph, brush);
        }

        foreach (var entity in _entities)
        {
            var brush = entity.Kind == ArcadeEntityKind.Enemy ? roseBrush : entity.Kind == ArcadeEntityKind.Wall ? hotBrush : dimBrush;
            DrawText(g, font, bounds.Left + (int)(entity.X * bounds.Width), bounds.Top + (int)(entity.Y * bounds.Height), entity.Glyph, brush);
        }

        foreach (var bullet in _bullets)
            DrawText(g, font, bounds.Left + (int)(bullet.X * bounds.Width), bounds.Top + (int)(bullet.Y * bounds.Height), bullet.Glyph, paleBrush);

        var px = bounds.Left + (int)(_playerX * bounds.Width);
        var py = bounds.Top + (int)(_playerY * bounds.Height);
        var playerBrush = _damageFlash > 0.01 ? badBrush : _boostFlash > 0.01 ? hotBrush : paleBrush;
        DrawText(g, font, px - cellW, py, _boostFlash > 0.01 ? "⟟" : "⟁", playerBrush);
        DrawText(g, font, px - cellW, py + cellH, _fireHeld ? "╹" : "▲", playerBrush);

        if (_hp <= 0)
        {
            using var overlay = new SolidBrush(Color.FromArgb(140, Color.Black));
            g.FillRectangle(overlay, bounds);
            DrawText(g, font, bounds.Left + bounds.Width / 2 - 58, bounds.Top + bounds.Height / 2 - 8, "SIGNAL LOST", badBrush);
        }

        if ((int)(time * 8) % 13 == 0)
            g.DrawLine(rosePen, bounds.Left, bounds.Top + (int)((time * 71) % bounds.Height), bounds.Right, bounds.Top + (int)((time * 71) % bounds.Height));
    }

    private void Shoot(bool spread)
    {
        _bullets.Add(new ArcadeEntity(_playerX, _playerY - 0.05, 0.0, -1.60, ArcadeEntityKind.Bullet, "│"));

        if (spread)
        {
            _bullets.Add(new ArcadeEntity(_playerX - 0.025, _playerY - 0.04, -0.22, -1.45, ArcadeEntityKind.Bullet, "╱"));
            _bullets.Add(new ArcadeEntity(_playerX + 0.025, _playerY - 0.04, 0.22, -1.45, ArcadeEntityKind.Bullet, "╲"));
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
            s.Y += dt * (0.75 + difficulty * 0.70 + beat * 0.45);
            s.X += s.Side * dt * (0.18 + s.Y * 0.92);

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

        _sideSpawnTimer = Math.Max(0.025, 0.16 / Math.Max(0.7, difficulty));
        var side = _random.NextDouble() < 0.5 ? -1 : 1;
        var x = side < 0 ? 0.04 + _random.NextDouble() * 0.12 : 0.84 + _random.NextDouble() * 0.12;
        _sideStreaks.Add(new SideStreak(x, -0.05, side, _random.Next(0, 5)));
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
