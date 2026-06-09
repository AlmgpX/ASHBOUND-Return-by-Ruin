using System;
using System.Linq;

namespace OUT_ASHBOUND;

public static class OUT_Validator
{
    public static void Validate(OUT_State state)
    {
        if (state == null) throw new InvalidOperationException("OUT invariant: state is null.");
        if (state.Content == null) throw new InvalidOperationException("OUT invariant: content is null.");
        if (state.WorldMap == null) throw new InvalidOperationException("OUT invariant: world map is null.");
        if (state.LocalMap == null) throw new InvalidOperationException("OUT invariant: local map is null.");
        if (state.Table == null) throw new InvalidOperationException("OUT invariant: table is null.");
        if (state.Events == null) throw new InvalidOperationException("OUT invariant: event bus is null.");

        OUT_RuntimeObject? player = state.Table.Get(state.PlayerId);
        if (player == null) throw new InvalidOperationException("OUT invariant: player object is missing.");
        if (player.Stats.Hp < 0 || player.Stats.Hp > player.Stats.MaxHp) throw new InvalidOperationException("OUT invariant: player HP out of range.");
        if (player.Stats.Stamina < 0 || player.Stats.Stamina > player.Stats.MaxStamina) throw new InvalidOperationException("OUT invariant: player stamina out of range.");
        if (state.Shards < 0 || state.Shards > 999) throw new InvalidOperationException("OUT invariant: shard count is invalid.");
        if (state.Loops < 0 || state.Memory < 0 || state.Residue < 0) throw new InvalidOperationException("OUT invariant: loop/memory/residue cannot be negative.");

        foreach (var group in state.Table.All.GroupBy(x => x.Id))
        {
            if (group.Count() > 1) throw new InvalidOperationException("OUT invariant: duplicate runtime id " + group.Key + ".");
        }

        foreach (var obj in state.Table.All)
        {
            if (obj.Def == null) throw new InvalidOperationException("OUT invariant: object without def id=" + obj.Id + ".");
            if (obj.Stats.Hp < 0 || obj.Stats.Hp > obj.Stats.MaxHp) throw new InvalidOperationException("OUT invariant: HP out of range id=" + obj.Id + ".");

            if (obj.Scope == OUT_Scope.World && !state.WorldMap.InBounds(obj.Pos))
                throw new InvalidOperationException("OUT invariant: world object outside map id=" + obj.Id + ".");

            if (obj.Scope == OUT_Scope.Local && !state.LocalMap.InBounds(obj.Pos))
                throw new InvalidOperationException("OUT invariant: local object outside map id=" + obj.Id + ".");
        }
    }
}
