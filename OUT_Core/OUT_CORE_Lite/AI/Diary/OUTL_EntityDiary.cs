using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_EntityDiary : MonoBehaviour, OUTL_IEventListener, OUTL_ITickable
{
    public OUTL_EntityAdapter Entity;
    public OUTL_DiaryLineSet Lines;
    public bool WriteToFile = true;
    public bool BufferedFileWrite = true;
    public float FlushInterval = 1.5f;
    public bool MirrorToConsoleTrace = false;
    public int MaxMemoryLines = 64;
    public string FolderName = "OUTL_Diaries";

    [Header("Noise Control")]
    public bool EnableEventThrottle = true;
    public float DefaultEventThrottle = 0.35f;
    public float AttackThrottle = 1.25f;
    public float DamageThrottle = 0.4f;
    public float PatrolThrottle = 1.5f;
    public bool LogFileErrorsOnce = true;

    private readonly List<string> memory = new List<string>(64);
    private readonly StringBuilder fileBuffer = new StringBuilder(1024);
    private readonly float[] nextAllowedEventTimes = new float[16];
    private string filePath;
    private int lastBoundEntityId;
    private int ringWriteIndex;
    private bool ringWrapped;
    private float nextFlushTime;
    private bool reportedFileError;
    private bool registeredTick;

    public IReadOnlyList<string> Memory { get { return memory; } }
    public string FilePath { get { EnsurePathIsCurrent(); return filePath; } }
    public bool OUTL_IsTickEnabled { get { return isActiveAndEnabled && WriteToFile && BufferedFileWrite && fileBuffer.Length > 0; } }
    public OUTL_TickLane OUTL_TickLane { get { return OUTL_TickLane.Logic; } }
    public float OUTL_TickInterval { get { return Mathf.Max(0.1f, FlushInterval); } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        BuildPath();
    }

    private void OnEnable()
    {
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Register(this);
            OUTL_World.Instance.Scheduler.Register(this);
            registeredTick = true;
        }
        nextFlushTime = Time.unscaledTime + Mathf.Max(0.1f, FlushInterval);
        Write(OUTL_DiaryEventType.Spawn, "spawn", true);
    }

    private void OnDisable()
    {
        if (OUTL_World.Instance != null)
        {
            OUTL_World.Instance.Events.Unregister(this);
            if (registeredTick) OUTL_World.Instance.Scheduler.Unregister(this);
        }
        registeredTick = false;
        FlushFileBuffer();
    }

    public void OUTL_Tick(OUTL_World world, float time, float deltaTime)
    {
        if (!WriteToFile || !BufferedFileWrite || fileBuffer.Length == 0) return;
        if (Time.unscaledTime < nextFlushTime) return;
        FlushFileBuffer();
        nextFlushTime = Time.unscaledTime + Mathf.Max(0.1f, FlushInterval);
    }

    public void OUTL_OnEvent(in OUTL_Event evt, OUTL_World world)
    {
        if (Entity == null || evt.Target != Entity.Id) return;
        if (evt.Type == OUTL_EventType.Damaged) Write(OUTL_DiaryEventType.TookDamage, "damage " + evt.FloatValue.ToString("0.0") + " key=" + evt.Key);
        else if (evt.Type == OUTL_EventType.Killed) Write(OUTL_DiaryEventType.Died, "killed by " + evt.Source, true);
        else if (evt.Type == OUTL_EventType.CommandExecuted) Write(OUTL_DiaryEventType.ReceivedOrder, "command " + (string.IsNullOrEmpty(evt.Key) ? evt.IntValue.ToString() : evt.Key));
    }

    public void Write(OUTL_DiaryEventType type, string context = "")
    {
        Write(type, context, false);
    }

    public void Write(OUTL_DiaryEventType type, string context, bool force)
    {
        if (!force && !CanWriteType(type)) return;
        EnsurePathIsCurrent();

        string phrase = Lines != null ? Lines.Pick(type) : string.Empty;
        if (string.IsNullOrEmpty(phrase)) phrase = type.ToString();
        string id = Entity != null && Entity.Id.IsValid ? Entity.Id.Value.ToString() : "?";
        OUTL_World world = OUTL_World.Instance;
        float worldTime = world != null ? world.WorldTime : Time.time;
        string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "][wt=" + worldTime.ToString("0.000") + "][" + id + "][" + type + "] " + phrase;
        if (!string.IsNullOrEmpty(context)) line += " | " + context;

        AddMemoryLine(line);

        if (WriteToFile)
        {
            if (BufferedFileWrite) fileBuffer.AppendLine(line);
            else AppendLineToFile(line);
        }

        if (MirrorToConsoleTrace && Entity != null) OUTL_DebugLog.TraceDiary(Entity.Id, line);
    }

    public string DumpMemory()
    {
        if (memory.Count == 0) return "diary empty";
        StringBuilder sb = new StringBuilder(memory.Count * 64);
        if (!ringWrapped)
        {
            for (int i = 0; i < memory.Count; i++) sb.AppendLine(memory[i]);
        }
        else
        {
            for (int i = ringWriteIndex; i < memory.Count; i++) sb.AppendLine(memory[i]);
            for (int i = 0; i < ringWriteIndex; i++) sb.AppendLine(memory[i]);
        }
        return sb.ToString();
    }

    public void ClearMemory()
    {
        memory.Clear();
        ringWriteIndex = 0;
        ringWrapped = false;
        for (int i = 0; i < nextAllowedEventTimes.Length; i++) nextAllowedEventTimes[i] = 0f;
    }

    public void FlushFileBuffer()
    {
        if (fileBuffer.Length == 0) return;
        EnsurePathIsCurrent();
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(filePath, fileBuffer.ToString());
            fileBuffer.Length = 0;
        }
        catch (Exception ex)
        {
            ReportFileError("flush", ex);
        }
    }

    private bool CanWriteType(OUTL_DiaryEventType type)
    {
        if (!EnableEventThrottle) return true;
        int index = (int)type;
        if (index < 0 || index >= nextAllowedEventTimes.Length) return true;
        float now = OUTL_World.Instance != null ? OUTL_World.Instance.WorldTime : Time.time;
        if (now < nextAllowedEventTimes[index]) return false;
        nextAllowedEventTimes[index] = now + GetThrottle(type);
        return true;
    }

    private float GetThrottle(OUTL_DiaryEventType type)
    {
        switch (type)
        {
            case OUTL_DiaryEventType.Attacked: return Mathf.Max(0f, AttackThrottle);
            case OUTL_DiaryEventType.TookDamage: return Mathf.Max(0f, DamageThrottle);
            case OUTL_DiaryEventType.Patrol: return Mathf.Max(0f, PatrolThrottle);
            case OUTL_DiaryEventType.Died:
            case OUTL_DiaryEventType.Spawn:
            case OUTL_DiaryEventType.DroppedLoot:
                return 0f;
            default:
                return Mathf.Max(0f, DefaultEventThrottle);
        }
    }

    private void AddMemoryLine(string line)
    {
        int max = Mathf.Max(1, MaxMemoryLines);
        if (memory.Count < max)
        {
            memory.Add(line);
            return;
        }

        if (ringWriteIndex >= memory.Count) ringWriteIndex = 0;
        memory[ringWriteIndex] = line;
        ringWriteIndex++;
        if (ringWriteIndex >= memory.Count)
        {
            ringWriteIndex = 0;
            ringWrapped = true;
        }
    }

    private void AppendLineToFile(string line)
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(filePath, line + "\n");
        }
        catch (Exception ex)
        {
            ReportFileError("append", ex);
        }
    }

    private void ReportFileError(string op, Exception ex)
    {
        if (LogFileErrorsOnce && reportedFileError) return;
        reportedFileError = true;
        OUTL_DebugLog.Log(OUTL_DebugChannel.Diary, "diary file " + op + " failed path=" + filePath + " error=" + ex.Message, true);
    }

    private void EnsurePathIsCurrent()
    {
        int id = Entity != null && Entity.Id.IsValid ? Entity.Id.Value : 0;
        if (string.IsNullOrEmpty(filePath) || id != lastBoundEntityId)
            BuildPath();
    }

    private void BuildPath()
    {
        lastBoundEntityId = Entity != null && Entity.Id.IsValid ? Entity.Id.Value : 0;
        string address = BuildStableAddress();
        filePath = Path.Combine(Application.persistentDataPath, FolderName, "Entity_" + SanitizePathToken(address) + ".log");
    }

    private string BuildStableAddress()
    {
        if (Entity != null)
        {
            if (Entity.Runtime != null)
            {
                if (!string.IsNullOrEmpty(Entity.Runtime.StableId)) return Entity.Runtime.StableId;
                if (!string.IsNullOrEmpty(Entity.Runtime.TargetName)) return Entity.Runtime.TargetName;
                if (!string.IsNullOrEmpty(Entity.Runtime.ClassName)) return Entity.Runtime.ClassName;
            }

            if (!string.IsNullOrEmpty(Entity.StableId)) return Entity.StableId;
            if (!string.IsNullOrEmpty(Entity.TargetName)) return Entity.TargetName;
            if (!string.IsNullOrEmpty(Entity.ClassNameOverride)) return Entity.ClassNameOverride;
        }

        return "unbound";
    }

    private static string SanitizePathToken(string value)
    {
        if (string.IsNullOrEmpty(value)) return "unbound";
        char[] invalid = Path.GetInvalidFileNameChars();
        StringBuilder sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool bad = c == '/' || c == '\\';
            for (int n = 0; !bad && n < invalid.Length; n++)
                bad = c == invalid[n];
            sb.Append(bad ? '_' : c);
        }
        return sb.Length > 0 ? sb.ToString() : "unbound";
    }
}
