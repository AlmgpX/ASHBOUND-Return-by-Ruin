namespace OUT_RayMicro.Physics;

public interface IOutmPhysicsBackendBridge
{
    OutmPhysicsBackendRole Role { get; }
    bool IsNativeReady { get; }
    void BuildFromRuntime(OutmPhysicsRuntime runtime);
    void Step(float dt);
    void Unload();
}

public sealed class OutmManagedPhysicsBridge : IOutmPhysicsBackendBridge
{
    public OutmPhysicsBackendRole Role => OutmPhysicsBackendRole.ManagedFallback;
    public bool IsNativeReady => false;

    public void BuildFromRuntime(OutmPhysicsRuntime runtime)
    {
        // Managed fallback uses OutmPhysicsRuntime buffers directly.
    }

    public void Step(float dt)
    {
        // No native world to step yet.
    }

    public void Unload()
    {
    }
}

public sealed class OutmNativeJoltBridge : IOutmPhysicsBackendBridge
{
    public OutmPhysicsBackendRole Role => OutmPhysicsBackendRole.NativeJolt;
    public bool IsNativeReady { get; private set; }

    public void BuildFromRuntime(OutmPhysicsRuntime runtime)
    {
        // Native Jolt body creation belongs here:
        // Body + Shape + Proxy data in, native static/kinematic/sensor bodies out.
        // Gameplay must never call Jolt directly.
        IsNativeReady = false;
    }

    public void Step(float dt)
    {
        if (!IsNativeReady)
            return;
    }

    public void Unload()
    {
        IsNativeReady = false;
    }
}

public static class OutmPhysicsBackendFactory
{
    public static IOutmPhysicsBackendBridge CreateDefault()
    {
        return new OutmManagedPhysicsBridge();
    }
}
