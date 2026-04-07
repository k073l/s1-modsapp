using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using MelonLoader;

namespace ModsApp.Helpers;

public static class MelonExtensions
{
    /// <summary>
    /// Logs a debug message to the console.
    /// This method only works in Debug builds. In Release builds, it does nothing.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="stacktrace">Whether to include the stack trace in the log message. Defaults to true.</param>
    public static void Debug(
        this MelonLogger.Instance logger,
        string message,
        bool stacktrace = true
    )
    {
#if RELEASE
        // Suppress debug logs in Release
#else
        var melon = MelonUtils.GetMelonFromStackTrace();
        var namesection_color = MelonLogger.DefaultMelonColor;
        if (melon is { Info: not null })
            namesection_color = melon.ConsoleColor;

        var name = GetLoggerName(logger);
        var finalMessage = stacktrace
            ? $"[DEBUG] {GetCallerInfo()} - {message}"
            : $"[DEBUG] {message}";

        InvokeNativeMsg(namesection_color, MelonLogger.DefaultTextColor, name, finalMessage);
#endif
    }

    private static string GetLoggerName(MelonLogger.Instance logger)
    {
        var field = typeof(MelonLogger.Instance).GetField(
            "Name",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        return field?.GetValue(logger) as string;
    }

    private static void InvokeNativeMsg(
        Color namesectionColor,
        Color textColor,
        string nameSection,
        string message
    )
    {
        var method = typeof(MelonLogger).GetMethod(
            "NativeMsg",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        method?.Invoke(
            null,
            new object[]
            {
                namesectionColor,
                textColor,
                nameSection,
                message ?? "null",
                false, // skipStackWalk
            }
        );
    }

    private static string GetCallerInfo()
    {
        var stackTrace = new StackTrace();
        for (int i = 2; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            if (method?.DeclaringType == null)
                continue;

            return $"{method.DeclaringType.FullName}.{method.Name}";
        }

        return "unknown";
    }
}

internal static class MelonPreferencesExtensions
{
    public static object GetDefaultValue(this MelonPreferences_Entry entry)
    {
        var current = entry.BoxedValue;
        entry.ResetToDefault();
        var defaultValue = entry.BoxedValue;
        entry.BoxedValue = current;
        return defaultValue;
    }
    
    public static object ConvertToMatching(this MelonPreferences_Entry entry, object value)
    {
        var type = entry.GetReflectedType();
        // if it's not of the type, try to convert
        if (value.GetType() == type)
            return value;
        if (type.IsEnum)
            return Enum.Parse(type, value.ToString());
        try
        {
            return Convert.ChangeType(value, type);
        }
        catch (Exception e)
        {
            Melon<ModsApp>.Logger.Error($"Failed to convert value '{value}' to type {type}: {e}");
            throw;
        }
    }
}