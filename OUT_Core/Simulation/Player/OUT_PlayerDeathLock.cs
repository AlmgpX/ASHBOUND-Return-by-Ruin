using UnityEngine;

[DisallowMultipleComponent]
public class OUT_PlayerDeathLock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_HealthSimple health;
    [SerializeField] private OUT_HL1PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    [Header("Death Lock")]
    [SerializeField] private bool disablePlayerControllerOnDeath = true;
    [SerializeField] private bool disableCharacterControllerOnDeath = false;
    [SerializeField] private bool unlockCursorOnDeath = true;

    [Header("Runtime")]
    [SerializeField] private bool locked;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();

        if (playerController == null)
            playerController = GetComponent<OUT_HL1PlayerController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += OnDied;

        RefreshLockState();
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void Update()
    {
        RefreshLockState();
    }

    private void OnDied(OUT_DamageContext context)
    {
        ApplyDeathLock();
    }

    private void RefreshLockState()
    {
        if (health != null && health.IsDead && !locked)
            ApplyDeathLock();
    }

    private void ApplyDeathLock()
    {
        locked = true;

        if (disablePlayerControllerOnDeath && playerController != null)
            playerController.enabled = false;

        if (disableCharacterControllerOnDeath && characterController != null)
            characterController.enabled = false;

        if (unlockCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
