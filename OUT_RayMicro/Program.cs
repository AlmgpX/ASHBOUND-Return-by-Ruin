using OUT_RayMicro.Runtime;

try
{
    OutmCrashLog.Write("Program start");
    OutmApp.Run();
    OutmCrashLog.Write("Program clean exit");
}
catch (Exception ex)
{
    OutmCrashLog.Write("MANAGED CRASH\n" + ex);
    Console.Error.WriteLine(ex);
    throw;
}
