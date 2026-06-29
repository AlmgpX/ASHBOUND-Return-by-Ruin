using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Core;

namespace OUT_RayMicro.Runtime;

public enum OutmSoundId : int
{
    Shot,
    Ricochet,
    Impact,
    Door,
    StepStone,
    StepWood,
    StepCarpet,
    StepWater
}

public sealed class OutmAudioSystem
{
    private readonly SoundBank[] banks;
    private readonly Random random = new(1337);
    private bool ready;
    private Music currentMusic;
    private bool hasMusic;

    public OutmAudioSystem()
    {
        int count = Enum.GetValues<OutmSoundId>().Length;
        banks = new SoundBank[count];
        for (int i = 0; i < banks.Length; i++)
            banks[i] = new SoundBank();
    }

    public void Load(OutmWorld world)
    {
        if (!Raylib.IsAudioDeviceReady())
            Raylib.InitAudioDevice();

        ready = Raylib.IsAudioDeviceReady();
        if (!ready)
        {
            world.PushLog("audio device failed");
            return;
        }

        LoadBank(OutmSoundId.Shot, FindFiles("audio/Weapon", "OUT_" + "BulletShot_*.wav"));
        LoadBank(OutmSoundId.Ricochet, FindFiles("audio/Misc", "Bullet" + "RicImpact.*").Concat(FindFiles("audio/Weapon", "OUT_Impact_Base.wav")));
        LoadBank(OutmSoundId.Impact, FindFiles("audio/Weapon", "OUT_Impact*.wav"));
        LoadBank(OutmSoundId.Door, FindFiles("audio/Misc", "DoorOpen.wav"));
        LoadBank(OutmSoundId.StepStone, FindFiles("audio/Footstep", "Step_*.wav").Concat(FindFiles("audio/Footstep", "step_*.wav")));
        LoadBank(OutmSoundId.StepWood, FindFiles("audio/Footstep", "Step_Wood_*.wav"));
        LoadBank(OutmSoundId.StepCarpet, FindFiles("audio/Footstep", "Step_Carpet_*.wav"));
        LoadBank(OutmSoundId.StepWater, FindFiles("audio/Footstep", "Step_Water_*.wav"));
        LoadFirstMusic(world);

        world.PushLog($"audio online: {LoadedSoundCount} sounds");
    }

    public void Update()
    {
        if (!ready || !hasMusic)
            return;

        Raylib.UpdateMusicStream(currentMusic);
    }

    public void ProcessEvents(OutmWorld world, Vector3 listenerPosition, Vector3 listenerRight)
    {
        if (!ready)
            return;

        while (world.Events.TryDequeue(out OutmEvent evt))
        {
            switch (evt.Type)
            {
                case OutmEventType.Fired:
                    PlaySpatial(OutmSoundId.Shot, evt.Point, listenerPosition, listenerRight, 1.0f, 2.0f, 32.0f, 0.94f, 1.08f);
                    break;
                case OutmEventType.ProjectileBounce:
                    PlaySpatial(OutmSoundId.Ricochet, evt.Point, listenerPosition, listenerRight, 0.88f, 1.0f, 24.0f, 0.90f, 1.18f);
                    break;
                case OutmEventType.ProjectileHit:
                    PlaySpatial(OutmSoundId.Impact, evt.Point, listenerPosition, listenerRight, 0.82f, 1.0f, 22.0f, 0.86f, 1.12f);
                    break;
                case OutmEventType.DoorToggled:
                    PlaySpatial(OutmSoundId.Door, evt.Point, listenerPosition, listenerRight, 0.80f, 2.0f, 20.0f, 0.96f, 1.04f);
                    break;
                case OutmEventType.Footstep:
                    PlaySpatial(OutmSoundId.StepStone, evt.Point, listenerPosition, listenerRight, 0.46f, 0.5f, 10.0f, 0.86f, 1.12f);
                    break;
            }
        }
    }

    public void Play(OutmSoundId id)
    {
        PlaySpatial(id, Vector3.Zero, Vector3.Zero, Vector3.UnitX, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, forceNonSpatial: true);
    }

    public void PlaySpatial(OutmSoundId id, Vector3 source, Vector3 listener, Vector3 listenerRight, float baseVolume, float nearDistance, float farDistance, float minPitch, float maxPitch, bool forceNonSpatial = false)
    {
        if (!ready)
            return;

        SoundBank bank = banks[(int)id];
        if (bank.Slots.Length == 0)
            return;

        int index = bank.Cursor++ % bank.Slots.Length;
        Sound sound = bank.Slots[index].Sound;

        float volume = baseVolume;
        float pan = 0.0f;

        if (!forceNonSpatial)
        {
            Vector3 toSource = source - listener;
            float distance = toSource.Length();
            volume *= ComputeAttenuation(distance, nearDistance, farDistance);

            if (distance > 0.0001f)
            {
                Vector3 direction = toSource / distance;
                Vector3 right = listenerRight.LengthSquared() > 0.0001f ? Vector3.Normalize(listenerRight) : Vector3.UnitX;
                pan = Math.Clamp(Vector3.Dot(direction, right), -1.0f, 1.0f);
            }
        }

        if (volume <= 0.001f)
            return;

        float pitch = RandomRange(minPitch, maxPitch);
        Raylib.SetSoundVolume(sound, Math.Clamp(volume, 0.0f, 1.0f));
        Raylib.SetSoundPitch(sound, Math.Clamp(pitch, 0.25f, 4.0f));
        Raylib.SetSoundPan(sound, Math.Clamp(pan, -1.0f, 1.0f));
        Raylib.PlaySound(sound);
    }

    public void Unload()
    {
        if (hasMusic)
        {
            Raylib.StopMusicStream(currentMusic);
            Raylib.UnloadMusicStream(currentMusic);
            hasMusic = false;
        }

        for (int b = 0; b < banks.Length; b++)
        {
            SoundSlot[] slots = banks[b].Slots;
            for (int i = 0; i < slots.Length; i++)
                Raylib.UnloadSound(slots[i].Sound);

            banks[b].Slots = Array.Empty<SoundSlot>();
            banks[b].Cursor = 0;
        }

        if (ready && Raylib.IsAudioDeviceReady())
            Raylib.CloseAudioDevice();

        ready = false;
    }

    private int LoadedSoundCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < banks.Length; i++)
                count += banks[i].Slots.Length;
            return count;
        }
    }

    private void LoadBank(OutmSoundId id, IEnumerable<string> paths)
    {
        var slots = new List<SoundSlot>();
        foreach (string path in paths)
        {
            if (!File.Exists(path))
                continue;

            Sound sound = Raylib.LoadSound(path);
            slots.Add(new SoundSlot(sound));
        }

        banks[(int)id].Slots = slots.ToArray();
        banks[(int)id].Cursor = 0;
    }

    private void LoadFirstMusic(OutmWorld world)
    {
        string folder = OutmAssetPaths.ResolveData("audio/Music");
        if (!Directory.Exists(folder))
            return;

        string? path = Directory.GetFiles(folder, "*.mp3", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(folder, "*.ogg", SearchOption.TopDirectoryOnly))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(path))
            return;

        currentMusic = Raylib.LoadMusicStream(path);
        Raylib.SetMusicVolume(currentMusic, 0.45f);
        Raylib.PlayMusicStream(currentMusic);
        hasMusic = true;
        world.PushLog($"music stream: {Path.GetFileName(path)}");
    }

    private float RandomRange(float min, float max)
    {
        if (max <= min)
            return min;

        return min + (float)random.NextDouble() * (max - min);
    }

    private static float ComputeAttenuation(float distance, float nearDistance, float farDistance)
    {
        if (distance <= nearDistance)
            return 1.0f;
        if (distance >= farDistance)
            return 0.0f;

        float t = (distance - nearDistance) / MathF.Max(0.001f, farDistance - nearDistance);
        float linear = 1.0f - t;
        return linear * linear;
    }

    private static IEnumerable<string> FindFiles(string relativeFolder, string pattern)
    {
        string folder = OutmAssetPaths.ResolveData(relativeFolder);
        if (!Directory.Exists(folder))
            return Array.Empty<string>();

        return Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class SoundBank
    {
        public SoundSlot[] Slots = Array.Empty<SoundSlot>();
        public int Cursor;
    }

    private readonly struct SoundSlot
    {
        public readonly Sound Sound;

        public SoundSlot(Sound sound)
        {
            Sound = sound;
        }
    }
}
