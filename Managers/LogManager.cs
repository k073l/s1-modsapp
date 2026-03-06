using System.Reflection;
using MelonLoader;

namespace ModsApp.Managers;

public enum LogLevel
{
    Msg,
    Warning,
    Error
}

public class LogEntry
{
    public string ModName;
    public string Section;
    public string Message;
    public LogLevel Level;
    public DateTime Time;
}

public class LogManager
{
    private static LogManager _instance;
    public static LogManager Instance => _instance ??= new LogManager();

    private const int PerModCap = 200;

    private readonly List<LogEntry> _globalBuffer = new();

    // section string -> resolved mod Info.Name
    private readonly Dictionary<string, string> _sectionToModName
        = new(StringComparer.OrdinalIgnoreCase);

    // mod key -> how many entries currently in _globalBuffer
    private readonly Dictionary<string, int> _modEntryCount = new();

    // mod Info.Name -> had at least one error this session
    private readonly HashSet<string> _modsWithErrors = new();

    private LogManager()
    {
        foreach (var mod in MelonMod.RegisteredMelons)
        {
            var modName = mod.Info.Name;
            foreach (var variant in ModManager.GetModNameVariants(modName))
                _sectionToModName.TryAdd(variant, modName);
        }
    }

    public void WireEvents()
    {
        // ugly event reflection hack to capture MsgDrawing in ML 0.7.0 and 0.7.2 (one uses Color, the other ColorARGB)
        var evt = typeof(MelonLogger).GetEvent("MsgDrawingCallbackHandler");
        if (evt != null)
        {
            try
            {
                var handlerType = evt.EventHandlerType;
                var invoke = handlerType.GetMethod("Invoke");

                var parameters = invoke!.GetParameters()
                    .Select(p => System.Linq.Expressions.Expression.Parameter(p.ParameterType))
                    .ToArray();

                var call = System.Linq.Expressions.Expression.Call(
                    System.Linq.Expressions.Expression.Constant(this),
                    typeof(LogManager).GetMethod(nameof(OnMsgDrawingInternal),
                        BindingFlags.Instance | BindingFlags.NonPublic)!,
                    System.Linq.Expressions.Expression.Convert(parameters[2], typeof(string)),
                    System.Linq.Expressions.Expression.Convert(parameters[3], typeof(string))
                );

                var lambda = System.Linq.Expressions.Expression.Lambda(handlerType, call, parameters);
                var del = lambda.Compile();

                evt.AddEventHandler(null, del);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to wire MsgDrawingCallbackHandler: {e}");
            }
        }

        MelonLogger.WarningCallbackHandler += (section, msg) =>
        {
            if (!string.IsNullOrEmpty(section))
                Append(section, msg, LogLevel.Warning);
        };

        MelonLogger.ErrorCallbackHandler += (section, msg) =>
        {
            if (string.IsNullOrEmpty(section))
                return;

            Append(section, msg, LogLevel.Error);

            var modName = Resolve(section);
            if (modName != null)
                _modsWithErrors.Add(modName);
        };
    }

    private void OnMsgDrawingInternal(string section, string msg)
    {
        if (!string.IsNullOrEmpty(section))
            Append(section, msg, LogLevel.Msg);
    }

    private void Append(string section, string message, LogLevel level)
    {
        var modKey = Resolve(section) ?? section;

        _modEntryCount.TryGetValue(modKey, out var count);

        if (count >= PerModCap)
        {
            // remove this mod's oldest entry
            for (var i = 0; i < _globalBuffer.Count; i++)
            {
                if (EntryModKey(_globalBuffer[i]) == modKey)
                {
                    _globalBuffer.RemoveAt(i);
                    _modEntryCount[modKey]--;
                    break;
                }
            }
        }

        _globalBuffer.Add(new LogEntry
        {
            ModName = Resolve(section),
            Section = section,
            Message = message,
            Level = level,
            Time = DateTime.Now
        });

        _modEntryCount[modKey] = (_modEntryCount.TryGetValue(modKey, out var c) ? c : 0) + 1;
    }

    private string Resolve(string section) =>
        _sectionToModName.TryGetValue(section, out var name) ? name : null;

    private string EntryModKey(LogEntry e) =>
        e.ModName ?? e.Section;

    public IEnumerable<LogEntry> GetLogsForMod(string modName, params LogLevel[] levels) =>
        Filter(_globalBuffer.Where(e => e.ModName == modName), levels);

    public IEnumerable<LogEntry> GetAllLogs(params LogLevel[] levels) =>
        Filter(_globalBuffer, levels);

    public bool HasErrorsForMod(string modName) =>
        _modsWithErrors.Contains(modName);

    private static IEnumerable<LogEntry> Filter(
        IEnumerable<LogEntry> source, LogLevel[] levels) =>
        levels.Length == 0
            ? source
            : source.Where(e => levels.Contains(e.Level));
}