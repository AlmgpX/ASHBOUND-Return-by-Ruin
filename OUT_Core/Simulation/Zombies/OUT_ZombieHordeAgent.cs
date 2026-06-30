using OUTPool = OutCore.pool.OUT;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_ZombieHordeAgent : MonoBehaviour, IDamageable, IOutPoolResettable, OUTL_IPoolReset, IOutRuntimeTierReceiver
{
    [Header("Profile")]
    public OUT_ZombieHordeProfile Profile;

    [Header("References")]
    public Transform VisualRoot;
    public CharacterController CharacterController;
    public Collider BodyCollider;
    public Animator Animator;
    public AudioSource AudioSource;
    public AudioClip[] Moans;
    public AudioClip[] AttackSounds;
    public AudioClip[] HitSounds;
    public AudioClip[] DeathSounds;

    [Header("Runtime")]
    public bool RegisterOnEnable = true;
    public bool UseGravityGrounding = true;
    public bool UseAnimator = true;
    public bool UseAudio = true;
    public bool UseGibsOnDeath = true;
    public bool DebugDrawTarget = false;

    private int health;
    private bool dead;
    private OUT_RuntimeTier runtimeTier = OUT_RuntimeTier.Full;
    private OUT_ZombieTarget target;
    private Vector3 velocity;
    private float nextTargetRefresh;
    private float nextAttackTime;
    private float nextMoanTime;
    private int speedHash;
    private int attackHash;
    private int hitHash;
    private int deathHash;
    private int aliveHash;

    public bool IsDead { get { return dead; } }
    public OUT_ZombieTarget Target { get { return target; } }
    public OUT_RuntimeTier RuntimeTier { get { return runtimeTier; } }

    public int Health
    {
        get { return health; }
        set
        {
            if (dead)
                return;

            health = value;
            if (health <= 0)
                Die(transform.position + Vector3.up, Vector3.up);
        }
    }

    private void Awake()
    {
        CacheReferences();
        RebuildAnimatorHashes();
        ResetRuntimeState();
    }

    private void OnEnable()
    {
        CacheReferences();
        ResetRuntimeState();
        if (RegisterOnEnable) OUT_ZombieHordeSystem.Register(this);
    }

    private void OnDisable()
    {
        if (RegisterOnEnable) OUT_ZombieHordeSystem.Unregister(this);
    }

    private void CacheReferences()
    {
        if (VisualRoot == null) VisualRoot = transform;
        if (CharacterController == null) CharacterController = GetComponent<CharacterController>();
        if (BodyCollider == null) BodyCollider = GetComponent<Collider>();
        if (Animator == null) Animator = GetComponentInChildren<Animator>();
        if (AudioSource == null) AudioSource = GetComponent<AudioSource>();
        if (Profile == null) Profile = OUT_ZombieHordeSystem.DefaultProfile;
    }

    private void RebuildAnimatorHashes()
    {
        OUT_ZombieHordeProfile p = Profile;
        speedHash = Animator.StringToHash(p != null ? p.AnimatorSpeedFloat : "Speed");
        attackHash = Animator.StringToHash(p != null ? p.AnimatorAttackTrigger : "Attack");
        hitHash = Animator.StringToHash(p != null ? p.AnimatorHitTrigger : "Hit");
        deathHash = Animator.StringToHash(p != null ? p.AnimatorDeathTrigger : "Death");
        aliveHash = Animator.StringToHash(p != null ? p.AnimatorAliveBool : "Alive");
    }

    private void ResetRuntimeState()
    {
        OUT_ZombieHordeProfile p = GetProfile();
        health = p != null ? p.MaxHealth : 35;
        dead = false;
        target = null;
        velocity = Vector3.zero;
        nextTargetRefresh = 0f;
        nextAttackTime = 0f;
        nextMoanTime = Time.time + Random.Range(1f, 5f);
        if (BodyCollider != null) BodyCollider.enabled = true;
        if (CharacterController != null) CharacterController.enabled = true;
        if (Animator != null && UseAnimator) Animator.SetBool(aliveHash, true);
    }

    public void HordeTick(float now, float deltaTime, Vector3 hordeCenter)
    {
        if (dead || !isActiveAndEnabled) return;

        OUT_ZombieHordeProfile p = GetProfile();
        if (p == null) return;

        if (target == null || now >= nextTargetRefresh || !target.CanBeTargeted)
        {
            nextTargetRefresh = now + p.TargetRefreshInterval + Random.Range(0f, p.TargetRefreshInterval * 0.35f);
            target = OUT_ZombieTargetHub.EnsureExists().FindBestTarget(transform.position, p, target);
        }

        if (target == null)
        {
            UpdateAnimator(0f);
            return;
        }

        Vector3 toTarget = target.Position - transform.position;
        toTarget.y = 0f;
        float sqr = toTarget.sqrMagnitude;
        float attackDistance = p.AttackRange + target.Radius;

        if (sqr <= attackDistance * attackDistance)
        {
            UpdateAnimator(0f);
            FaceDirection(toTarget, p, deltaTime);
            TryAttack(now, p);
            TryMoan(now, p, hordeCenter);
            return;
        }

        float speed = runtimeTier == OUT_RuntimeTier.Near || runtimeTier == OUT_RuntimeTier.Full ? p.RunSpeed : p.WalkSpeed;
        Vector3 dir = sqr > 0.001f ? toTarget.normalized : Vector3.zero;
        Move(dir, speed, p, deltaTime);
        FaceDirection(dir, p, deltaTime);
        UpdateAnimator(speed);
        TryMoan(now, p, hordeCenter);

        if (DebugDrawTarget) Debug.DrawLine(transform.position + Vector3.up, target.Position + Vector3.up, Color.red, 0.05f);
    }

    private void Move(Vector3 dir, float speed, OUT_ZombieHordeProfile p, float deltaTime)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 motion = dir * speed;
        if (UseGravityGrounding)
        {
            velocity.y += Physics.gravity.y * deltaTime;
            if (velocity.y < -24f) velocity.y = -24f;
            motion.y = velocity.y;
        }

        if (CharacterController != null && CharacterController.enabled)
        {
            CharacterController.Move(motion * deltaTime);
            if (CharacterController.isGrounded) velocity.y = -0.5f;
            return;
        }

        transform.position += motion * deltaTime;
        if (UseGravityGrounding) SnapToGround(p);
    }

    private void SnapToGround(OUT_ZombieHordeProfile p)
    {
        if (p == null) return;
        Vector3 origin = transform.position + Vector3.up * p.GroundRayHeight;
        RaycastHit hit;
        if (Physics.Raycast(origin, Vector3.down, out hit, p.GroundRayDistance, p.GroundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
            velocity.y = 0f;
        }
    }

    private void FaceDirection(Vector3 dir, OUT_ZombieHordeProfile p, float deltaTime)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, p.RotationSpeed * deltaTime);
    }

    private void TryAttack(float now, OUT_ZombieHordeProfile p)
    {
        if (now < nextAttackTime || target == null) return;
        nextAttackTime = now + p.AttackInterval + Random.Range(0f, p.AttackInterval * 0.2f);

        Vector3 hitPoint = target.Position;
        Vector3 hitNormal = (target.Position - transform.position).sqrMagnitude > 0.001f ? (target.Position - transform.position).normalized : transform.forward;
        target.ApplyZombieDamage(p.AttackDamage, hitPoint, hitNormal);

        if (Animator != null && UseAnimator && runtimeTier != OUT_RuntimeTier.Far && runtimeTier != OUT_RuntimeTier.Dormant)
            Animator.SetTrigger(attackHash);

        PlayRandom(AttackSounds, p, forceNearOnly: true);
    }

    private void TryMoan(float now, OUT_ZombieHordeProfile p, Vector3 hordeCenter)
    {
        if (!UseAudio || AudioSource == null || Moans == null || Moans.Length == 0) return;
        if (runtimeTier == OUT_RuntimeTier.Far || runtimeTier == OUT_RuntimeTier.Dormant) return;
        if (now < nextMoanTime) return;

        float min = Mathf.Min(p.MoanMinInterval, p.MoanMaxInterval);
        float max = Mathf.Max(p.MoanMinInterval, p.MoanMaxInterval);
        nextMoanTime = now + Random.Range(min, max);

        if (Random.value > p.MoanChance) return;
        PlayRandom(Moans, p, forceNearOnly: true);
    }

    private void PlayRandom(AudioClip[] clips, OUT_ZombieHordeProfile p, bool forceNearOnly)
    {
        if (!UseAudio || AudioSource == null || clips == null || clips.Length == 0) return;
        if (forceNearOnly && !(runtimeTier == OUT_RuntimeTier.Near || runtimeTier == OUT_RuntimeTier.Full)) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        AudioSource.PlayOneShot(clip);
    }

    private void UpdateAnimator(float speed)
    {
        if (!UseAnimator || Animator == null) return;
        if (runtimeTier == OUT_RuntimeTier.Far || runtimeTier == OUT_RuntimeTier.Dormant)
        {
            Animator.speed = 0f;
            return;
        }
        Animator.speed = 1f;
        Animator.SetFloat(speedHash, speed);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position + Vector3.up, -transform.forward);
    }

    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (dead || damage <= 0) return;
        Health = health - damage;

        OUT_ZombieHordeProfile p = GetProfile();
        if (p != null && p.HitVFX != null) SpawnVFX(p.HitVFX, hitPoint, Quaternion.LookRotation(hitNormal.sqrMagnitude > 0.001f ? hitNormal : Vector3.up));

        if (!dead && Animator != null && UseAnimator && runtimeTier != OUT_RuntimeTier.Far && runtimeTier != OUT_RuntimeTier.Dormant)
            Animator.SetTrigger(hitHash);

        if (!dead)
            PlayRandom(HitSounds, p, forceNearOnly: true);
    }

    private void Die(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (dead) return;
        dead = true;
        OUT_ZombieHordeSystem.Unregister(this);

        OUT_ZombieHordeProfile p = GetProfile();
        if (Animator != null && UseAnimator) Animator.SetBool(aliveHash, false);
        if (Animator != null && UseAnimator && runtimeTier != OUT_RuntimeTier.Far && runtimeTier != OUT_RuntimeTier.Dormant) Animator.SetTrigger(deathHash);

        PlayRandom(DeathSounds, p, forceNearOnly: true);

        if (UseGibsOnDeath && p != null && p.GibVFX != null)
            SpawnVFX(p.GibVFX, hitPoint, Quaternion.LookRotation(hitNormal.sqrMagnitude > 0.001f ? hitNormal : Vector3.up));

        if (BodyCollider != null) BodyCollider.enabled = false;
        if (CharacterController != null) CharacterController.enabled = false;

        if (p != null && p.HideInsteadOfDestroy)
        {
            gameObject.SetActive(false);
        }
        else
        {
            if (OUTPool.IsManaged(gameObject)) OUTPool.Destroy(gameObject);
            else Destroy(gameObject);
        }
    }

    private void SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;
        OUTPool.Instantiate(prefab, position, rotation);
    }

    private OUT_ZombieHordeProfile GetProfile()
    {
        if (Profile != null) return Profile;
        return OUT_ZombieHordeSystem.DefaultProfile;
    }

    public void OnRuntimeTierChanged(OUT_RuntimeTier oldTier, OUT_RuntimeTier newTier)
    {
        runtimeTier = newTier;
    }

    public void OnTakenFromPool()
    {
        ResetRuntimeState();
        if (RegisterOnEnable) OUT_ZombieHordeSystem.Register(this);
    }

    public void OnReturnedToPool()
    {
        if (RegisterOnEnable) OUT_ZombieHordeSystem.Unregister(this);
    }

    public void OUTL_OnPoolSpawn()
    {
        OnTakenFromPool();
    }

    public void OUTL_OnPoolRelease()
    {
        OnReturnedToPool();
    }
}
