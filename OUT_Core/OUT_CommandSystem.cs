using System;
using System.Linq;

namespace OUT_ASHBOUND;

public sealed class OUT_CommandSystem
{
    private readonly OUT_State state;
    private readonly OUT_EffectSystem effects;

    public OUT_CommandSystem(OUT_State state, OUT_EffectSystem effects)
    {
        this.state = state;
        this.effects = effects;
    }

    public bool Send(OUT_Command command)
    {
        var actor = state.Table.Get(command.Source);
        if (actor == null || actor.Stats.Hp <= 0) return false;

        return command.Type switch
        {
            OUT_CommandType.Move => Move(actor, command.Direction),
            OUT_CommandType.Wait => Wait(actor),
            OUT_CommandType.Interact => Interact(actor),
            OUT_CommandType.Talk => Talk(actor),
            OUT_CommandType.Attack => Strike(actor, command.Target),
            _ => false
        };
    }

    private bool Move(OUT_RuntimeObject actor, OUT_Pos dir)
    {
        var next = actor.Pos + dir;

        if (state.Mode == OUT_Mode.World)
        {
            if (!state.WorldMap.InBounds(next) || !state.WorldMap.TileAt(next).Walkable)
            {
                OUT_Log.Add(state, "[WORLD] blocked");
                return false;
            }

            actor.Pos = next;
            actor.WorldPos = next;
            var tile = state.WorldMap.TileAt(next);
            state.Turn += tile.Cost;
            effects.ModifyStamina(actor.Id, -tile.Cost);
            OUT_Log.Add(state, "[WORLD] " + tile.Name + " cost=" + tile.Cost);
            return true;
        }

        if (!state.LocalMap.InBounds(next) || !state.LocalMap.TileAt(next).Walkable)
        {
            OUT_Log.Add(state, "[LOCAL] blocked");
            return false;
        }

        var block = state.Table.BlockingAt(next, OUT_Scope.Local);
        if (block != null && block.Id != actor.Id) return Strike(actor, block.Id);

        actor.Pos = next;
        var item = state.Table.FirstAt(next, OUT_Scope.Local, false);
        if (item != null && item.Def.IsPickup) Pickup(actor, item);
        return true;
    }

    private bool Wait(OUT_RuntimeObject actor)
    {
        OUT_Log.Add(state, "[CHOICE] wait instead of automatic reaction");
        effects.ModifyStamina(actor.Id, 1);
        return true;
    }

    private bool Interact(OUT_RuntimeObject actor)
    {
        if (state.Mode == OUT_Mode.World)
        {
            var loc = state.Content.World.Locations.FirstOrDefault(l => l.Pos == actor.Pos);
            if (loc == null)
            {
                OUT_Log.Add(state, "[WORLD] no symbol here");
                return false;
            }

            if (loc.Kind == "gate")
            {
                if (state.Shards >= state.ShardsRequired) state.Events.Emit(OUT_Event.Gate("[REACTION] Gate opens"));
                else OUT_Log.Add(state, "[THRESHOLD] need shards " + state.ShardsRequired);
                return true;
            }

            if (loc.Kind == "ruin_node")
            {
                state.RuinNode = actor.Pos;
                OUT_Log.Add(state, "[SAVE NODE] ruin node bound");
                return true;
            }

            OUT_Local.Enter(state);
            return true;
        }

        foreach (var dir in OUT_Pos.Cardinals)
        {
            var obj = state.Table.FirstAt(actor.Pos + dir, OUT_Scope.Local, false);
            if (obj == null) continue;
            if (obj.Def.IsPickup)
            {
                Pickup(actor, obj);
                return true;
            }
            if (obj.Def.CanTalk)
            {
                OUT_Log.Add(state, "[CHOICE] " + obj.Def.Name + ": I feel your deaths without facts.");
                return true;
            }
        }

        if (state.LocalMap.TileAt(actor.Pos).Key == "exit")
        {
            actor.Scope = OUT_Scope.World;
            actor.Pos = actor.WorldPos;
            state.Mode = OUT_Mode.World;
            OUT_Log.Add(state, "[WORLD] returned");
            return true;
        }

        OUT_Log.Add(state, "[LOCAL] no usable symbol nearby");
        return false;
    }

    private bool Talk(OUT_RuntimeObject actor)
    {
        foreach (var dir in OUT_Pos.Cardinals)
        {
            var obj = state.Table.FirstAt(actor.Pos + dir, OUT_Scope.Local, false);
            if (obj != null && obj.Def.CanTalk)
            {
                OUT_Log.Add(state, "[SYMBOL] " + obj.Def.Name + ": The crown is not worn. It wears the wound.");
                return true;
            }
        }

        OUT_Log.Add(state, "[TALK] nobody close");
        return false;
    }

    private bool Strike(OUT_RuntimeObject actor, int targetId)
    {
        effects.Hurt(actor.Id, targetId, Math.Max(1, actor.Stats.Attack));
        return true;
    }

    private void Pickup(OUT_RuntimeObject actor, OUT_RuntimeObject item)
    {
        if (item.Def.Key == "shard") state.Events.Emit(OUT_Event.Shard("[SYMBOL] shard taken"));
        else
        {
            actor.OUT_Put(item.Def.Key, 1);
            OUT_Log.Add(state, "[ITEM] " + item.Def.Name);
        }

        state.Table.Drop(item.Id);
    }
}
