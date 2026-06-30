public enum OUT_TaskStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2
}

public readonly struct OUT_ScheduleHandle
{
    public readonly string Id;

    public OUT_ScheduleHandle(string id)
    {
        Id = id;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Id);
}

public readonly struct OUT_TaskHandle
{
    public readonly string Id;

    public OUT_TaskHandle(string id)
    {
        Id = id;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Id);
}
