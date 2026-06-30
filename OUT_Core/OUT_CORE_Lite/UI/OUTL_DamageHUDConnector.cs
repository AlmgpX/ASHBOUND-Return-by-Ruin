using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OUTL_DamageHUDConnector : MonoBehaviour, OUTL_IEventListener
{
    public OUTL_EntityAdapter PlayerEntity;
    public Transform PlayerView;

    [Header("Health")]
    public Text HealthText;
    public Image FullscreenPainImage;
    public float FullscreenFadeSpeed = 2.5f;
    public float FullscreenMaxAlpha = 0.35f;

    [Header("Pain Compass")]
    public Image FrontImage;
    public Image RearImage;
    public Image LeftImage;
    public Image RightImage;
    public float CompassFadeSpeed = 2f;
    public float NearDamageDistance = 1.6f;
    public float DirectionThreshold = 0.3f;

    private float front;
    private float rear;
    private float left;
    private float right;
    private float fullscreen;
    private int lastHealthTextValue = int.MinValue;

    private void Awake()
    {
        if (PlayerEntity == null) PlayerEntity = GetComponentInParent<OUTL_EntityAdapter>();
        if (PlayerView == null && PlayerEntity != null) PlayerView = PlayerEntity.transform;
    }

    private void OnEnable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Register(this);
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null) OUTL_World.Instance.Events.Unregister(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        front = Mathf.Max(0f, front - CompassFadeSpeed * dt);
        rear = Mathf.Max(0f, rear - CompassFadeSpeed * dt);
        left = Mathf.Max(0f, left - CompassFadeSpeed * dt);
        right = Mathf.Max(0f, right - CompassFadeSpeed * dt);
        fullscreen = Mathf.Max(0f, fullscreen - FullscreenFadeSpeed * dt);

        SetAlpha(FrontImage, front);
        SetAlpha(RearImage, rear);
        SetAlpha(LeftImage, left);
        SetAlpha(RightImage, right);
        SetAlpha(FullscreenPainImage, Mathf.Min(fullscreen, FullscreenMaxAlpha));

        RefreshHealthText();
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (PlayerEntity == null || !PlayerEntity.Id.IsValid) return;
        if (evt.Target != PlayerEntity.Id) return;

        if (evt.Type == OUTL_EventType.Damaged)
        {
            fullscreen = FullscreenMaxAlpha;
            CalculateDamageDirection(evt.Point);
        }
        else if (evt.Type == OUTL_EventType.Healed)
        {
            if (HealthText != null && PlayerEntity.Runtime != null)
                RefreshHealthText();
        }
    }

    public void CalculateDamageDirection(Vector3 sourcePoint)
    {
        if (PlayerEntity == null || PlayerView == null) return;
        if (sourcePoint == Vector3.zero)
        {
            front = rear = left = right = 1f;
            return;
        }

        Vector3 origin = PlayerView.position;
        Vector3 from = sourcePoint - origin;
        float dist = from.magnitude;
        if (dist <= NearDamageDistance)
        {
            front = rear = left = right = 1f;
            return;
        }

        from.y = 0f;
        if (from.sqrMagnitude <= 0.0001f)
        {
            front = rear = left = right = 1f;
            return;
        }

        from.Normalize();
        Vector3 fwd = PlayerView.forward;
        Vector3 rgt = PlayerView.right;
        fwd.y = 0f;
        rgt.y = 0f;
        fwd.Normalize();
        rgt.Normalize();

        float side = Vector3.Dot(from, fwd);
        float lateral = Vector3.Dot(from, rgt);

        if (side > DirectionThreshold) front = Mathf.Max(front, side);
        else if (side < -DirectionThreshold) rear = Mathf.Max(rear, -side);

        if (lateral > DirectionThreshold) right = Mathf.Max(right, lateral);
        else if (lateral < -DirectionThreshold) left = Mathf.Max(left, -lateral);
    }

    private static void SetAlpha(Image image, float value)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = Mathf.Clamp01(value);
        image.color = c;
    }

    private void RefreshHealthText()
    {
        if (HealthText == null || PlayerEntity == null || PlayerEntity.Runtime == null) return;
        int value = Mathf.CeilToInt(PlayerEntity.Runtime.Stats.Get(OUTL_StatId.Health, 0f));
        if (value == lastHealthTextValue) return;
        lastHealthTextValue = value;
        HealthText.text = value.ToString();
    }
}
