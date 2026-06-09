using System;
using System.Collections.Generic;

namespace OUT_ASHBOUND;

public static class OUT_Local
{
    public static void Enter(OUT_State state)
    {
        state.LocalMap = OUT_Map.GenerateLocal(state.Content.World.Tiles, state.Content.World.Seed + state.Turn + state.Player!.Pos.X * 31 + state.Player.Pos.Y * 997);
        state.Table.DropScope(OUT_Scope.Local);

        var player = state.Player!;
        player.Scope = OUT_Scope.Local;
        player.Pos = new OUT_Pos(state.LocalMap.Width / 2, state.LocalMap.Height / 2);

        var loc = state.Content.World.Locations.Find(x => x.Pos == player.WorldPos || x.Pos == player.Pos);
        var spawns = loc?.LocalSpawns ?? new List<OUT_SpawnDef>
        {
            new() { Def = "wolf", Count = 2 },
            new() { Def = "herb", Count = 2 }
        };

        foreach (var spawn in spawns)
            for (int i = 0; i < spawn.Count; i++) Spawn(state, spawn.Def);

        state.Mode = OUT_Mode.Local;
        OUT_Log.Add(state, "[THRESHOLD] local layer entered");
    }

    private static void Spawn(OUT_State state, string defKey)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            var pos = new OUT_Pos(state.Rng.Next(1, state.LocalMap.Width - 1), state.Rng.Next(1, state.LocalMap.Height - 1));
            if (!state.LocalMap.TileAt(pos).Walkable) continue;
            if (state.Table.BlockingAt(pos, OUT_Scope.Local) != null) continue;
            state.Table.Create(state.Content.Entity(defKey), pos, OUT_Scope.Local);
            return;
        }
    }
}
