using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_Dropper : MonoBehaviour, OUTL_IEventListener
{
    public OUTL_EntityAdapter Entity;
    public OUTL_DropTable DropTable;
    public bool DropOnKilled = true;
    public bool DropOnlyOnce = true;
    public bool LogDrops = true;
    public bool WriteDiaryEntry = true;

    private bool dropped;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    private void OnEnable()
    {
        dropped = false;
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Register(this);
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (!DropOnKilled || DropTable == null || Entity == null) return;
        if (evt.Type != OUTL_EventType.Killed || evt.Target != Entity.Id) return;
        Drop(evt.Point != Vector3.zero ? evt.Point : transform.position);
    }

    public void Drop(Vector3 position)
    {
        if (DropOnlyOnce && dropped) return;
        if (!OUTL_NetworkAuthority.CanSpawnDrop())
        {
            OUTL_NetworkAuthority.TraceBlocked("legacy_drop", Entity);
            return;
        }
        dropped = true;
        DropTable.SpawnDrops(position, transform.rotation);
        OUTL_StimulusBus.EmitResource(Entity != null ? Entity.Id : OUTL_EntityId.None, position, 10f, 1f, 0.5f, DropTable != null ? DropTable.TableId : "drop");

        string entityId = Entity != null ? Entity.Id.Value.ToString() : "?";
        string tableId = DropTable != null ? DropTable.TableId : "null";

        if (LogDrops) OUTL_DebugLog.Log(OUTL_DebugChannel.Loot, "[DROP " + entityId + "] table=" + tableId + " pos=" + position);

        if (WriteDiaryEntry && Entity != null)
        {
            OUTL_EntityDiary diary = Entity.GetComponent<OUTL_EntityDiary>();
            if (diary != null) diary.Write(OUTL_DiaryEventType.DroppedLoot, "table=" + tableId + " pos=" + position, true);
        }
    }
}
