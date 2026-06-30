public sealed class OUT_TaskRunner
{
    private OUT_ScheduleDefinition _currentSchedule;
    private int _currentTaskIndex = -1;
    private bool _taskStarted;

    public OUT_ScheduleDefinition CurrentSchedule => _currentSchedule;
    public int CurrentTaskIndex => _currentTaskIndex;
    public bool IsRunning => _currentSchedule != null && _currentTaskIndex >= 0;

    public void Clear(IOutTaskExecutor executor = null)
    {
        if (executor != null && IsRunning)
        {
            OUT_TaskDefinition current = GetCurrentTaskDefinition();
            if (current != null)
                executor.StopTask(current.ToHandle());
        }

        _currentSchedule = null;
        _currentTaskIndex = -1;
        _taskStarted = false;
    }

    public void SetSchedule(OUT_ScheduleDefinition schedule, IOutTaskExecutor executor)
    {
        Clear(executor);

        if (schedule == null || schedule.Tasks.Length == 0)
            return;

        _currentSchedule = schedule;
        _currentTaskIndex = 0;
        _taskStarted = false;

        StartCurrentTask(executor);
    }

    public void Tick(IOutTaskExecutor executor, float deltaTime, OUT_ConditionState currentConditions)
    {
        if (executor == null || !IsRunning || _currentSchedule == null)
            return;

        if (_currentSchedule.CanInterrupt(currentConditions))
        {
            Clear(executor);
            return;
        }

        OUT_TaskDefinition current = GetCurrentTaskDefinition();
        if (current == null)
        {
            Clear(executor);
            return;
        }

        if (!_taskStarted)
            StartCurrentTask(executor);

        OUT_TaskStatus status = executor.RunTask(current.ToHandle(), deltaTime);

        switch (status)
        {
            case OUT_TaskStatus.Succeeded:
                AdvanceToNextTask(executor);
                break;

            case OUT_TaskStatus.Failed:
                Clear(executor);
                break;

            case OUT_TaskStatus.Running:
            default:
                break;
        }
    }

    private void StartCurrentTask(IOutTaskExecutor executor)
    {
        OUT_TaskDefinition current = GetCurrentTaskDefinition();
        if (current == null)
            return;

        executor.StartTask(current.ToHandle());
        _taskStarted = true;
    }

    private void AdvanceToNextTask(IOutTaskExecutor executor)
    {
        OUT_TaskDefinition current = GetCurrentTaskDefinition();
        if (current != null)
            executor.StopTask(current.ToHandle());

        _currentTaskIndex++;

        if (_currentSchedule == null || _currentTaskIndex >= _currentSchedule.Tasks.Length)
        {
            Clear();
            return;
        }

        _taskStarted = false;
        StartCurrentTask(executor);
    }

    private OUT_TaskDefinition GetCurrentTaskDefinition()
    {
        if (_currentSchedule == null)
            return null;

        if (_currentTaskIndex < 0 || _currentTaskIndex >= _currentSchedule.Tasks.Length)
            return null;

        return _currentSchedule.Tasks[_currentTaskIndex];
    }
}