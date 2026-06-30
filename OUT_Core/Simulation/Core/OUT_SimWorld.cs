using System.Collections.Generic;

public sealed class OUT_SimWorld
{
    private int nextEntityId = 1;
    private readonly Dictionary<int, string> entityLabels = new Dictionary<int, string>(1024);
    private readonly Dictionary<int, OUT_ChunkId> entityChunks = new Dictionary<int, OUT_ChunkId>(1024);

    public int Seed { get; private set; } = 1337;
    public int CurrentTick { get; internal set; }
    public float CurrentTime { get; internal set; }

    public int EntityCount => entityLabels.Count;

    public void SetSeed(int seed)
    {
        Seed = seed;
    }

    public OUT_SimEntityId AllocateEntity(string label = null, OUT_ChunkId chunk = default)
    {
        OUT_SimEntityId id = new OUT_SimEntityId(nextEntityId++);
        entityLabels[id.Value] = string.IsNullOrWhiteSpace(label) ? "entity_" + id.Value : label;
        entityChunks[id.Value] = chunk;
        return id;
    }

    public bool Exists(OUT_SimEntityId id)
    {
        return id.IsValid && entityLabels.ContainsKey(id.Value);
    }

    public bool TryGetLabel(OUT_SimEntityId id, out string label)
    {
        if (!id.IsValid)
        {
            label = null;
            return false;
        }

        return entityLabels.TryGetValue(id.Value, out label);
    }

    public void SetChunk(OUT_SimEntityId id, OUT_ChunkId chunk)
    {
        if (!id.IsValid)
            return;

        entityChunks[id.Value] = chunk;
    }

    public bool TryGetChunk(OUT_SimEntityId id, out OUT_ChunkId chunk)
    {
        if (!id.IsValid)
        {
            chunk = default;
            return false;
        }

        return entityChunks.TryGetValue(id.Value, out chunk);
    }

    public bool RemoveEntity(OUT_SimEntityId id)
    {
        if (!id.IsValid)
            return false;

        entityChunks.Remove(id.Value);
        return entityLabels.Remove(id.Value);
    }

    public void Clear()
    {
        nextEntityId = 1;
        entityLabels.Clear();
        entityChunks.Clear();
        CurrentTick = 0;
        CurrentTime = 0f;
    }
}
