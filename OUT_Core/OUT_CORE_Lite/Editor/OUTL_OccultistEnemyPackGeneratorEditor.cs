#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class OUTL_OccultistEnemyPackGeneratorEditor
{
    private const string Root = "Assets/OUT/OUT_Core/OUT_CORE_Lite/Generated/OccultistEnemyPack";
    private const string PrefabFolder = Root + "/Prefabs";
    private const string DefFolder = Root + "/Definitions";
    private const string ProfileFolder = Root + "/Profiles";
    private const string AudioFolder = Root + "/Audio";
    private const string RuntimeRigPath = PrefabFolder + "/OUTL_Occultist_RuntimeRig.prefab";
    private const string PopulationFieldPath = PrefabFolder + "/OUTL_Occultist_1000_AbstractField.prefab";

    static OUTL_OccultistEnemyPackGeneratorEditor()
    {
        EditorApplication.delayCall += AutoGenerate;
    }

    [MenuItem("OUT CORE Lite/Content/Generate Occultist Enemy Pack")]
    public static void Generate()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(DefFolder);
        EnsureFolder(ProfileFolder);
        EnsureFolder(AudioFolder);

        OUTL_FactionDef playerFaction = EnsureFaction("OUTL_Faction_Player.asset", "player", "Player");
        OUTL_FactionDef occultistFaction = EnsureFaction("OUTL_Faction_Occultists.asset", "occultists", "Occultists");
        playerFaction.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = playerFaction, Relation = 1f },
            new OUTL_FactionRelation { Faction = occultistFaction, Relation = -1f }
        };
        occultistFaction.Relations = new[]
        {
            new OUTL_FactionRelation { Faction = occultistFaction, Relation = 1f },
            new OUTL_FactionRelation { Faction = playerFaction, Relation = -1f }
        };
        EditorUtility.SetDirty(playerFaction);
        EditorUtility.SetDirty(occultistFaction);

        OUTL_AudioProfile alert = EnsureAudio("OUTL_Occultist_Alert.asset");
        OUTL_AudioProfile combat = EnsureAudio("OUTL_Occultist_Combat.asset");
        OUTL_AudioProfile pain = EnsureAudio("OUTL_Occultist_Pain.asset");
        OUTL_AudioProfile death = EnsureAudio("OUTL_Occultist_Death.asset");
        OUTL_CharacterIdentityProfile identityProfile = EnsureIdentityProfile();

        GameObject bullet = EnsureProjectilePrefab("OUTL_Projectile_OccultistBullet.prefab", false, 0.055f, 0.025f);
        GameObject grenade = EnsureProjectilePrefab("OUTL_Projectile_OccultistGrenade.prefab", true, 0.18f, 0.45f);

        OUTL_AttackProfile shotgun = EnsureAttack("OUTL_Attack_OccultistShotgun.asset", "occultist.shotgun", bullet, 5f, 52f, 1.15f, 75f, 8, 7.5f, 4.5f, false);
        OUTL_AttackProfile rifle = EnsureAttack("OUTL_Attack_OccultistRifle.asset", "occultist.rifle", bullet, 9f, 72f, 0.38f, 100f, 1, 1.25f, 0.7f, false);
        OUTL_AttackProfile smg = EnsureAttack("OUTL_Attack_OccultistSMG.asset", "occultist.smg", bullet, 4f, 55f, 0.105f, 88f, 1, 3.2f, 1.8f, false);
        OUTL_AttackProfile grenadeAttack = EnsureAttack("OUTL_Attack_OccultistGrenade.asset", "occultist.grenade", grenade, 8f, 42f, 3.2f, 14f, 1, 2f, 1f, true);
        grenadeAttack.ProjectileLifetime = 2.8f;
        grenadeAttack.ProjectileDetonateOnEntityHit = true;
        grenadeAttack.ProjectileDetonateOnWorldHit = false;
        grenadeAttack.ProjectileDetonateOnLifetimeEnd = true;
        grenadeAttack.ProjectileBounceOnWorldHit = true;
        grenadeAttack.ProjectileMaxBounces = 4;
        grenadeAttack.ProjectileBounceDamping = 0.72f;
        grenadeAttack.ProjectileFrictionDamping = 0.82f;
        grenadeAttack.UseExplosion = true;
        grenadeAttack.ExplosionRadius = 5.5f;
        grenadeAttack.ExplosionDamage = 48f;
        grenadeAttack.ExplosionFalloff = OUTL_ExplosionFalloff.Smooth;
        grenadeAttack.ExplosionRequireLineOfSight = true;
        grenadeAttack.ExplosionHitMask = ~0;
        grenadeAttack.ExplosionObstacleMask = ~0;
        grenadeAttack.AimMode = OUTL_AimMode.BallisticLowArc;
        EditorUtility.SetDirty(grenadeAttack);

        Variant[] variants =
        {
            new Variant("Shotgun", shotgun, null, 70f, 3.2f, 15f, 3f, 0f),
            new Variant("Rifle", rifle, null, 58f, 3.5f, 25f, 5f, 0f),
            new Variant("SMG", smg, null, 52f, 4.0f, 18f, 4f, 0f),
            new Variant("Grenadier", rifle, grenadeAttack, 72f, 3.2f, 27f, 7f, 0.65f),
            new Variant("Breacher", shotgun, grenadeAttack, 95f, 3.0f, 13f, 3f, 0.28f)
        };

        OUTL_EntityDef[] defs = new OUTL_EntityDef[variants.Length];
        GameObject[] enemyPrefabs = new GameObject[variants.Length];
        for (int i = 0; i < variants.Length; i++)
        {
            Variant variant = variants[i];
            OUTL_EnemyArchetypeProfile enemyProfile = EnsureEnemyProfile(variant, shotgun, grenadeAttack);
            OUTL_EntityDef def = EnsureEntityDef(variant);
            GameObject prefab = EnsureEnemyPrefab(variant, def, occultistFaction, enemyProfile, identityProfile, alert, combat, pain, death);
            def.Prefab = prefab;
            EditorUtility.SetDirty(def);
            defs[i] = def;
            enemyPrefabs[i] = prefab;
        }

        EnsureRuntimeRig(defs, enemyPrefabs, bullet, grenade);
        EnsurePopulationField(defs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeRigPath);
        Debug.Log("OUT CORE Lite: generated canonical Occultist Enemy Pack at " + Root);
    }

    private static void AutoGenerate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoGenerate;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeRigPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(PopulationFieldPath) != null &&
            !GeneratedPrefabsNeedRepair()) return;
        Generate();
    }

    private static bool GeneratedPrefabsNeedRepair()
    {
        string[] paths =
        {
            RuntimeRigPath,
            PrefabFolder + "/OUTL_Enemy_Occultist_Shotgun.prefab",
            PrefabFolder + "/OUTL_Enemy_Occultist_Rifle.prefab",
            PrefabFolder + "/OUTL_Enemy_Occultist_SMG.prefab",
            PrefabFolder + "/OUTL_Enemy_Occultist_Grenadier.prefab",
            PrefabFolder + "/OUTL_Enemy_Occultist_Breacher.prefab"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            OUTL_CharacterIdentity identity = prefab != null && i > 0 ? prefab.GetComponent<OUTL_CharacterIdentity>() : null;
            if (prefab == null ||
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) > 0 ||
                (i > 0 && (identity == null || identity.Profile == null)))
                return true;
        }

        return false;
    }

    private static OUTL_FactionDef EnsureFaction(string fileName, string id, string displayName)
    {
        string path = DefFolder + "/" + fileName;
        OUTL_FactionDef value = AssetDatabase.LoadAssetAtPath<OUTL_FactionDef>(path);
        if (value == null)
        {
            value = ScriptableObject.CreateInstance<OUTL_FactionDef>();
            AssetDatabase.CreateAsset(value, path);
        }
        value.FactionId = id;
        value.DisplayName = displayName;
        return value;
    }

    private static OUTL_AudioProfile EnsureAudio(string fileName)
    {
        string path = AudioFolder + "/" + fileName;
        OUTL_AudioProfile value = AssetDatabase.LoadAssetAtPath<OUTL_AudioProfile>(path);
        if (value == null)
        {
            value = ScriptableObject.CreateInstance<OUTL_AudioProfile>();
            value.Clips = new AudioClip[0];
            value.Volume = 0.8f;
            value.SpatialBlend = 1f;
            value.MinDistance = 3f;
            value.MaxDistance = 55f;
            AssetDatabase.CreateAsset(value, path);
        }
        return value;
    }

    private static OUTL_CharacterIdentityProfile EnsureIdentityProfile()
    {
        string path = ProfileFolder + "/OUTL_Identity_Occultist.asset";
        OUTL_CharacterIdentityProfile value = AssetDatabase.LoadAssetAtPath<OUTL_CharacterIdentityProfile>(path);
        if (value == null)
        {
            value = ScriptableObject.CreateInstance<OUTL_CharacterIdentityProfile>();
            AssetDatabase.CreateAsset(value, path);
        }

        value.ProfileId = "identity.occultist";
        value.Seed = 1976;
        value.GivenNames = new[]
        {
            "Elias", "Silas", "Jonah", "Caleb", "Gideon", "Mara", "Ruth", "Esther",
            "Viktor", "Anton", "Mikhail", "Nadia", "Zoya", "Irina", "Lev", "Yakov"
        };
        value.FamilyNames = new[]
        {
            "Blackwood", "Crowe", "Harrow", "Voss", "Rook", "Graves", "Mercer", "Vale",
            "Morozov", "Volkov", "Orlov", "Sokolov", "Belov", "Kravets", "Markin", "Radin"
        };
        value.Nicknames = new[]
        {
            "Ash", "Preacher", "Nails", "Hound", "Dust", "Red", "Pilgrim", "Knell",
            "Wolf", "Rat", "Deacon", "Scab"
        };
        value.NicknameChance = 0.28f;
        value.Courage = new Vector2(0.30f, 0.92f);
        value.Aggression = new Vector2(0.38f, 0.96f);
        value.Discipline = new Vector2(0.18f, 0.78f);
        value.Awareness = new Vector2(0.30f, 0.88f);
        value.Loyalty = new Vector2(0.24f, 0.90f);
        value.Greed = new Vector2(0.12f, 0.82f);
        EditorUtility.SetDirty(value);
        return value;
    }

    private static OUTL_AttackProfile EnsureAttack(string fileName, string id, GameObject projectile, float damage, float range, float cooldown, float speed, int count, float spreadH, float spreadV, bool gravity)
    {
        string path = ProfileFolder + "/" + fileName;
        OUTL_AttackProfile value = AssetDatabase.LoadAssetAtPath<OUTL_AttackProfile>(path);
        if (value == null)
        {
            value = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
            AssetDatabase.CreateAsset(value, path);
        }
        value.AttackId = id;
        value.Mode = OUTL_AttackMode.Projectile;
        value.Damage = damage;
        value.HitDamageKey = id;
        value.Range = range;
        value.Cooldown = cooldown;
        value.ProjectilePrefab = projectile;
        value.ProjectileSpeed = speed;
        value.ProjectileLifetime = Mathf.Max(1f, range / Mathf.Max(1f, speed) + 0.5f);
        value.ProjectileUsesGravity = gravity;
        value.ProjectileGravity = 9.81f;
        value.ProjectilesPerShot = Mathf.Max(1, count);
        value.ProjectileIgnoreTriggers = true;
        value.ProjectileDetonateOnEntityHit = false;
        value.ProjectileDetonateOnWorldHit = false;
        value.ProjectileDetonateOnLifetimeEnd = false;
        value.ProjectileBounceOnWorldHit = false;
        value.AimMode = gravity ? OUTL_AimMode.BallisticLowArc : OUTL_AimMode.PredictLinear;
        value.UseTargetVelocityPrediction = true;
        value.PredictionStrength = 0.72f;
        value.MaxPredictionTime = 1.1f;
        value.HorizontalSpreadDegrees = spreadH;
        value.VerticalSpreadDegrees = spreadV;
        value.MinSpreadDistance = 2f;
        value.HitMask = ~0;
        EditorUtility.SetDirty(value);
        return value;
    }

    private static OUTL_EnemyArchetypeProfile EnsureEnemyProfile(Variant variant, OUTL_AttackProfile shotgun, OUTL_AttackProfile grenade)
    {
        string path = ProfileFolder + "/OUTL_Enemy_Occultist" + variant.Name + ".asset";
        OUTL_EnemyArchetypeProfile value = AssetDatabase.LoadAssetAtPath<OUTL_EnemyArchetypeProfile>(path);
        if (value == null)
        {
            value = ScriptableObject.CreateInstance<OUTL_EnemyArchetypeProfile>();
            AssetDatabase.CreateAsset(value, path);
        }
        value.ArchetypeId = "occultist." + variant.Name.ToLowerInvariant();
        value.SightDistance = Mathf.Max(variant.PreferredRange + 20f, 48f);
        value.SightAngle = variant.Primary == shotgun ? 135f : 115f;
        value.EyeHeight = 1.62f;
        value.LineOfSightMask = ~0;
        value.TargetMemorySeconds = 7f;
        value.AcquireInterval = 0.22f;
        value.WanderRadius = 16f;
        value.WanderPointIntervalMin = 3f;
        value.WanderPointIntervalMax = 8f;
        value.HomeLeashDistance = 72f;
        value.ReturnStopDistance = 3f;
        value.PrimaryAttack = variant.Primary;
        value.SecondaryAttack = variant.Secondary;
        value.PreferredRange = variant.PreferredRange;
        value.MinimumRange = variant.MinimumRange;
        value.SecondaryMinimumRange = 10f;
        value.SecondaryMaximumRange = 38f;
        value.SecondaryChance = variant.SecondaryChance;
        value.TurnDegreesPerSecond = 540f;
        value.CombatStimulusRadius = 38f;
        value.FullInterval = 0.05f;
        value.NearInterval = 0.10f;
        value.MidInterval = 1.25f;
        value.FarInterval = 10f;
        value.DormantInterval = 60f;
        EditorUtility.SetDirty(value);
        return value;
    }

    private static OUTL_EntityDef EnsureEntityDef(Variant variant)
    {
        string path = DefFolder + "/OUTL_Entity_Occultist" + variant.Name + ".asset";
        OUTL_EntityDef value = AssetDatabase.LoadAssetAtPath<OUTL_EntityDef>(path);
        if (value == null)
        {
            value = ScriptableObject.CreateInstance<OUTL_EntityDef>();
            AssetDatabase.CreateAsset(value, path);
        }
        value.ClassName = "enemy.occultist." + variant.Name.ToLowerInvariant();
        value.DisplayName = "Occultist " + variant.Name;
        value.Tags = new[] { "Actor", "NPC", "Enemy", "Occultist", "Ranged", variant.Name };
        value.BaseStats = new[]
        {
            new OUTL_StatEntry { Key = "Health", Value = variant.Health },
            new OUTL_StatEntry { Key = "MaxHealth", Value = variant.Health },
            new OUTL_StatEntry { Key = "Damage", Value = variant.Primary != null ? variant.Primary.Damage : 1f },
            new OUTL_StatEntry { Key = "Speed", Value = variant.Speed },
            new OUTL_StatEntry { Key = "Armor", Value = variant.Name == "Breacher" ? 12f : 0f }
        };
        EditorUtility.SetDirty(value);
        return value;
    }

    private static GameObject EnsureProjectilePrefab(string fileName, bool grenade, float scale, float mass)
    {
        string path = PrefabFolder + "/" + fileName;
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = System.IO.Path.GetFileNameWithoutExtension(fileName);
        go.transform.localScale = Vector3.one * scale;
        SphereCollider collider = go.GetComponent<SphereCollider>();
        collider.isTrigger = false;
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.mass = mass;
        body.drag = grenade ? 0.05f : 0f;
        body.angularDrag = grenade ? 0.05f : 0f;
        body.useGravity = grenade;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (!grenade) body.constraints = RigidbodyConstraints.FreezeRotation;
        OUTL_RigidbodyProjectile projectile = go.AddComponent<OUTL_RigidbodyProjectile>();
        projectile.Body = body;
        projectile.BodyCollider = collider;
        projectile.LifetimeCheckInterval = grenade ? 0.05f : 0.03f;
        projectile.AlignToVelocity = !grenade;
        projectile.ReleaseWhenTooSlow = false;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject EnsureEnemyPrefab(Variant variant, OUTL_EntityDef def, OUTL_FactionDef faction, OUTL_EnemyArchetypeProfile profile, OUTL_CharacterIdentityProfile identityProfile, OUTL_AudioProfile alert, OUTL_AudioProfile combat, OUTL_AudioProfile pain, OUTL_AudioProfile deathAudio)
    {
        string path = PrefabFolder + "/OUTL_Enemy_Occultist_" + variant.Name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(contents) > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);

            OUTL_EntityAdapter repairedEntity = contents.GetComponent<OUTL_EntityAdapter>();
            OUTL_CharacterIdentity repairedIdentity = contents.GetComponent<OUTL_CharacterIdentity>();
            if (repairedIdentity == null)
                repairedIdentity = contents.AddComponent<OUTL_CharacterIdentity>();
            repairedIdentity.Entity = repairedEntity;
            repairedIdentity.Profile = identityProfile;
            repairedIdentity.GenerateFromStableId = true;
            repairedIdentity.Role = "occultist." + variant.Name.ToLowerInvariant();
            repairedIdentity.Background = "interstate.outpost";

            OUTL_ProcessingTierController repairedTiers = contents.GetComponent<OUTL_ProcessingTierController>();
            if (repairedTiers == null)
                repairedTiers = contents.AddComponent<OUTL_ProcessingTierController>();

            NavMeshAgent repairedAgent = contents.GetComponent<NavMeshAgent>();
            OUTL_NavMeshMover repairedMover = contents.GetComponent<OUTL_NavMeshMover>();
            if (repairedMover != null)
            {
                repairedMover.SnapToNavMeshOnEnable = true;
                repairedMover.NavMeshSpawnSampleDistance = 6f;
            }
            repairedTiers.BehaviourMinimumTier = OUTL_RuntimeTier.Near;
            repairedTiers.AnimatorMinimumTier = OUTL_RuntimeTier.Near;
            repairedTiers.AudioMinimumTier = OUTL_RuntimeTier.Near;
            repairedTiers.RendererMinimumTier = OUTL_RuntimeTier.Far;
            repairedTiers.GameObjectMinimumTier = OUTL_RuntimeTier.Near;
            repairedTiers.Behaviours = repairedAgent != null ? new Behaviour[] { repairedAgent } : new Behaviour[0];
            repairedTiers.Animators = contents.GetComponentsInChildren<Animator>(true);
            repairedTiers.AudioSources = contents.GetComponentsInChildren<AudioSource>(true);
            repairedTiers.Renderers = contents.GetComponentsInChildren<Renderer>(true);
            repairedTiers.GameObjects = new GameObject[0];

            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        GameObject root = new GameObject("OUTL_Enemy_Occultist_" + variant.Name);
        root.tag = "Enemy";

        CapsuleCollider bodyCollider = root.AddComponent<CapsuleCollider>();
        bodyCollider.center = new Vector3(0f, 1f, 0f);
        bodyCollider.height = 2f;
        bodyCollider.radius = 0.38f;

        NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 2f;
        agent.speed = variant.Speed;
        agent.acceleration = 18f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = Mathf.Max(1.5f, variant.MinimumRange);
        agent.autoBraking = true;

        OUTL_EntityAdapter entity = root.AddComponent<OUTL_EntityAdapter>();
        entity.Def = def;
        entity.Faction = faction;
        entity.ClassNameOverride = def.ClassName;
        entity.SavePersistent = true;
        entity.RestoreSpawnIfMissing = true;
        entity.RegisterTick = true;
        entity.TickLane = OUTL_TickLane.Logic;
        entity.TickInterval = 0.5f;
        entity.RegisterRandomTick = false;
        entity.RegisterInSectors = true;

        OUTL_CharacterIdentity identity = root.AddComponent<OUTL_CharacterIdentity>();
        identity.Entity = entity;
        identity.Profile = identityProfile;
        identity.GenerateFromStableId = true;
        identity.Role = "occultist." + variant.Name.ToLowerInvariant();
        identity.Background = "interstate.outpost";

        OUTL_Hitbox hitbox = root.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        hitbox.Zone = OUTL_HitboxZone.Torso;

        OUTL_DamageReceiver damage = root.AddComponent<OUTL_DamageReceiver>();
        damage.Entity = entity;

        OUTL_Vitals vitals = root.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        vitals.DefaultHealth = variant.Health;
        vitals.DefaultMaxHealth = variant.Health;
        vitals.RegisterTick = false;

        OUTL_FallDamageSensor fallDamage = root.AddComponent<OUTL_FallDamageSensor>();
        fallDamage.Entity = entity;
        fallDamage.GroundProbeCollider = bodyCollider;
        fallDamage.TickLane = OUTL_TickLane.Full;
        fallDamage.TickInterval = 0.02f;

        OUTL_DeathHandler death = root.AddComponent<OUTL_DeathHandler>();
        death.Entity = entity;
        death.DisableAI = false;
        death.DisableColliders = true;
        death.DisableRenderers = true;
        death.QueueDespawn = true;
        death.DespawnDelay = 2.5f;

        OUTL_NavMeshMover mover = root.AddComponent<OUTL_NavMeshMover>();
        mover.Entity = entity;
        mover.Agent = agent;
        mover.AutoFindAgent = false;
        mover.UseOUTLTick = true;
        mover.UseUnityUpdateForFullNear = false;
        mover.AllowLegacyUpdateTick = false;
        mover.AllowVisualUpdate = false;
        mover.FallbackSpeed = variant.Speed;
        mover.RotationSpeed = 720f;
        mover.RepathInterval = 0.3f;
        mover.StopDistance = Mathf.Max(1.5f, variant.MinimumRange);
        mover.AffectedByGravity = false;
        mover.SnapToNavMeshOnEnable = true;
        mover.NavMeshSpawnSampleDistance = 6f;

        GameObject eye = new GameObject("Eye");
        eye.transform.SetParent(root.transform, false);
        eye.transform.localPosition = new Vector3(0f, 1.62f, 0.08f);

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(root.transform, false);
        muzzle.transform.localPosition = new Vector3(0.22f, 1.35f, 0.62f);

        OUTL_AttackDriver attack = root.AddComponent<OUTL_AttackDriver>();
        attack.Source = entity;
        attack.Muzzle = muzzle.transform;
        attack.Primary = profile.PrimaryAttack;
        attack.Secondary = profile.SecondaryAttack;
        attack.RespectCooldownOnFireAt = true;

        OUTL_EnemyBarkDriver bark = root.AddComponent<OUTL_EnemyBarkDriver>();
        bark.Entity = entity;
        bark.Alert = alert;
        bark.Combat = combat;
        bark.Pain = pain;
        bark.Death = deathAudio;

        OUTL_OutpostEnemyBrain brain = root.AddComponent<OUTL_OutpostEnemyBrain>();
        brain.Entity = entity;
        brain.NavMover = mover;
        brain.AttackDriver = attack;
        brain.BarkDriver = bark;
        brain.Profile = profile;
        brain.Eye = eye.transform;

        GameObject visualRoot = new GameObject("VisualRoot_REPLACE_ME");
        visualRoot.transform.SetParent(root.transform, false);
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        placeholder.name = "PlaceholderBody_DELETE_ME";
        placeholder.transform.SetParent(visualRoot.transform, false);
        placeholder.transform.localPosition = Vector3.up;
        Collider placeholderCollider = placeholder.GetComponent<Collider>();
        if (placeholderCollider != null) Object.DestroyImmediate(placeholderCollider);
        Renderer placeholderRenderer = placeholder.GetComponent<Renderer>();
        if (placeholderRenderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = VariantColor(variant.Name);
            string materialPath = ProfileFolder + "/OUTL_Occultist_" + variant.Name + "_Placeholder.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            placeholderRenderer.sharedMaterial = material;
        }

        OUTL_ProcessingTierController tiers = root.AddComponent<OUTL_ProcessingTierController>();
        tiers.BehaviourMinimumTier = OUTL_RuntimeTier.Near;
        tiers.AnimatorMinimumTier = OUTL_RuntimeTier.Near;
        tiers.AudioMinimumTier = OUTL_RuntimeTier.Near;
        tiers.RendererMinimumTier = OUTL_RuntimeTier.Far;
        tiers.GameObjectMinimumTier = OUTL_RuntimeTier.Near;
        tiers.Behaviours = new Behaviour[] { agent };
        tiers.Animators = new Animator[0];
        tiers.AudioSources = new AudioSource[0];
        tiers.Renderers = placeholderRenderer != null ? new[] { placeholderRenderer } : new Renderer[0];
        tiers.GameObjects = new GameObject[0];

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureRuntimeRig(OUTL_EntityDef[] defs, GameObject[] enemies, GameObject bullet, GameObject grenade)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeRigPath);
        bool loadedPrefab = existing != null;
        GameObject root = loadedPrefab
            ? PrefabUtility.LoadPrefabContents(RuntimeRigPath)
            : new GameObject("OUTL_Occultist_RuntimeRig");

        if (loadedPrefab)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

        OUTL_SaveSpawnResolverRegistry resolver = root.GetComponent<OUTL_SaveSpawnResolverRegistry>();
        if (resolver == null)
            resolver = root.AddComponent<OUTL_SaveSpawnResolverRegistry>();
        resolver.EntityDefs = defs;
        resolver.RequireRestoreSpawnIfMissingFlag = true;
        resolver.AllowBareEntityFallback = false;

        OUTL_PoolPrewarmPlan prewarm = root.GetComponent<OUTL_PoolPrewarmPlan>();
        if (prewarm == null)
            prewarm = root.AddComponent<OUTL_PoolPrewarmPlan>();
        OUTL_PoolPrewarmEntry[] entries = new OUTL_PoolPrewarmEntry[2 + enemies.Length];
        entries[0] = new OUTL_PoolPrewarmEntry { Prefab = bullet, Count = 128 };
        entries[1] = new OUTL_PoolPrewarmEntry { Prefab = grenade, Count = 16 };
        for (int i = 0; i < enemies.Length; i++)
            entries[2 + i] = new OUTL_PoolPrewarmEntry { Prefab = enemies[i], Count = i == 0 ? 12 : 6 };
        prewarm.Entries = entries;
        prewarm.PrewarmOnStart = true;

        PrefabUtility.SaveAsPrefabAsset(root, RuntimeRigPath);
        if (loadedPrefab)
            PrefabUtility.UnloadPrefabContents(root);
        else
            Object.DestroyImmediate(root);
    }

    private static void EnsurePopulationField(OUTL_EntityDef[] defs)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PopulationFieldPath) != null) return;
        GameObject root = new GameObject("OUTL_Occultist_1000_AbstractField");
        OUTL_EnemyPopulationField field = root.AddComponent<OUTL_EnemyPopulationField>();
        field.Variants = defs;
        field.Count = 1000;
        field.Size = new Vector2(400f, 400f);
        field.Seed = 1976;
        field.StableIdPrefix = "occultist.interstate";
        field.RegisterOnStart = true;
        PrefabUtility.SaveAsPrefabAsset(root, PopulationFieldPath);
        Object.DestroyImmediate(root);
    }

    private static Color VariantColor(string name)
    {
        switch (name)
        {
            case "Shotgun": return new Color(0.45f, 0.08f, 0.08f);
            case "Rifle": return new Color(0.12f, 0.25f, 0.48f);
            case "SMG": return new Color(0.2f, 0.42f, 0.2f);
            case "Grenadier": return new Color(0.42f, 0.32f, 0.08f);
            default: return new Color(0.35f, 0.08f, 0.4f);
        }
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private struct Variant
    {
        public string Name;
        public OUTL_AttackProfile Primary;
        public OUTL_AttackProfile Secondary;
        public float Health;
        public float Speed;
        public float PreferredRange;
        public float MinimumRange;
        public float SecondaryChance;

        public Variant(string name, OUTL_AttackProfile primary, OUTL_AttackProfile secondary, float health, float speed, float preferredRange, float minimumRange, float secondaryChance)
        {
            Name = name;
            Primary = primary;
            Secondary = secondary;
            Health = health;
            Speed = speed;
            PreferredRange = preferredRange;
            MinimumRange = minimumRange;
            SecondaryChance = secondaryChance;
        }
    }
}
#endif
