using System;
using System.Linq;
using Raylib_cs;

namespace OUT_ASHBOUND;

public sealed class OUT_RaylibApp
{
    private OUT_State state;
    private OUT_EffectSystem effects;
    private OUT_CommandSystem commands;
    private OUT_Scheduler scheduler;
    private readonly OUT_RaylibRenderer renderer = new();
    private bool running = true;

    public OUT_RaylibApp(OUT_State state)
    {
        this.state = state;
        RebindSystems();
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1280, 720, "OUT_ASHBOUND: Return by Ruin");
        Raylib.SetTargetFPS(60);
        renderer.Load();

        try
        {
            while (running && !Raylib.WindowShouldClose())
            {
                OUT_Validator.Validate(state);
                HandleInput();
                OUT_Fx.Tick(state);
                renderer.Render(state);
            }
        }
        finally
        {
            renderer.Unload();
            Raylib.CloseWindow();
        }
    }

    private void RebindSystems()
    {
        effects = new OUT_EffectSystem(state);
        commands = new OUT_CommandSystem(state, effects);
        scheduler = new OUT_Scheduler();
        scheduler.Register(new OUT_AIBrain(state));
    }

    private void HandleInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Q))
        {
            running = false;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F5))
        {
            OUT_SaveSystem.Save(state, "OUT_Save/slot1.json");
            OUT_Log.Add(state, "[SAVE] OUT_Save/slot1.json");
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F9))
        {
            var loaded = OUT_SaveSystem.TryLoad(state.Content, "OUT_Save/slot1.json");
            if (loaded != null)
            {
                state = loaded;
                RebindSystems();
                OUT_Log.Add(state, "[LOAD] OUT_Save/slot1.json");
            }
            else OUT_Log.Add(state, "[LOAD] no save slot");
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            state.WorldMap = OUT_WorldGenerator.Generate(state.Content.World, Environment.TickCount);
            state.Table.DropScope(OUT_Scope.Local);
            state.Mode = OUT_Mode.World;
            if (state.Player != null)
            {
                state.Player.Scope = OUT_Scope.World;
                state.Player.Pos = state.Content.World.Start;
                state.Player.WorldPos = state.Content.World.Start;
            }
            OUT_Log.Add(state, "[GEN] procedural world regenerated");
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            ToggleLayer();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.I))
        {
            state.ShowInventory = !state.ShowInventory;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.P))
        {
            DrinkPotion();
            EndTurn();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F))
        {
            Shoot();
            EndTurn();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.E))
        {
            commands.Send(OUT_Command.Interact(state.PlayerId));
            EndTurn();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.T))
        {
            commands.Send(OUT_Command.Talk(state.PlayerId));
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            commands.Send(OUT_Command.Wait(state.PlayerId));
            EndTurn();
            return;
        }

        OUT_Pos dir = OUT_Pos.Zero;
        if (Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up)) dir = OUT_Pos.Up;
        else if (Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down)) dir = OUT_Pos.Down;
        else if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left)) dir = OUT_Pos.Left;
        else if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right)) dir = OUT_Pos.Right;

        if (dir != OUT_Pos.Zero)
        {
            state.LastAim = dir;
            commands.Send(OUT_Command.Move(state.PlayerId, dir));
            EndTurn();
        }
    }

    private void DrinkPotion()
    {
        var player = state.Player;
        if (player == null) return;

        if (!player.Bag.TryGetValue("potion", out int count) || count <= 0)
        {
            OUT_Log.Add(state, "[ITEM] no potion");
            return;
        }

        player.Bag["potion"] = count - 1;
        effects.ModifyHp(player.Id, 8);
        OUT_Log.Add(state, "[ITEM] potion used +8 HP");
    }

    private void Shoot()
    {
        var player = state.Player;
        if (player == null) return;

        if (!player.Bag.TryGetValue("arrow", out int count) || count <= 0)
        {
            OUT_Log.Add(state, "[SHOT] no arrows");
            return;
        }

        player.Bag["arrow"] = count - 1;
        OUT_Scope scope = state.Mode == OUT_Mode.World ? OUT_Scope.World : OUT_Scope.Local;
        OUT_Pos pos = player.Pos;

        for (int i = 0; i < 8; i++)
        {
            pos += state.LastAim;
            OUT_Fx.Shot(state, scope, pos);
            bool inBounds = scope == OUT_Scope.World ? state.WorldMap.InBounds(pos) : state.LocalMap.InBounds(pos);
            bool walkable = scope == OUT_Scope.World ? state.WorldMap.TileAt(pos).Walkable : state.LocalMap.TileAt(pos).Walkable;
            if (!inBounds || !walkable) break;

            var target = state.Table.BlockingAt(pos, scope);
            if (target != null && target.Id != player.Id)
            {
                effects.Hurt(player.Id, target.Id, 7);
                OUT_Log.Add(state, "[SHOT] arrow hits " + target.Def.Name);
                return;
            }
        }

        OUT_Log.Add(state, "[SHOT] arrow lost to the world's excellent emptiness");
    }

    private void ToggleLayer()
    {
        if (state.Mode == OUT_Mode.World)
        {
            OUT_Local.Enter(state);
            return;
        }

        var player = state.Player;
        if (player == null) return;
        player.Scope = OUT_Scope.World;
        player.Pos = player.WorldPos;
        state.Mode = OUT_Mode.World;
        state.Table.DropScope(OUT_Scope.Local);
        OUT_Log.Add(state, "[WORLD] returned to travel map");
    }

    private void EndTurn()
    {
        FlushEvents();
        scheduler.Tick(state.Turn++);
        if (state.Mode == OUT_Mode.World) RandomWorldEvent();
        FlushEvents();
        if (state.Player != null && state.Player.Stats.Hp <= 0) DeathLoop();
    }

    private void RandomWorldEvent()
    {
        var player = state.Player;
        if (player == null) return;
        var tile = state.WorldMap.TileAt(player.Pos);
        if (state.Rng.Next(100) >= tile.RandomEventChance) return;

        var events = state.Content.Events.Where(e => e.Terrain == tile.Key || e.Terrain == "any").ToList();
        if (events.Count == 0) return;

        var ev = events[state.Rng.Next(events.Count)];
        OUT_Log.Add(state, "[SIGNAL] " + ev.Text);
        if (ev.HpDelta != 0) effects.ModifyHp(state.PlayerId, ev.HpDelta);
        state.Residue += ev.ResidueDelta;
        if (!string.IsNullOrWhiteSpace(ev.Item)) player.OUT_Put(ev.Item, Math.Max(1, ev.ItemCount));
    }

    private void FlushEvents()
    {
        state.Events.Flush(evt =>
        {
            OUT_Log.Add(state, evt.Text);
            if (evt.Type == OUT_EventType.ShardTaken) state.Shards++;
            if (evt.Type == OUT_EventType.GateOpened) OUT_Log.Add(state, "[REACTION] The ruin confirms transition.");
        });
    }

    private void DeathLoop()
    {
        var player = state.Player;
        if (player == null) return;
        state.Loops++;
        state.Memory++;
        state.Residue += 2;
        player.Stats.Hp = player.Stats.MaxHp;
        player.Scope = OUT_Scope.World;
        player.Pos = state.RuinNode;
        state.Table.DropScope(OUT_Scope.Local);
        state.Mode = OUT_Mode.World;
        OUT_Log.Add(state, "[SHADOW] death returns. loop=" + state.Loops + " memory=" + state.Memory);
    }
}
