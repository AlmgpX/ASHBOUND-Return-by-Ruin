using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Runtime;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Editor;

public sealed class OutmEditorShell
{
    public bool Visible = true;

    public void Update(OutmWorld world)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
        {
            Visible = !Visible;
            world.PushLog(Visible ? "editor overlay on" : "editor overlay off");
        }
    }

    public void Draw(OutmWorld world, OutmCameraMotor camera, OutmDemoMap map)
    {
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();
        Raylib.DrawText("+", w / 2 - 4, h / 2 - 8, 20, Color.White);

        if (!Visible)
            return;

        Raylib.DrawRectangle(10, 10, 440, 230, new Color(0, 0, 0, 170));
        Raylib.DrawRectangleLines(10, 10, 440, 230, Color.Cyan);
        Raylib.DrawText("OUT RAYMICRO // M0-M1 SEED", 22, 20, 18, Color.Cyan);
        Raylib.DrawText("WASD move  MOUSE look  LMB fire  F1 overlay", 22, 46, 14, Color.LightGray);
        Raylib.DrawText($"POS {camera.Position.X:0.00}, {camera.Position.Y:0.00}, {camera.Position.Z:0.00}", 22, 70, 14, Color.Yellow);
        Raylib.DrawText($"DOOR {(map.DoorOpen ? "OPEN" : "CLOSED")}", 22, 90, 14, map.DoorOpen ? Color.Green : Color.Orange);

        for (int i = 0; i < 8; i++)
        {
            string line = world.GetLogLineFromNewest(i);
            if (!string.IsNullOrWhiteSpace(line))
                Raylib.DrawText(line, 22, 120 + i * 14, 12, Color.LightGray);
        }
    }
}
