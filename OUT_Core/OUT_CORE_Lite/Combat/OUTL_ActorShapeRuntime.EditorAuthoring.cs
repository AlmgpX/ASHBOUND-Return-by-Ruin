#if UNITY_EDITOR
using UnityEngine;

public sealed partial class OUTL_ActorShapeRuntime
{
    partial void ApplyHurtboxProfileEditorBoundary(bool clearExisting)
    {
        if (HurtboxRoot == null)
        {
            GameObject root = new GameObject("OUTL_Hurtboxes");
            root.transform.SetParent(transform, false);
            HurtboxRoot = root.transform;
        }

        if (clearExisting)
        {
            for (int i = HurtboxRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = HurtboxRoot.GetChild(i);
                if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        for (int i = 0; i < HurtboxProfile.Hurtboxes.Length; i++)
        {
            OUTL_HurtboxProfileEntry entry = HurtboxProfile.Hurtboxes[i];
            if (entry == null) continue;
            string id = string.IsNullOrEmpty(entry.Id) ? "hurtbox_" + i : entry.Id;
            Transform child = HurtboxRoot.Find(id);
            GameObject go = child != null ? child.gameObject : new GameObject(id);
            if (child == null) go.transform.SetParent(HurtboxRoot, false);
            go.transform.localPosition = entry.LocalCenter;
            go.transform.localRotation = Quaternion.Euler(entry.LocalEuler);
            go.SetActive(entry.EnabledByDefault);
            if (entry.Layer >= 0 && entry.Layer <= 31) go.layer = entry.Layer;

            Collider collider = EnsureHurtboxCollider(go, entry.Shape);
            ConfigureHurtboxCollider(collider, entry);

            OUTL_Hitbox hitbox = go.GetComponent<OUTL_Hitbox>();
            if (hitbox == null) hitbox = go.AddComponent<OUTL_Hitbox>();
            hitbox.Entity = Entity;
            hitbox.Zone = entry.Zone;
            hitbox.DamageMultiplier = Mathf.Max(0f, entry.DamageMultiplier);
            hitbox.UseZoneAsSuffix = true;
        }
    }

    private static Collider EnsureHurtboxCollider(GameObject go, OUTL_HurtboxShape shape)
    {
        BoxCollider box = go.GetComponent<BoxCollider>();
        SphereCollider sphere = go.GetComponent<SphereCollider>();
        CapsuleCollider capsule = go.GetComponent<CapsuleCollider>();

        if (shape == OUTL_HurtboxShape.Box)
        {
            if (sphere != null) DestroyHurtboxCollider(sphere);
            if (capsule != null) DestroyHurtboxCollider(capsule);
            return box != null ? box : go.AddComponent<BoxCollider>();
        }

        if (shape == OUTL_HurtboxShape.Sphere)
        {
            if (box != null) DestroyHurtboxCollider(box);
            if (capsule != null) DestroyHurtboxCollider(capsule);
            return sphere != null ? sphere : go.AddComponent<SphereCollider>();
        }

        if (box != null) DestroyHurtboxCollider(box);
        if (sphere != null) DestroyHurtboxCollider(sphere);
        return capsule != null ? capsule : go.AddComponent<CapsuleCollider>();
    }

    private static void ConfigureHurtboxCollider(Collider collider, OUTL_HurtboxProfileEntry entry)
    {
        if (collider == null || entry == null) return;
        collider.isTrigger = entry.IsTrigger;

        BoxCollider box = collider as BoxCollider;
        if (box != null)
        {
            box.center = Vector3.zero;
            box.size = Abs(entry.BoxSize);
            return;
        }

        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null)
        {
            sphere.center = Vector3.zero;
            sphere.radius = Mathf.Max(0.001f, entry.Radius);
            return;
        }

        CapsuleCollider capsule = collider as CapsuleCollider;
        if (capsule != null)
        {
            capsule.center = Vector3.zero;
            capsule.radius = Mathf.Max(0.001f, entry.Radius);
            capsule.height = Mathf.Max(capsule.radius * 2f, entry.Height);
            capsule.direction = 1;
        }
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private static void DestroyHurtboxCollider(Collider collider)
    {
        if (collider == null) return;
        UnityEngine.Object.DestroyImmediate(collider);
    }
}
#endif
