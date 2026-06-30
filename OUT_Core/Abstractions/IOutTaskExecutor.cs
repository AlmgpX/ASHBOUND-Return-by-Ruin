public interface IOutTaskExecutor
{
    void StartTask(in OUT_TaskHandle task);
    OUT_TaskStatus RunTask(in OUT_TaskHandle task, float deltaTime);
    void StopTask(in OUT_TaskHandle task);
}
