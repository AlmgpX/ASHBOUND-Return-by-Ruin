using UnityEngine;

[DisallowMultipleComponent]
public class OUT_CameraShakeEmitter : MonoBehaviour, IOutCameraShakeSource
{
    [Header("Shake")]
    [SerializeField] private OUT_CameraShakeKind kind = OUT_CameraShakeKind.Generic;
    [SerializeField][Min(0f)] private float amplitude = 1f;
    [SerializeField][Min(0.01f)] private float frequency = 12f;
    [SerializeField][Min(0.01f)] private float duration = 0.35f;
    [SerializeField][Min(0f)] private float radius = 0f;
    [SerializeField] private bool requireGrounded = false;
    [SerializeField] private bool emitOnEnable = false;

    private void OnEnable()
    {
        if (emitOnEnable)
            Emit();
    }

    public OUT_CameraShakeRequest BuildShakeRequest()
    {
        return new OUT_CameraShakeRequest(
            instigator: gameObject,
            origin: transform.position,
            amplitude: amplitude,
            frequency: frequency,
            duration: duration,
            radius: radius,
            kind: kind,
            requireGrounded: requireGrounded);
    }

    public void Emit()
    {
        OUT_CameraShakeRequest request = BuildShakeRequest();
        OUT_CameraShakeService.Shake(request);
    }

    public void EmitAt(Vector3 position)
    {
        OUT_CameraShakeRequest request = new OUT_CameraShakeRequest(
            instigator: gameObject,
            origin: position,
            amplitude: amplitude,
            frequency: frequency,
            duration: duration,
            radius: radius,
            kind: kind,
            requireGrounded: requireGrounded);

        OUT_CameraShakeService.Shake(request);
    }
}
