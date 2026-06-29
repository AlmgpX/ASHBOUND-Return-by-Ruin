using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Editor;
using OUT_RayMicro.Gameplay;
using OUT_RayMicro.Input;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Runtime;

public static class OutmApp
{
    public static void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1280, 720, "OUT RayMicro // Quake-room seed");
        Raylib.SetTargetFPS(120);
        Raylib.DisableCursor();

        var world = new OutmWorld();
        var map = OutmDemoMap.CreateQuakeRoom();
        var camera = new OutmCameraMotor(map.PlayerStart);
        var weapons = new OutmWeaponSystem();
        var editor = new OutmEditorShell();
        var inputSampler = new OutmInputSampler();

        world.PushLog("OUT RayMicro boot");
        world.PushLog("raylib host online");
        world.PushLog("one room, one gun, one door; civilization trembles");

        bool wasInTrigger = false;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Math.Clamp(Raylib.GetFrameTime(), 0.0f, 0.05f);
            world.BeginFrame(dt);
            OutmInputFrame input = inputSampler.Sample(dt);

            editor.Update(world, input);
            camera.Update(input, map);

            bool inTrigger = map.IntersectsTrigger(camera.Position);
            if (inTrigger && !wasInTrigger)
            {
                map.DoorOpen = !map.DoorOpen;
                world.Emit(new OutmEvent(OutmEventType.TriggerEntered, EntityId.None, EntityId.None, camera.Position, map.DoorOpen ? 1 : 0, "door trigger"));
                world.Emit(new OutmEvent(OutmEventType.DoorToggled, EntityId.None, EntityId.None, camera.Position, map.DoorOpen ? 1 : 0, map.DoorOpen ? "door opened" : "door closed"));
            }
            wasInTrigger = inTrigger;

            Vector3 muzzle = camera.Position + new Vector3(0, -0.08f, 0) + camera.Right * 0.22f;
            weapons.Update(input, muzzle, camera.Forward, map, world);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(6, 8, 12, 255));

            Raylib.BeginMode3D(camera.ToRayCamera());
            Raylib.DrawGrid(18, 1.0f);
            map.Draw();
            weapons.Draw();
            DrawViewRay(camera);
            Raylib.EndMode3D();

            DrawWeaponHud();
            editor.Draw(world, camera, map);

            Raylib.EndDrawing();
        }

        Raylib.EnableCursor();
        Raylib.CloseWindow();
    }

    private static void DrawViewRay(OutmCameraMotor camera)
    {
        Vector3 start = camera.Position + camera.Forward * 0.4f;
        Vector3 end = camera.Position + camera.Forward * 2.0f;
        Raylib.DrawLine3D(start, end, new Color(255, 220, 80, 220));
    }

    private static void DrawWeaponHud()
    {
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(w - 245, h - 82, 230, 62, new Color(0, 0, 0, 160));
        Raylib.DrawRectangleLines(w - 245, h - 82, Color.Orange);
        Raylib.DrawText("REVOLVER // PROJECTILE", w - 232, h - 72, 14, Color.Orange);
        Raylib.DrawText("LMB: physical shot", w - 232, h - 50, 12, Color.LightGray);
    }
}
