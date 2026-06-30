using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Render;

public sealed class OutmSceneRenderer
{
    private readonly OutmModelCache modelCache;

    public OutmSceneRenderer(OutmModelCache modelCache)
    {
        this.modelCache = modelCache;
    }

    public void Draw(OutmMapDef mapDef)
    {
        for (int i = 0; i < mapDef.Meshes.Length; i++)
            DrawMeshRef(mapDef.Meshes[i]);
    }

    private void DrawMeshRef(OutmMeshRefDef mesh)
    {
        Vector3 position = OutmMapDef.ToVector3(mesh.Position, Vector3.Zero);
        Vector3 rotation = OutmMapDef.ToVector3(mesh.Rotation, Vector3.Zero);
        Vector3 scale = OutmMapDef.ToVector3(mesh.Scale, Vector3.One);

        if (modelCache.TryGet(mesh.Path, out Model model))
        {
            // Raylib DrawModelEx rotation angle is degrees. OUTMAP rotation is stored as degrees too.
            Raylib.DrawModelEx(model, position, Vector3.UnitY, rotation.Y, scale, Color.White);
            return;
        }

        DrawMissingMeshPlaceholder(position, scale, mesh.Id);
    }

    private static void DrawMissingMeshPlaceholder(Vector3 position, Vector3 scale, string id)
    {
        Vector3 size = new(
            MathF.Max(0.25f, MathF.Abs(scale.X)),
            MathF.Max(0.25f, MathF.Abs(scale.Y)),
            MathF.Max(0.25f, MathF.Abs(scale.Z)));

        Raylib.DrawCubeV(position + Vector3.UnitY * (size.Y * 0.5f), size, new Color(200, 40, 180, 120));
        Raylib.DrawCubeWiresV(position + Vector3.UnitY * (size.Y * 0.5f), size, new Color(255, 80, 220, 255));
        Raylib.DrawSphere(position + Vector3.UnitY * (size.Y + 0.2f), 0.08f, Color.Magenta);
    }
}
