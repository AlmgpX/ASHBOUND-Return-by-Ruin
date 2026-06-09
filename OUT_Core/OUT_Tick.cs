using System;
using System.Collections.Generic;
using System.Linq;

namespace OUT_ASHBOUND;

public interface OUT_ITickSystem
{
    void Tick(int turn);
}

public sealed class OUT_Scheduler
{
    private readonly List<OUT_ITickSystem> systems = new();
    public void Register(OUT_ITickSystem system) => systems.Add(system);
    public void Tick(int turn)
    {
        foreach (var system in systems) system.Tick(turn);
    }
}

public sealed class OUT_AIBrain : OUT_ITickSystem
{
    private readonly OUT_State state;

    public OUT_AIBrain(OUT_State state)
    {
        this.state = state;
    }

    public void Tick(int turn)
    {
        if (state.Mode != OUT_Mode.Local) return;
        var player = state.Player;
        if (player == null) return;

        foreach (var npc in state.Table.All.ToList())
        {
            if (npc.Scope != OUT_Scope.Local || !npc.Def.IsActor || npc.Id == state.PlayerId || npc.Stats.Hp <= 0) continue;
            int dist = OUT_Pos.Distance(npc.Pos, player.Pos);
            if (dist == 1)
            {
                var effects = new OUT_EffectSystem(state);
                effects.Hurt(npc.Id, player.Id, Math.Max(1, npc.Stats.Attack));
                continue;
            }

            OUT_Pos dir = dist <= npc.Def.Sight ? StepToward(npc.Pos, player.Pos) : OUT_Pos.Cardinals[state.Rng.Next(OUT_Pos.Cardinals.Length)];
            var next = npc.Pos + dir;
            if (!state.LocalMap.InBounds(next) || !state.LocalMap.TileAt(next).Walkable) continue;
            if (state.Table.BlockingAt(next, OUT_Scope.Local) != null) continue;
            npc.Pos = next;
        }
    }

    private static OUT_Pos StepToward(OUT_Pos from, OUT_Pos to)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);
        return Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y) ? new OUT_Pos(dx, 0) : new OUT_Pos(0, dy);
    }
}
