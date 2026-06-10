using System.Collections.Generic;

namespace OUT_ASHBOUND;

public static class OUT_Bootstrap
{
    public static OUT_State NewGame(OUT_Content content)
    {
        var state = new OUT_State(content);
        state.RuinNode = content.World.Start;
        state.PlayerId = state.Table.Create(content.Entity("player"), content.World.Start, OUT_Scope.World);

        var player = state.Table.Get(state.PlayerId)!;
        player.TargetName = "player";
        player.ClassName = "actor.controlled";
        player.OUT_Put("old_coin", 2);
        player.OUT_Put("potion", 2);
        player.OUT_Put("arrow", 12);

        foreach (var loc in content.World.Locations)
        {
            foreach (var spawn in loc.Spawns ?? new List<OUT_SpawnDef>())
            {
                for (int i = 0; i < spawn.Count; i++)
                    state.Table.Create(content.Entity(spawn.Def), loc.Pos, OUT_Scope.World);
            }
        }

        OUT_Log.Add(state, "[ACTOR] The Crownbound wakes under a crown-shaped wound in the sky.");
        OUT_Log.Add(state, "[SYMBOL] Shards are visible memory. Gate opens at 3.");
        OUT_Log.Add(state, "[ITEM] Starter kit: 2 potions, 12 arrows. Civilization peaked.");
        return state;
    }
}
