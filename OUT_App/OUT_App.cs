using System;
using System.Linq;

namespace OUT_ASHBOUND;

public sealed class OUT_App
{
    private OUT_State state;
    private OUT_EffectSystem effects;
    private OUT_CommandSystem commands;
    private OUT_Scheduler scheduler;
    private readonly OUT_Renderer renderer = new();
    private bool running = true;

    public OUT_App(OUT_State state)
    {
        this.state = state;
        RebindSystems();
    }

    public void Run()
    {
        while (running)
        {
            OUT_Validator.Validate(state);
            renderer.Render(state);
            Handle(Console.ReadKey(true).Key);
        }
    }

    private void RebindSystems()
    {
        effects = new OUT_EffectSystem(state);
        commands = new OUT_CommandSystem(state, effects);
        scheduler = new OUT_Scheduler();
        scheduler.Register(new OUT_AIBrain(state));
    }

    private void Handle(ConsoleKey key)
    {
        if (key == ConsoleKey.Q || key == ConsoleKey.Escape)
        {
            running = false;
            return;
        }

        if (key == ConsoleKey.F5)
        {
            OUT_SaveSystem.Save(state, "OUT_Save/slot1.json");
            OUT_Log.Add(state, "[SAVE] OUT_Save/slot1.json");
            return;
        }

        if (key == ConsoleKey.F9)
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

        if (key == ConsoleKey.Tab)
        {
            if (state.Mode == OUT_Mode.World) OUT_Local.Enter(state);
            else
            {
                var player = state.Player!;
                player.Scope = OUT_Scope.World;
                player.Pos = player.WorldPos;
                state.Mode = OUT_Mode.World;
                state.Table.DropScope(OUT_Scope.Local);
                OUT_Log.Add(state, "[WORLD] returned to travel map");
            }
            return;
        }

        if (key == ConsoleKey.I)
        {
            state.ShowInventory = !state.ShowInventory;
            return;
        }

        if (key == ConsoleKey.E)
        {
            commands.Send(OUT_Command.Interact(state.PlayerId));
            EndTurn();
            return;
        }

        if (key == ConsoleKey.T)
        {
            commands.Send(OUT_Command.Talk(state.PlayerId));
            return;
        }

        if (key == ConsoleKey.Spacebar)
        {
            commands.Send(OUT_Command.Wait(state.PlayerId));
            EndTurn();
            return;
        }

        var dir = key switch
        {
            ConsoleKey.W or ConsoleKey.UpArrow => OUT_Pos.Up,
            ConsoleKey.S or ConsoleKey.DownArrow => OUT_Pos.Down,
            ConsoleKey.A or ConsoleKey.LeftArrow => OUT_Pos.Left,
            ConsoleKey.D or ConsoleKey.RightArrow => OUT_Pos.Right,
            _ => OUT_Pos.Zero
        };

        if (dir != OUT_Pos.Zero)
        {
            commands.Send(OUT_Command.Move(state.PlayerId, dir));
            EndTurn();
        }
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
