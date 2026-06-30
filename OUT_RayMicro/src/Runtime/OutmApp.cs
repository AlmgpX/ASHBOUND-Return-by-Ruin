using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Content;
using OUT_RayMicro.Core;
using OUT_RayMicro.Editor;
using OUT_RayMicro.Gameplay;
using OUT_RayMicro.Input;
using OUT_RayMicro.Physics;
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
        OutmFontSystem.Load();

        var content = OutmContentRegistry.LoadDefault();
        var world = new OutmWorld();
        var map = OutmDemoMap.CreateQuakeRoom();
        IOutmCollisionWorld collision = new OutmDemoCollisionWorld(map);
        var camera = new OutmCameraMotor(map.PlayerStart);
        var weapons = new OutmWeaponSystem(content.GetWeapon("weapon.revolver"));
        var editor = new OutmEditorShell();
        var inputSampler = new OutmInputSampler();
        var audio = new OutmAudioSystem();
        audio.Load(world);

        world.PushLog("OUT RayMicro boot");
        world.PushLog($"defs: weapons {content.Weapons.Count}");
        world.PushLog($"collision backend: {collision.BackendKind}");
        world.PushLog(OutmFontSystem.IsLoaded ? "unicode HUD font online" : "unicode HUD font missing");

        bool wasInTrigger = false;
        float stepTimer = 0.0f;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Math.Clamp(Raylib.GetFrameTime(), 0.0f, 0.05f);
            world.BeginFrame(dt);
            OutmInputFrame input = inputSampler.Sample(dt);
            collision.Step(dt);

            editor.Update(world, input);
            if (!world.PlayerVitals.IsDead)
            {
                camera.Update(input, collision);
                UpdateFootsteps(world, camera, input, dt, ref stepTimer);
            }

            bool inTrigger = map.IntersectsTrigger(camera.Position);
            if (!world.PlayerVitals.IsDead && inTrigger && !wasInTrigger)
            {
                map.DoorOpen = !map.DoorOpen;
                world.Emit(new OutmEvent(OutmEventType.TriggerEntered, EntityId.None, EntityId.None, camera.Position, map.DoorOpen ? 1 : 0, "door trigger"));
                world.Emit(new OutmEvent(OutmEventType.DoorToggled, EntityId.None, EntityId.None, camera.Position, map.DoorOpen ? 1 : 0, map.DoorOpen ? "door opened" : "door closed"));
            }
            wasInTrigger = inTrigger;

            Vector3 muzzle = camera.Position + new Vector3(0, -0.08f, 0) + camera.Right * 0.22f;
            if (!world.PlayerVitals.IsDead)
                weapons.Update(input, muzzle, camera.Forward, collision, world);
            audio.ProcessEvents(world, camera.Position, camera.Right);
            audio.Update();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(6, 8, 12, 255));

            Raylib.BeginMode3D(camera.ToRayCamera());
            Raylib.DrawGrid(18, 1.0f);
            map.Draw();
            weapons.Draw();
            DrawViewRay(camera);
            Raylib.EndMode3D();

            DrawWeaponHud(world);
            editor.Draw(world, camera, map);

            Raylib.EndDrawing();
        }

        audio.Unload();
        OutmFontSystem.Unload();
        Raylib.EnableCursor();
        Raylib.CloseWindow();
    }

    private static void UpdateFootsteps(OutmWorld world, OutmCameraMotor camera, in OutmInputFrame input, float dt, ref float stepTimer)
    {
        float speed = camera.HorizontalSpeed;
        if (!camera.Grounded || speed < 1.2f)
        {
            stepTimer = 0.0f;
            return;
        }

        stepTimer -= dt;
        if (stepTimer > 0.0f)
            return;

        world.Emit(new OutmEvent(OutmEventType.Footstep, EntityId.None, EntityId.None, camera.Position, speed, "footstep stone"));

        if (camera.IsCrouching)
            stepTimer = 0.56f;
        else if (input.IsDown(OutmButtons.Sprint))
            stepTimer = 0.28f;
        else
            stepTimer = 0.38f;
    }

    private static void DrawViewRay(OutmCameraMotor camera)
    {
        Vector3 start = camera.Position + camera.Forward * 0.4f;
        Vector3 end = camera.Position + camera.Forward * 2.0f;
        Raylib.DrawLine3D(start, end, new Color(255, 220, 80, 220));
    }

    private static void DrawWeaponHud(OutmWorld world)
    {
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(w - 245, h - 82, 230, 62, new Color(0, 0, 0, 160));
        bool dead = world.PlayerVitals.IsDead;
        Raylib.DrawRectangleLines(w - 245, h - 82, 230, 62, dead ? Color.Red : Color.Orange);
        OutmFontSystem.DrawText(dead ? "PLAYER // DEAD" : "REVOLVER // PROJECTILE", w - 232, h - 72, 14, dead ? Color.Red : Color.Orange);
        OutmFontSystem.DrawText(dead ? "input locked" : "LMB: physical shot", w - 232, h - 50, 12, Color.LightGray);
    }
}
