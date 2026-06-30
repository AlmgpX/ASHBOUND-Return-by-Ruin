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
        Raylib.InitWindow(1280, 720, "OUT RayMicro // outmap seed");
        Raylib.SetTargetFPS(120);
        Raylib.DisableCursor();
        OutmFontSystem.Load();

        var content = OutmContentRegistry.LoadDefault();
        var world = new OutmWorld();
        OutmMapDef mapDef = OutmMapLoader.LoadOrDefault("maps/test_room.outmap.json");
        var map = OutmMapLoader.BuildDemoMap(mapDef);
        IOutmCollisionWorld collision = new OutmDemoCollisionWorld(map);
        var camera = new OutmCameraMotor(map.PlayerStart);
        var weapons = new OutmWeaponSystem(content.GetWeapon("weapon.revolver"));
        var editor = new OutmEditorShell();
        var inputSampler = new OutmInputSampler();
        var commands = new OutmCommandQueue();
        var fixedStep = new OutmFixedStep();
        var audio = new OutmAudioSystem();
        audio.Load(world);

        world.PushLog("OUT RayMicro boot");
        world.PushLog($"map: {map.DisplayName}");
        world.PushLog($"defs: weapons {content.Weapons.Count}");
        world.PushLog($"fixed tick: {1.0f / fixedStep.FixedDelta:0} hz");
        world.PushLog($"collision backend: {collision.BackendKind}");
        world.PushLog(OutmFontSystem.IsLoaded ? "unicode HUD font online" : "unicode HUD font missing");

        string currentTriggerId = "";
        float stepTimer = 0.0f;
        OutmButtons bufferedPressed = OutmButtons.None;
        OutmButtons bufferedReleased = OutmButtons.None;
        Vector2 bufferedLook = Vector2.Zero;

        while (!Raylib.WindowShouldClose())
        {
            float frameDt = Math.Clamp(Raylib.GetFrameTime(), 0.0f, 0.10f);
            OutmInputFrame sampledInput = inputSampler.Sample(frameDt);
            editor.Update(world, sampledInput);

            // Fixed tick can skip a render frame when the accumulator has not reached 1/60 yet.
            // Edge input must survive that gap, otherwise short Space taps vanish like dignity in a rush build.
            bufferedPressed |= sampledInput.Pressed;
            bufferedReleased |= sampledInput.Released;
            bufferedLook += sampledInput.LookDelta;

            fixedStep.AddFrameTime(frameDt);

            int ticksThisFrame = 0;
            while (ticksThisFrame < OutmFixedStep.MaxTicksPerRenderFrame && fixedStep.TryConsumeTick(out int simTick))
            {
                OutmButtons pressed = bufferedPressed;
                OutmButtons released = bufferedReleased;
                Vector2 look = bufferedLook;

                bufferedPressed = OutmButtons.None;
                bufferedReleased = OutmButtons.None;
                bufferedLook = Vector2.Zero;

                var userCommand = new OutmUserCommand(
                    sampledInput.Sequence,
                    simTick,
                    fixedStep.FixedDelta,
                    sampledInput.Move,
                    look,
                    sampledInput.Down,
                    pressed,
                    released);

                commands.Enqueue(new OutmCommand(OutmCommandType.UserInput, userCommand));
                SimulateFixedTick(world, map, collision, camera, weapons, commands, fixedStep.FixedDelta, ref currentTriggerId, ref stepTimer);
                ticksThisFrame++;
            }

            if (ticksThisFrame >= OutmFixedStep.MaxTicksPerRenderFrame)
                fixedStep.ClampAfterSpiralLimit();

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

    private static void SimulateFixedTick(
        OutmWorld world,
        OutmDemoMap map,
        IOutmCollisionWorld collision,
        OutmCameraMotor camera,
        OutmWeaponSystem weapons,
        OutmCommandQueue commands,
        float fixedDt,
        ref string currentTriggerId,
        ref float stepTimer)
    {
        world.BeginFrame(fixedDt);
        collision.Step(fixedDt);

        while (commands.TryDequeue(out OutmCommand command))
        {
            if (command.Type != OutmCommandType.UserInput)
                continue;

            OutmInputFrame input = command.User.ToInputFrame();
            if (!world.PlayerVitals.IsDead)
            {
                camera.Update(input, collision);
                UpdateFootsteps(world, camera, input, fixedDt, ref stepTimer);
            }

            HandleTriggers(world, map, camera.Position, ref currentTriggerId);

            if (!world.PlayerVitals.IsDead)
            {
                Vector3 muzzle = camera.Position + new Vector3(0, -0.08f, 0) + camera.Right * 0.22f;
                weapons.Update(input, muzzle, camera.Forward, collision, world);
            }
        }
    }

    private static void HandleTriggers(OutmWorld world, OutmDemoMap map, Vector3 position, ref string currentTriggerId)
    {
        if (world.PlayerVitals.IsDead)
            return;

        if (!map.TryGetEnteredTrigger(position, out OutmTriggerRuntime trigger))
        {
            currentTriggerId = "";
            return;
        }

        if (string.Equals(currentTriggerId, trigger.Id, StringComparison.OrdinalIgnoreCase))
            return;

        currentTriggerId = trigger.Id;
        world.Emit(new OutmEvent(OutmEventType.TriggerEntered, EntityId.None, EntityId.None, position, 0, trigger.Id));

        if (!string.Equals(trigger.Kind, "door_toggle", StringComparison.OrdinalIgnoreCase))
            return;

        bool open = map.TryToggleDoor(trigger.Target);
        world.Emit(new OutmEvent(OutmEventType.DoorToggled, EntityId.None, EntityId.None, position, open ? 1 : 0, open ? "door opened" : "door closed"));
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
