using UnityEngine;

[DefaultExecutionOrder(-7550)]
[DisallowMultipleComponent]
[RequireComponent(typeof(OUT_SimulationService))]
public class OUT_SimulationConsoleCommands : MonoBehaviour
{
    [SerializeField] private bool registerOnStart = true;

    private OUT_SimulationService simulation;
    private OUT_ConsoleService console;

    private void Awake()
    {
        simulation = GetComponent<OUT_SimulationService>();
    }

    private void Start()
    {
        if (registerOnStart)
            Register();
    }

    public void Register()
    {
        if (simulation == null)
            simulation = GetComponent<OUT_SimulationService>();

        console = OUT_ConsoleService.Instance;
        if (console == null)
            console = FindObjectOfType<OUT_ConsoleService>();

        if (console == null || console.CVars == null)
        {
            Debug.LogWarning("OUT_SimulationConsoleCommands: OUT_ConsoleService not found. Commands will register later only if Register() is called again.");
            return;
        }

        RegisterCVars();
        RegisterCommands();
    }

    private void RegisterCVars()
    {
        console.CVars.RegisterFloat("out_sim.tickrate", simulation.FullTickRate, OUT_CVarFlags.Archive, "Full simulation tick rate.", c => simulation.FullTickRate = c.FloatValue);
        console.CVars.RegisterFloat("out_sim.reduced_hz", simulation.ReducedTickRate, OUT_CVarFlags.Archive, "Reduced simulation tick rate.", c => simulation.ReducedTickRate = c.FloatValue);
        console.CVars.RegisterFloat("out_sim.abstract_hz", simulation.AbstractTickRate, OUT_CVarFlags.Archive, "Abstract simulation tick rate.", c => simulation.AbstractTickRate = c.FloatValue);
        console.CVars.RegisterFloat("out_sim.random_hz", simulation.RandomWorldTickRate, OUT_CVarFlags.Archive, "Random world tick rate.", c => simulation.RandomWorldTickRate = c.FloatValue);
        console.CVars.RegisterFloat("out_sim.timescale", simulation.SimulationTimeScale, OUT_CVarFlags.Archive, "OUT simulation time scale, separate from Unity Time.timeScale.", c => simulation.SimulationTimeScale = c.FloatValue);
        console.CVars.RegisterInt("out_sim.max_steps", simulation.MaxStepsPerFrame, OUT_CVarFlags.Archive, "Maximum simulation catch-up steps per frame.", c => simulation.MaxStepsPerFrame = c.IntValue);
        console.CVars.RegisterBool("out_sim.paused", simulation.Paused, OUT_CVarFlags.Archive, "Pauses OUT simulation ticks.", c => simulation.Paused = c.BoolValue);
        console.CVars.RegisterInt("out_sim.randomtickspeed", simulation.RandomTicksPerRandomWorldStep, OUT_CVarFlags.Archive, "Minecraft-like random world tick attempts per random-world step.", c => simulation.RandomTicksPerRandomWorldStep = c.IntValue);
    }

    private void RegisterCommands()
    {
        console.RegisterCommand("out_sim.stats", CmdStats, "Prints OUT simulation stats.", "out_sim.stats");
        console.RegisterCommand("out_sim.step", CmdStep, "Runs one full simulation step while paused.", "out_sim.step");
        console.RegisterCommand("out_sim.pause", CmdPause, "Pauses OUT simulation.", "out_sim.pause");
        console.RegisterCommand("out_sim.resume", CmdResume, "Resumes OUT simulation.", "out_sim.resume");
        console.RegisterCommand("out_sim.discover", CmdDiscover, "Discovers IOutSimSystem and random tick receivers in scene.", "out_sim.discover");
        console.RegisterCommand("out_sim.resetstats", CmdResetStats, "Resets simulation statistics.", "out_sim.resetstats");
        console.RegisterCommand("out_sim.resetclock", CmdResetClock, "Resets simulation clocks and accumulators.", "out_sim.resetclock", true);
        console.RegisterCommand("out_sim.rates", CmdRates, "Prints simulation rates and time scale.", "out_sim.rates");
    }

    private void CmdStats(OUT_ConsoleCommandContext ctx)
    {
        ctx.Service.Log.Add(simulation.BuildDebugReport());
    }

    private void CmdStep(OUT_ConsoleCommandContext ctx)
    {
        simulation.Paused = true;
        if (console != null && console.CVars != null)
            console.CVars.TrySet("out_sim.paused", "1", true, out _);
        simulation.RequestSingleStep();
        ctx.Service.Log.Add("OUT simulation single step requested.");
    }

    private void CmdPause(OUT_ConsoleCommandContext ctx)
    {
        simulation.Paused = true;
        if (console != null && console.CVars != null)
            console.CVars.TrySet("out_sim.paused", "1", true, out _);
        ctx.Service.Log.Add("OUT simulation paused.");
    }

    private void CmdResume(OUT_ConsoleCommandContext ctx)
    {
        simulation.Paused = false;
        if (console != null && console.CVars != null)
            console.CVars.TrySet("out_sim.paused", "0", true, out _);
        ctx.Service.Log.Add("OUT simulation resumed.");
    }

    private void CmdDiscover(OUT_ConsoleCommandContext ctx)
    {
        simulation.DiscoverSystems();
        ctx.Service.Log.Add("OUT simulation discovered systems. " + simulation.BuildDebugReport());
    }

    private void CmdResetStats(OUT_ConsoleCommandContext ctx)
    {
        simulation.ResetStats();
        ctx.Service.Log.Add("OUT simulation stats reset.");
    }

    private void CmdResetClock(OUT_ConsoleCommandContext ctx)
    {
        simulation.ResetClock();
        ctx.Service.Log.Add("OUT simulation clock reset.");
    }

    private void CmdRates(OUT_ConsoleCommandContext ctx)
    {
        ctx.Service.Log.Add(
            "full:" + simulation.FullTickRate.ToString("0.###") +
            " reduced:" + simulation.ReducedTickRate.ToString("0.###") +
            " abstract:" + simulation.AbstractTickRate.ToString("0.###") +
            " random:" + simulation.RandomWorldTickRate.ToString("0.###") +
            " timescale:" + simulation.SimulationTimeScale.ToString("0.###") +
            " maxSteps:" + simulation.MaxStepsPerFrame +
            " randomtickspeed:" + simulation.RandomTicksPerRandomWorldStep);
    }
}
