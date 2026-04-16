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
    private const int PerModErrorCap = 10;

    private readonly List<LogEntry> _globalBuffer = new();
    private readonly Dictionary<string, Queue<LogEntry>> _recentErrors = new();

    // section string -> resolved mod Info.Name
    private readonly Dictionary<string, string> _sectionToModName
        = new(StringComparer.OrdinalIgnoreCase);

    // mod key -> how many entries currently in _globalBuffer
    private readonly Dictionary<string, int> _modEntryCount = new();

    // mod Info.Name -> had at least one error this session
    internal readonly HashSet<string> ModsWithErrors = new();

    internal event Action OnError;

    private LogManager()
    {
        foreach (var mod in MelonMod.RegisteredMelons)
        {
            var modName = mod.Info.Name;

            foreach (var name in new[]
                     {
                         modName,
                         mod.MelonAssembly?.Assembly?.GetName()?.Name
                     })
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                foreach (var variant in ModManager.GetModNameVariants(name))
                    _sectionToModName.TryAdd(variant, modName);
            }
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
            if (modName == null) return;
            ModsWithErrors.Add(modName);
            OnError?.Invoke();
        };
    }

    private void OnMsgDrawingInternal(string section, string msg)
    {
        if (!string.IsNullOrEmpty(section))
            Append(section, msg, LogLevel.Msg);
    }

    private void Append(string section, string message, LogLevel level)
    {
        var resolved = Resolve(section);
        var modKey = resolved ?? section;

        var entry = new LogEntry
        {
            ModName = resolved,
            Section = section,
            Message = message,
            Level = level,
            Time = DateTime.Now
        };

        _globalBuffer.Add(entry);

        _modEntryCount[modKey] = _modEntryCount.GetValueOrDefault(modKey) + 1;

        // Cleanup
        if (level == LogLevel.Error)
        {
            if (!_recentErrors.TryGetValue(modKey, out var q))
            {
                q = new Queue<LogEntry>();
                _recentErrors[modKey] = q;
            }

            q.Enqueue(entry);

            if (q.Count > PerModErrorCap)
                q.Dequeue();
        }

        if (_modEntryCount[modKey] > PerModCap)
        {
            // remove this mod's oldest entry
            for (var i = 0; i < _globalBuffer.Count; i++)
            {
                if (EntryModKey(_globalBuffer[i]) != modKey)
                    continue;

                _globalBuffer.RemoveAt(i);
                _modEntryCount[modKey]--;
                break;
            }
        }
    }

    private string Resolve(string section)
    {
        if (string.IsNullOrEmpty(section))
            return null;

        // check exact first
        if (_sectionToModName.TryGetValue(section, out var name))
            return name;

        // then check case-insensitive contains (for sections like "ModName:Subsection")
        foreach (var kvp in _sectionToModName)
        {
            if (section.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }

    private string EntryModKey(LogEntry e) =>
        e.ModName ?? e.Section;

    public IEnumerable<LogEntry> GetLogsForMod(string modName, params LogLevel[] levels)
    {
        var baseLogs = _globalBuffer.Where(e => e.ModName == modName);

        var stickyErrors =
            _recentErrors.TryGetValue(modName, out var q)
                ? q
                : Enumerable.Empty<LogEntry>();

        return Dedup(Filter(baseLogs.Concat(stickyErrors).OrderBy(e => e.Time), levels));
    }

    public IEnumerable<LogEntry> GetAllLogs(params LogLevel[] levels)
    {
        var sticky = _recentErrors.Values.SelectMany(q => q);

        return Dedup(Filter(_globalBuffer.Concat(sticky).OrderBy(e => e.Time), levels));
    }

    public bool HasErrorsForMod(string modName) =>
        ModsWithErrors.Contains(modName);

    private static IEnumerable<LogEntry> Filter(
        IEnumerable<LogEntry> source, LogLevel[] levels) =>
        levels.Length == 0
            ? source
            : source.Where(e => levels.Contains(e.Level));
    
    private static IEnumerable<LogEntry> Dedup(IEnumerable<LogEntry> logs)
    {
        return logs.GroupBy(e => (e.Time, e.Message, e.ModName, e.Level))
            .Select(g => g.First());
    }
}