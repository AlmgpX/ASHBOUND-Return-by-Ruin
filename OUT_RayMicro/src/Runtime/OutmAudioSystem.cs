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

    public void ProcessEvents(OutmWorld world)
    {
        if (!ready)
            return;

        while (world.Events.TryDequeue(out OutmEvent evt))
        {
            switch (evt.Type)
            {
                case OutmEventType.Fired:
                    Play(OutmSoundId.Shot);
                    break;
                case OutmEventType.ProjectileBounce:
                    Play(OutmSoundId.Ricochet);
                    break;
                case OutmEventType.ProjectileHit:
                    Play(OutmSoundId.Impact);
                    break;
                case OutmEventType.DoorToggled:
                    Play(OutmSoundId.Door);
                    break;
            }
        }
    }

    public void Play(OutmSoundId id)
    {
        if (!ready)
            return;

        SoundBank bank = banks[(int)id];
        if (bank.Slots.Length == 0)
            return;

        int index = bank.Cursor++ % bank.Slots.Length;
        Raylib.PlaySound(bank.Slots[index].Sound);
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
