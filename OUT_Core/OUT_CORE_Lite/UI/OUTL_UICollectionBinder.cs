using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum OUTL_UICollectionSource
{
    Inventory = 0,
    EquipmentSlots = 1,
    ChallengeProgress = 2,
    RewardsFeed = 3
}

[DefaultExecutionOrder(1225)]
[DisallowMultipleComponent]
public sealed class OUTL_UICollectionBinder : MonoBehaviour, OUTL_IEventListener
{
    public OUTL_EntityAdapter Entity;
    public OUTL_GameLoopRunner GameLoop;
    public OUTL_UICollectionSource Source = OUTL_UICollectionSource.EquipmentSlots;
    public RectTransform Root;
    public Text TextPrefab;
    public int MaxRows = 12;
    public float RefreshInterval = 0.25f;
    public bool UseExistingRows = true;
    public bool AutoCreateRows = true;
    public bool RefreshOnEvents = true;

    private readonly List<Text> rows = new List<Text>(16);
    private readonly List<GameObject> pooledRows = new List<GameObject>(16);
    private readonly List<OUTL_InventoryItemSnapshot> inventory = new List<OUTL_InventoryItemSnapshot>(32);
    private readonly List<string> feed = new List<string>(16);
    private float nextRefresh;
    private bool registered;
    private bool existingRowsScanned;
    private bool missingPrefabWarned;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponentInParent<OUTL_EntityAdapter>();
        if (GameLoop == null) GameLoop = GetComponentInParent<OUTL_GameLoopRunner>();
        if (Root == null) Root = GetComponent<RectTransform>();
        EnsureRows();
    }

    private void OnEnable()
    {
        RegisterEvents();
        RefreshNow();
    }

    private void OnDisable()
    {
        if (registered && OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
        registered = false;
    }

    private void OnDestroy()
    {
        ReleasePooledRows();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + Mathf.Max(0.05f, RefreshInterval);
        RefreshNow();
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (!RefreshOnEvents) return;
        if (evt.Key == OUTL_LoopKeys.RewardGranted || evt.Key == OUTL_LoopKeys.ChallengeProgress || evt.Key == OUTL_LoopKeys.ChallengeCompleted || evt.Type == OUTL_EventType.ItemAdded || evt.Type == OUTL_EventType.ItemRemoved || evt.Type == OUTL_EventType.Equipped || evt.Type == OUTL_EventType.Unequipped)
        {
            if (evt.Key == OUTL_LoopKeys.RewardGranted) PushFeed("Reward +" + evt.IntValue + " / XP " + evt.FloatValue.ToString("0"));
            RefreshNow();
        }
    }

    [ContextMenu("OUT Refresh Collection")]
    public void RefreshNow()
    {
        EnsureRows();
        ClearRows();
        switch (Source)
        {
            case OUTL_UICollectionSource.Inventory: FillInventory(); break;
            case OUTL_UICollectionSource.ChallengeProgress: FillChallenges(); break;
            case OUTL_UICollectionSource.RewardsFeed: FillFeed(); break;
            case OUTL_UICollectionSource.EquipmentSlots:
            default: FillEquipment(); break;
        }
    }

    private void FillEquipment()
    {
        if (Entity == null || Entity.Runtime == null) return;
        int row = 0;
        for (int i = 0; i < 6 && row < rows.Count; i++)
        {
            OUTL_EquipmentSlot slot = (OUTL_EquipmentSlot)i;
            string value = Entity.Runtime.State.GetString(OUTL_Equipment.BuildSlotStateKey(slot), string.Empty);
            SetRow(row++, slot + ": " + (string.IsNullOrEmpty(value) ? "-" : value));
        }
    }

    private void FillInventory()
    {
        if (Entity == null || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Inventory.CopyItems(Entity.Id, inventory);
        for (int i = 0; i < inventory.Count && i < rows.Count; i++)
            SetRow(i, inventory[i].Item != null ? inventory[i].Item.DisplayName + " x" + inventory[i].Count : "<null>");
    }

    private void FillChallenges()
    {
        if (GameLoop == null || GameLoop.GameLoop == null || GameLoop.GameLoop.StartupChallenges == null) return;
        for (int i = 0; i < GameLoop.GameLoop.StartupChallenges.Length && i < rows.Count; i++)
        {
            OUTL_ChallengeDef c = GameLoop.GameLoop.StartupChallenges[i];
            if (c == null) continue;
            SetRow(i, c.DisplayName + ": " + GameLoop.GetProgress(c.ChallengeId) + "/" + Mathf.Max(1, c.TargetCount) + (GameLoop.IsCompleted(c.ChallengeId) ? " DONE" : ""));
        }
    }

    private void FillFeed()
    {
        for (int i = 0; i < feed.Count && i < rows.Count; i++) SetRow(i, feed[i]);
    }

    private void PushFeed(string text)
    {
        feed.Insert(0, text);
        while (feed.Count > MaxRows) feed.RemoveAt(feed.Count - 1);
    }

    private void EnsureRows()
    {
        if (Root == null) return;
        CollectExistingRows();
        if (!AutoCreateRows) return;

        int target = Mathf.Max(1, MaxRows);
        while (rows.Count < target)
        {
            Text row = SpawnPooledRow(rows.Count);
            if (row == null) return;
            rows.Add(row);
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < rows.Count; i++) SetRow(i, string.Empty);
    }

    private void SetRow(int index, string value)
    {
        if (index < 0 || index >= rows.Count) return;
        Text row = rows[index];
        if (row == null) return;
        value = value ?? string.Empty;
        if (row.text != value) row.text = value;
    }

    private void CollectExistingRows()
    {
        if (!UseExistingRows || existingRowsScanned || Root == null) return;
        existingRowsScanned = true;

        Text[] existing = Root.GetComponentsInChildren<Text>(true);
        int target = Mathf.Max(1, MaxRows);
        for (int i = 0; i < existing.Length && rows.Count < target; i++)
        {
            Text row = existing[i];
            if (row == null || row == TextPrefab || ContainsRow(row)) continue;
            rows.Add(row);
        }
    }

    private Text SpawnPooledRow(int index)
    {
        if (TextPrefab == null)
        {
            WarnMissingPrefab();
            return null;
        }

        GameObject go = OUTL_PoolSystem.SpawnShared(TextPrefab.gameObject, Root.position, Root.rotation, false);
        if (go == null) return null;

        go.name = "OUTL_Row_" + index.ToString("00");
        go.transform.SetParent(Root, false);

        Text row = go.GetComponent<Text>();
        if (row == null)
        {
            OUTL_PoolSystem.ReleaseShared(go);
            return null;
        }

        ApplyPrefabTransform(row);
        row.text = string.Empty;
        pooledRows.Add(go);
        go.SetActive(true);
        return row;
    }

    private void ReleasePooledRows()
    {
        for (int i = pooledRows.Count - 1; i >= 0; i--)
        {
            GameObject row = pooledRows[i];
            if (row != null) OUTL_PoolSystem.ReleaseShared(row);
        }

        pooledRows.Clear();
    }

    private bool ContainsRow(Text row)
    {
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] == row)
                return true;
        return false;
    }

    private void ApplyPrefabTransform(Text row)
    {
        RectTransform source = TextPrefab != null ? TextPrefab.transform as RectTransform : null;
        RectTransform target = row != null ? row.transform as RectTransform : null;
        if (source == null || target == null) return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private void WarnMissingPrefab()
    {
        if (missingPrefabWarned) return;
        missingPrefabWarned = true;
        Debug.LogWarning("OUTL_UICollectionBinder needs preauthored Text rows under Root or a TextPrefab for pooled runtime rows.", this);
    }

    private void RegisterEvents()
    {
        if (registered || OUTL_World.Instance == null) return;
        OUTL_World.Instance.Events.Register(this);
        registered = true;
    }
}
