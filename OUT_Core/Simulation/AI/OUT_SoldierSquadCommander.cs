using System.Collections.Generic;
using UnityEngine;
using Time = OUT_SimTime;

[DisallowMultipleComponent]
public class OUT_SoldierSquadCommander : MonoBehaviour
{
    public enum SquadOrder
    {
        Hold = 0,
        Advance = 1,
        Suppress = 2,
        Regroup = 3,
        Retreat = 4,
        TakeCover = 5
    }

    [Header("Squad")]
    [SerializeField] private Transform squadAnchor;
    [SerializeField][Min(1)] private int maxMembers = 4;
    [SerializeField][Min(0.05f)] private float updateInterval = 0.35f;

    [Header("Formation")]
    [SerializeField][Min(0.5f)] private float frontSpacing = 6f;
    [SerializeField][Min(0.5f)] private float sideSpacing = 4f;
    [SerializeField][Min(0.5f)] private float retreatDistance = 12f;
    [SerializeField][Min(0.5f)] private float regroupRadius = 8f;

    [Header("Explosives")]
    [SerializeField] private bool allowExplosives = true;
    [SerializeField][Min(0f)] private float explosiveSafetyRadius = 5f;

    [Header("Runtime")]
    [SerializeField] private SquadOrder forcedOrder = SquadOrder.Hold;
    [SerializeField] private bool useForcedOrder = false;

    private readonly List<OUT_SoldierSquadAgent> _agents = new List<OUT_SoldierSquadAgent>(8);
    private Vector3 _lastEnemyPosition;
    private float _lastEnemySeenTime;
    private float _nextUpdateTime;
    private SquadOrder _currentOrder;

    public SquadOrder CurrentOrder => useForcedOrder ? forcedOrder : _currentOrder;
    public bool HasRecentEnemy => Time.time - _lastEnemySeenTime <= 6f;
    public Vector3 LastEnemyPosition => _lastEnemyPosition;
    public int AgentCount => _agents.Count;
    public Transform SquadAnchor => squadAnchor != null ? squadAnchor : transform;

    private void Awake()
    {
        if (squadAnchor == null)
            squadAnchor = transform;

        _currentOrder = SquadOrder.Hold;
    }

    private void Update()
    {
        if (Time.time < _nextUpdateTime)
            return;

        _nextUpdateTime = Time.time + updateInterval + Random.Range(0f, 0.08f);
        RefreshOrder();
    }

    public void SetSquadAnchor(Transform anchor)
    {
        squadAnchor = anchor != null ? anchor : transform;
    }

    public void RegisterAgent(OUT_SoldierSquadAgent agent)
    {
        if (agent == null || _agents.Contains(agent))
            return;

        if (_agents.Count >= maxMembers)
            return;

        _agents.Add(agent);
        RebuildSlots();
    }

    public void UnregisterAgent(OUT_SoldierSquadAgent agent)
    {
        if (agent == null)
            return;

        if (_agents.Remove(agent))
            RebuildSlots();
    }

    public OUT_SoldierSquadAgent GetAgentAt(int index)
    {
        if (index < 0 || index >= _agents.Count)
            return null;

        return _agents[index];
    }

    public void ReportEnemy(Vector3 enemyPosition)
    {
        _lastEnemyPosition = enemyPosition;
        _lastEnemySeenTime = Time.time;
    }

    public void SetForcedOrder(SquadOrder order)
    {
        forcedOrder = order;
        useForcedOrder = true;
    }

    public void ClearForcedOrder()
    {
        useForcedOrder = false;
    }

    public Vector3 GetSlotWorldPoint(OUT_SoldierSquadAgent agent, Vector3 fallbackOrigin, Vector3 enemyPosition)
    {
        Vector3 anchor = SquadAnchor != null ? SquadAnchor.position : fallbackOrigin;
        Vector3 enemy = HasRecentEnemy ? _lastEnemyPosition : enemyPosition;

        Vector3 toEnemy = enemy - anchor;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude <= 0.0001f)
            toEnemy = SquadAnchor != null ? SquadAnchor.forward : Vector3.forward;
        toEnemy.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, toEnemy).normalized;
        int slot = agent != null ? agent.SlotIndex : 0;

        Vector3[] offsets = new Vector3[4]
        {
            -right * sideSpacing,
            right * sideSpacing,
            (-right * sideSpacing * 0.7f) - toEnemy * frontSpacing,
            (right * sideSpacing * 0.7f) - toEnemy * frontSpacing
        };

        Vector3 baseOffset = slot < offsets.Length ? offsets[slot] : Vector3.zero;

        switch (CurrentOrder)
        {
            case SquadOrder.Advance:
                baseOffset += toEnemy * frontSpacing;
                break;
            case SquadOrder.Suppress:
                break;
            case SquadOrder.Regroup:
                baseOffset = Random.insideUnitSphere * regroupRadius;
                baseOffset.y = 0f;
                break;
            case SquadOrder.Retreat:
            case SquadOrder.TakeCover:
                baseOffset -= toEnemy * retreatDistance;
                break;
        }

        return anchor + baseOffset;
    }

    public bool CanUseExplosives(OUT_SoldierSquadAgent requester, Vector3 targetPoint)
    {
        if (!allowExplosives || CurrentOrder == SquadOrder.Regroup || CurrentOrder == SquadOrder.Retreat)
            return false;

        for (int i = 0; i < _agents.Count; i++)
        {
            OUT_SoldierSquadAgent other = _agents[i];
            if (other == null || other == requester || !other.isActiveAndEnabled)
                continue;

            float distance = Vector3.Distance(other.transform.position, targetPoint);
            if (distance <= explosiveSafetyRadius)
                return false;
        }

        return true;
    }

    private void RefreshOrder()
    {
        if (useForcedOrder)
            return;

        if (!HasRecentEnemy)
        {
            _currentOrder = SquadOrder.Hold;
            return;
        }

        int alive = 0;
        int lowHealth = 0;
        float closestToEnemy = float.MaxValue;

        for (int i = 0; i < _agents.Count; i++)
        {
            OUT_SoldierSquadAgent agent = _agents[i];
            if (agent == null || !agent.isActiveAndEnabled)
                continue;

            alive++;
            if (agent.IsLowHealth())
                lowHealth++;

            float distance = Vector3.Distance(agent.transform.position, _lastEnemyPosition);
            if (distance < closestToEnemy)
                closestToEnemy = distance;
        }

        if (alive <= 0)
        {
            _currentOrder = SquadOrder.Hold;
            return;
        }

        if (lowHealth >= Mathf.CeilToInt(alive * 0.5f))
        {
            _currentOrder = SquadOrder.TakeCover;
            return;
        }

        if (closestToEnemy < 7f)
        {
            _currentOrder = SquadOrder.Retreat;
            return;
        }

        if (closestToEnemy > 22f)
        {
            _currentOrder = SquadOrder.Advance;
            return;
        }

        _currentOrder = SquadOrder.Suppress;
    }

    private void RebuildSlots()
    {
        for (int i = 0; i < _agents.Count; i++)
        {
            if (_agents[i] != null)
                _agents[i].SetSlotIndex(i);
        }
    }
}
