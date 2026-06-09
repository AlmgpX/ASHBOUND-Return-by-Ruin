using System;
using System.Collections.Generic;

namespace OUT_ASHBOUND;

public sealed class OUT_State
{
    public OUT_Content Content { get; }
    public OUT_Map WorldMap { get; set; }
    public OUT_Map LocalMap { get; set; }
    public OUT_Table Table { get; set; } = new();
    public OUT_EventBus Events { get; set; } = new();
    public List<string> LogLines { get; set; } = new();
    public int PlayerId { get; set; }
    public int Turn { get; set; }
    public int Loops { get; set; }
    public int Memory { get; set; }
    public int Residue { get; set; }
    public int Shards { get; set; }
    public int ShardsRequired { get; set; } = 3;
    public OUT_Mode Mode { get; set; } = OUT_Mode.World;
    public bool ShowInventory { get; set; }
    public OUT_Pos RuinNode { get; set; }
    public Random Rng { get; } = new();

    public OUT_State(OUT_Content content)
    {
        Content = content;
        WorldMap = OUT_Map.FromRows(content.World.Map, content.World.Tiles);
        LocalMap = OUT_Map.GenerateLocal(content.World.Tiles, content.World.Seed + 99);
    }

    public OUT_RuntimeObject? Player => Table.Get(PlayerId);
}
