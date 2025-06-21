using BepInEx;
using BepInEx.Configuration;

namespace Topic_of_Love.Mian;

public class TolConfig
{
    public static ConfigEntry<bool> Debug;
    public static ConfigEntry<string> SlowOnLog;
    public static ConfigEntry<string> StackTrace;
    public static ConfigEntry<string> Ignore;


    public static void Init(BaseUnityPlugin plugin)
    {
        Debug = plugin.Config.Bind("Debugging", "Debug", true, "Enable debug logging");
        StackTrace = plugin.Config.Bind("Debugging", "StackTrace", "", "Print the stacktrace when a specific part of a log shows up | Use commas to add multiple parts");
        Ignore = plugin.Config.Bind("Debugging", "Ignore", "", "Prevent a log from logging if any specified parts are within its contents | Use commas to add multiple parts");
        SlowOnLog = plugin.Config.Bind("Debugging", "SlowOnLog", "", "Slow the game when a specific part of a log shows up | Use commas to add multiple parts");
    }
}