using System.Diagnostics;
using System.Reflection;

#if MELONLOADER
using MelonLoader;
#elif BEPINEX
using BepInEx.Logging;
#endif

public static class RoutingFlagLogger
{
    public static void Log(string message, LogType logType = LogType.Info)
    {

#if DEBUG_MELONLOADER || DEBUG_BEPINEX

        StackFrame stackFrame;

        if (logType == LogType.Error)
        {
            stackFrame = new StackTrace().GetFrame(2);
        }
        else
        {
            stackFrame = new StackTrace().GetFrame(1);
        }

        MethodBase method = stackFrame.GetMethod();
        string className = method.DeclaringType?.Name ?? "UnknownClass";
        string methodName = method.Name;
        string logMessage = $"[{className}.{methodName}] {message}";

#if MELONLOADER

        switch (logType)
        {
            case LogType.Info:
                MelonLogger.Msg(logMessage);
                break;
            case LogType.Debug:
                MelonLogger.Msg($"[DEBUG] {logMessage}");
                break;
            case LogType.Warning:
                MelonLogger.Warning(logMessage);
                break;
            case LogType.Error:
                MelonLogger.Error(logMessage);
                break;
            default:
                MelonLogger.Msg(logMessage);
                break;
        }

#elif BEPINEX

        ManualLogSource logger = Logger.CreateLogSource("RoutingFlagExt");
        switch (logType)
        {
            case LogType.Info:
                logger.LogInfo(logMessage);
                break;
            case LogType.Debug:
                logger.LogDebug(logMessage);
                break;
            case LogType.Warning:
                logger.LogWarning(logMessage);
                break;
            case LogType.Error:
                logger.LogError(logMessage);
                break;
            default:
                logger.LogInfo(logMessage);
                break;
        }

#endif

#endif

    }

    public static void Info(string message)
    {
        Log(message, LogType.Info);
    }

    public static void Debug(string message)
    {
        Log(message, LogType.Debug);
    }

    public static void Warning(string message)
    {
        Log(message, LogType.Warning);
    }

    public static void Error(string message)
    {
        Log(message, LogType.Error);
    }

    public enum LogType
    {
        Info,
        Debug,
        Warning,
        Error
    }
}
