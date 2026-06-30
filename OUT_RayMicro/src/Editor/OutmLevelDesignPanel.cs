using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.Gameplay;
using OUT_RayMicro.Input;
using OUT_RayMicro.Runtime;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Editor;

public sealed class OutmLevelDesignPanel
{
    public bool Visible;
    private int category;
    private int index;

    private static readonly Color Back = new(0, 0, 0, 190);
    private static readonly Color Frame = new(245, 180, 80, 230);
    private static readonly Color Text = new(230, 230, 220, 255);
    private static readonly Color Dim = new(160, 170, 180, 255);
    private static readonly Color Hot = new(245, 180, 80, 255);

    public void Update(OutmWorld world, OutmMapDef mapDef, OutmDemoMap map, OutmMapRuntimeStores runtime, in OutmInputFrame input)
    {
        if (input.IsPressed(OutmButtons.LevelDesign))
        {
            Visible = !Visible;
            world.PushLog(Visible ? "level panel on" : "level panel off");
        }

        if (!Visible || !input.IsPressed(OutmButtons.LevelDesignNext))
            return;

        index++;
        if (index >= Count(category, mapDef, map, runtime))
        {
            index = 0;
            category = (category + 1) % 5;
        }

        world.PushLog("level select: " + Label(mapDef, map, runtime));
    }

    public void Draw(OutmMapDef mapDef, OutmDemoMap map, OutmMapRuntimeStores runtime)
    {
        if (!Visible)
            return;

        int x = Math.Max(20, Raylib.GetScreenWidth() - 500);
        int y = 18;
        Raylib.DrawRectangle(x, y, 480, 280, Back);
        Raylib.DrawRectangleLines(x, y, 480, 280, Frame);

        DrawText("OUT CORE // LEVEL DESIGN", x + 14, y + 12, 18, Hot);
        DrawText("F6 toggle   F7 next", x + 14, y + 38, 13, Dim);
        DrawText($"map: {mapDef.Id}", x + 14, y + 66, 13, Text);
        DrawText($"name: {mapDef.DisplayName}", x + 14, y + 84, 13, Text);
        DrawText($"start: {V(mapDef.PlayerStartVector)}", x + 14, y + 102, 13, Text);
        DrawText($"boxes {map.Boxes.Count} doors {runtime.Doors.Count} sensors {runtime.Triggers.Count}", x + 14, y + 132, 13, Text);
        DrawText($"pickups {runtime.Pickups.Count} meshes {mapDef.Meshes.Length}", x + 14, y + 150, 13, Text);
        DrawText("selected: " + Label(mapDef, map, runtime), x + 14, y + 184, 14, Hot);

        Vector3 p = SelectedPosition(mapDef, map, runtime);
        Vector3 s = SelectedSize(mapDef, map, runtime);
        DrawText("pos:  " + V(p), x + 24, y + 214, 13, Text);
        DrawText("size: " + V(s), x + 24, y + 232, 13, Text);
        DrawText("next layer: move selected object and export OUTMAP", x + 14, y + 258, 13, Dim);
    }

    private static int Count(int cat, OutmMapDef mapDef, OutmDemoMap map, OutmMapRuntimeStores runtime)
    {
        return cat switch
        {
            0 => map.Boxes.Count,
            1 => runtime.Doors.Count,
            2 => runtime.Triggers.Count,
            3 => runtime.Pickups.Count,
            4 => mapDef.Meshes.Length,
            _ => 0
        };
    }

    private string Label(OutmMapDef mapDef, OutmDemoMap map, OutmMapRuntimeStores runtime)
    {
        int count = Count(category, mapDef, map, runtime);
        if (count <= 0)
            return CategoryName(category) + " none";

        int i = Math.Clamp(index, 0, count - 1);
        return category switch
        {
            0 => $"box[{i}] {map.Boxes[i].Id}",
            1 => $"door[{i}] {runtime.Doors.Doors[i].Id}",
            2 => $"sensor[{i}] {runtime.Triggers.Triggers[i].Id}",
            3 => $"pickup[{i}] {runtime.Pickups.Pickups[i].Id}",
            4 => $"mesh[{i}] {mapDef.Meshes[i].Id}",
            _ => "none"
        };
    }

    private Vector3 SelectedPosition(OutmMapDef mapDef, OutmDemoMap map, OutmMapRuntimeStores runtime)
    {
        int i = Math.Clamp(index, 0, Math.Max(0, Count(category, mapDef, map, runtime) - 1));
        return category switch
        {
            0 when map.Boxes.Count > 0 => map.Boxes[i].Center,
            1 when runtime.Doors.Count > 0 => runtime.Doors.Doors[i].Center,
            2 when runtime.Triggers.Count > 0 => runtime.Triggers.Triggers[i].Center,
            3 when runtime.Pickups.Count > 0 => runtime.Pickups.Pickups[i].Position,
            4 when mapDef.Meshes.Length > 0 => OutmMapDef.ToVector3(mapDef.Meshes[i].Position, Vector3.Zero),
            _ => Vector3.Zero
        };
    }

    private Vector3 SelectedSize(OutmMapDef mapDef, OutmDemoMap map, OutmMapRuntimeStores runtime)
    {
        int i = Math.Clamp(index, 0, Math.Max(0, Count(category, mapDef, map, runtime) - 1));
        return category switch
        {
            0 when map.Boxes.Count > 0 => map.Boxes[i].Size,
            1 when runtime.Doors.Count > 0 => runtime.Doors.Doors[i].Size,
            2 when runtime.Triggers.Count > 0 => runtime.Triggers.Triggers[i].Size,
            3 when runtime.Pickups.Count > 0 => new Vector3(runtime.Pickups.Pickups[i].Radius),
            4 when mapDef.Meshes.Length > 0 => OutmMapDef.ToVector3(mapDef.Meshes[i].Scale, Vector3.One),
            _ => Vector3.Zero
        };
    }

    private static string CategoryName(int cat) => cat switch { 0 => "box", 1 => "door", 2 => "sensor", 3 => "pickup", 4 => "mesh", _ => "none" };
    private static string V(Vector3 v) => $"{v.X:0.00}, {v.Y:0.00}, {v.Z:0.00}";
    private static void DrawText(string text, int x, int y, int size, Color color) => OutmFontSystem.DrawText(text, x, y, size, color);
}
