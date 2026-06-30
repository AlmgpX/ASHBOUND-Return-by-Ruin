using OUT_RayMicro.Core;

namespace OUT_RayMicro.World;

public sealed class OutmEntityChunkMembership
{
    private OutmChunkKey[] entityChunks;
    private bool[] hasChunk;
    private readonly Dictionary<OutmChunkKey, List<EntityId>> chunkEntities = new();

    public OutmEntityChunkMembership(int initialCapacity = 512)
    {
        int capacity = Math.Max(16, initialCapacity);
        entityChunks = new OutmChunkKey[capacity];
        hasChunk = new bool[capacity];
    }

    public void Set(EntityId entity, OutmChunkKey chunk)
    {
        if (!entity.IsValid)
            return;

        EnsureCapacity(entity.Index + 1);

        if (hasChunk[entity.Index])
        {
            OutmChunkKey old = entityChunks[entity.Index];
            if (old.Equals(chunk))
                return;

            RemoveFromChunkList(entity, old);
        }

        entityChunks[entity.Index] = chunk;
        hasChunk[entity.Index] = true;

        if (!chunkEntities.TryGetValue(chunk, out List<EntityId>? list))
        {
            list = new List<EntityId>(16);
            chunkEntities[chunk] = list;
        }

        list.Add(entity);
    }

    public bool TryGet(EntityId entity, out OutmChunkKey chunk)
    {
        if (entity.Index < 0 || entity.Index >= hasChunk.Length || !hasChunk[entity.Index])
        {
            chunk = default;
            return false;
        }

        chunk = entityChunks[entity.Index];
        return true;
    }

    public void Remove(EntityId entity)
    {
        if (entity.Index < 0 || entity.Index >= hasChunk.Length || !hasChunk[entity.Index])
            return;

        OutmChunkKey old = entityChunks[entity.Index];
        RemoveFromChunkList(entity, old);
        hasChunk[entity.Index] = false;
        entityChunks[entity.Index] = default;
    }

    public int CountInChunk(OutmChunkKey chunk)
    {
        return chunkEntities.TryGetValue(chunk, out List<EntityId>? list) ? list.Count : 0;
    }

    public IReadOnlyList<EntityId> GetEntitiesInChunk(OutmChunkKey chunk)
    {
        return chunkEntities.TryGetValue(chunk, out List<EntityId>? list) ? list : Array.Empty<EntityId>();
    }

    private void RemoveFromChunkList(EntityId entity, OutmChunkKey chunk)
    {
        if (!chunkEntities.TryGetValue(chunk, out List<EntityId>? list))
            return;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Index == entity.Index && list[i].Generation == entity.Generation)
            {
                int last = list.Count - 1;
                list[i] = list[last];
                list.RemoveAt(last);
                break;
            }
        }

        if (list.Count == 0)
            chunkEntities.Remove(chunk);
    }

    private void EnsureCapacity(int required)
    {
        if (required <= entityChunks.Length)
            return;

        int next = entityChunks.Length;
        while (next < required)
            next *= 2;

        Array.Resize(ref entityChunks, next);
        Array.Resize(ref hasChunk, next);
    }
}
