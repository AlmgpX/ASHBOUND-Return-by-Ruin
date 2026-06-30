using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Content;
using OUT_RayMicro.Core;
using OUT_RayMicro.Editor;
using OUT_RayMicro.Gameplay;
using OUT_RayMicro.Input;
using OUT_RayMicro.Physics;
using OUT_RayMicro.Render;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Runtime;

public static class OutmApp
{
    public static void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1280, 720, "OUT CORE // GLB scene seed");
        Raylib.SetTargetFPS(120);
        Raylib.DisableCursor();
        OutmFontSystem.Load();

        var content = OutmContentRegistry.LoadDefault();
        var world = new OutmWorld();
        world.PlayerEntity = world.Entities.Create(OutmEntityKind.Player, "player.local");
        OutmMapManifest manifest = OutmMapManifestLoader.LoadOrDefault();
        OutmMapManifestEntry mapEntry = manifest.FindDefault();
        OutmMapDef mapDef = OutmMapLoader.LoadOrDefault(mapEntry.Path);
        OutmMapValidationReport validation = OutmMapValidator.Validate(mapDef, world.PushLog);
        var map = OutmMapLoader.BuildDemoMap(mapDef);
        IOutmCollisionWorld collision = new OutmDemoCollisionWorld(map);
        var camera = new OutmCameraMotor(map.PlayerStart);
        world.Transforms.Set(world.PlayerEntity, camera.Position, new Vector3(0.0f, camera.Yaw, camera.Pitch));
        var logicTicks = new OutmLogicTickScheduler();
        var weapons = new OutmWeaponSystem(content.GetWeapon("weapon.revolver"));
        var use = new OutmUseSystem();
        var triggers = new OutmTriggerSystem(use);
        var editor = new OutmEditorShell();
        var inputSampler = new OutmInputSampler();
        var commands = new OutmCommandQueue();
        var fixedStep = new OutmFixedStep();
        var modelCache = new OutmModelCache();
        var sceneRenderer = new OutmSceneRenderer(modelCache);
        var audio = new OutmAudioSystem();
        audio.Load(world);

        world.PushLog("OUT CORE boot");
        world.PushLog($"player entity: {world.PlayerEntity}");
        world.PushLog($"manifest maps: {manifest.Maps.Length}");
        world.PushLog($"map: {map.DisplayName}");
        world.PushLog(validation.Summary);
        world.PushLog($"mesh refs: {mapDef.Meshes.Length}");
        world.PushLog($"logic ticks: mid/{logicTicks.Policy.MidEveryTicks} far/{logicTicks.Policy.FarEveryTicks} dormant/{logicTicks.Policy.DormantEveryTicks}");
        world.PushLog($"defs: weapons {content.Weapons.Count}");
        world.PushLog($"fixed tick: {1.0f / fixedStep.FixedDelta:0} hz");
        world.PushLog($"collision backend: {collision.BackendKind}");
        world.PushLog(OutmFontSystem.IsLoaded ? "unicode HUD font online" : "unicode HUD font missing");

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
                SimulateFixedTick(world, map, collision, camera, weapons, triggers, commands, fixedStep.FixedDelta, ref stepTimer);
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
            sceneRenderer.Draw(mapDef);
            map.Draw();
            weapons.Draw();
            DrawViewRay(camera);
            Raylib.EndMode3D();

            DrawWeaponHud(world);
            editor.Draw(world, camera, map);

            Raylib.EndDrawing();
        }

        modelCache.Unload();
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
        OutmTriggerSystem triggers,
        OutmCommandQueue commands,
        float fixedDt,
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
                world.Transforms.Set(world.PlayerEntity, camera.Position, new Vector3(0.0f, camera.Yaw, camera.Pitch));
                UpdateFootsteps(world, camera, input, fixedDt, ref stepTimer);
            }

            triggers.UpdateUseTriggers(world, map, camera.Position, camera.Forward, input);

            if (!world.PlayerVitals.IsDead)
            {
                Vector3 muzzle = camera.Position + new Vector3(0, -0.08f, 0) + camera.Right * 0.22f;
                weapons.Update(input, muzzle, camera.Forward, collision, world);
            }
        }
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
