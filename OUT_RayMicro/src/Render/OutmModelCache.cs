using Raylib_cs;
using OUT_RayMicro.Runtime;

namespace OUT_RayMicro.Render;

public sealed class OutmModelCache
{
    private readonly Dictionary<string, ModelSlot> models = new(StringComparer.OrdinalIgnoreCase);

    public int LoadedCount
    {
        get
        {
            int count = 0;
            foreach (ModelSlot slot in models.Values)
            {
                if (slot.Loaded)
                    count++;
            }
            return count;
        }
    }

    public bool TryGet(string relativePath, out Model model)
    {
        model = default;
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        if (models.TryGetValue(relativePath, out ModelSlot cached))
        {
            model = cached.Model;
            return cached.Loaded;
        }

        string path = OutmAssetPaths.ResolveData(relativePath);
        if (!File.Exists(path))
        {
            models[relativePath] = ModelSlot.Missing;
            OutmCrashLog.Write($"model missing: {path}");
            return false;
        }

        try
        {
            OutmCrashLog.Write($"model load: {path}");
            model = Raylib.LoadModel(path);
            models[relativePath] = new ModelSlot(true, model);
            return true;
        }
        catch (Exception ex)
        {
            models[relativePath] = ModelSlot.Missing;
            OutmCrashLog.Write($"model load failed: {path}\n{ex}");
            model = default;
            return false;
        }
    }

    public void Unload()
    {
        foreach (ModelSlot slot in models.Values)
        {
            if (!slot.Loaded)
                continue;

            try
            {
                Raylib.UnloadModel(slot.Model);
            }
            catch (Exception ex)
            {
                OutmCrashLog.Write("model unload failed\n" + ex);
            }
        }

        models.Clear();
    }

    private readonly struct ModelSlot
    {
        public readonly bool Loaded;
        public readonly Model Model;

        public ModelSlot(bool loaded, Model model)
        {
            Loaded = loaded;
            Model = model;
        }

        public static readonly ModelSlot Missing = new(false, default);
    }
}
