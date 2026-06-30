using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIAnimationDriverSimple : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private OUT_AIActorBrain brain;

    [Header("Movement")]
    [SerializeField] private bool writeIsMoving = true;
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private bool writeMoveSpeed = true;
    [SerializeField] private string moveSpeedParameter = "MoveSpeed";
    [SerializeField] private float moveSpeedScale = 1f;

    [Header("State")]
    [SerializeField] private bool writeAIStateInt = false;
    [SerializeField] private string aiStateParameter = "AIState";
    [SerializeField] private bool writeAlertBool = false;
    [SerializeField] private string alertBoolParameter = "IsAlert";
    [SerializeField] private bool writeCombatBool = false;
    [SerializeField] private string combatBoolParameter = "IsCombat";

    private IOutAILocomotion _locomotion;

    private void Reset()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (brain == null)
            brain = GetComponent<OUT_AIActorBrain>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (brain == null)
            brain = GetComponent<OUT_AIActorBrain>();

        _locomotion = FindInterface<IOutAILocomotion>();
    }

    private void Update()
    {
        if (animator == null)
            return;

        bool isMoving = _locomotion != null && _locomotion.IsMoving;
        float moveSpeed = _locomotion != null ? _locomotion.Velocity.magnitude * Mathf.Max(0f, moveSpeedScale) : 0f;
        OUT_AIState state = brain != null ? brain.CurrentState : OUT_AIState.Idle;

        if (writeIsMoving && !string.IsNullOrEmpty(isMovingParameter))
            animator.SetBool(isMovingParameter, isMoving);

        if (writeMoveSpeed && !string.IsNullOrEmpty(moveSpeedParameter))
            animator.SetFloat(moveSpeedParameter, moveSpeed);

        if (writeAIStateInt && !string.IsNullOrEmpty(aiStateParameter))
            animator.SetInteger(aiStateParameter, (int)state);

        if (writeAlertBool && !string.IsNullOrEmpty(alertBoolParameter))
            animator.SetBool(alertBoolParameter, state == OUT_AIState.Alert);

        if (writeCombatBool && !string.IsNullOrEmpty(combatBoolParameter))
            animator.SetBool(combatBoolParameter, state == OUT_AIState.Combat);
    }

    private T FindInterface<T>() where T : class
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is T match)
                return match;
        }

        return null;
    }
}
