using System.Collections.Immutable;
using System.Numerics;
using Bang;
using Murder.Diagnostics;

namespace DevTools.Core;

public static class ConsoleEngine
{
    public struct ConsoleEntry
    {
        public string Text;
        public Vector4 Color;
    }

    private static readonly Vector4 ColorCommand = new(0.5f, 1f, 0.3f, 1f);
    private static readonly Vector4 ColorOutput = new(1f, 1f, 1f, 1f);
    private static readonly Vector4 ColorError = new(1f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 ColorInfo = new(0.6f, 0.8f, 1f, 1f);
    private static readonly Vector4 ColorEngine = new(0.7f, 0.7f, 0.7f, 1f);

    private static readonly List<ConsoleEntry> _log = new();
    private static readonly List<string> _history = new();
    private static int _historyIndex = -1;
    private static int _lastEngineLogCount = 0;

    private static ImmutableDictionary<string, CommandServices.Command>? _commandsCache;

    public static IReadOnlyList<ConsoleEntry> Log => _log;
    public static IReadOnlyList<string> History => _history;
    public static int HistoryIndex { get => _historyIndex; set => _historyIndex = value; }

    public static ImmutableDictionary<string, CommandServices.Command> Commands
    {
        get
        {
            _commandsCache ??= CommandServices.AllCommands;
            return _commandsCache;
        }
    }

    /// <summary>
    /// Poll GameLogger for new messages and forward them to our console.
    /// Call this each frame.
    /// </summary>
    public static void PollEngineLogs()
    {
        try
        {
            var logs = GameLogger.FetchLogs();
            if (logs.Length > _lastEngineLogCount)
            {
                for (int i = _lastEngineLogCount; i < logs.Length; i++)
                    AddEntry($"[engine] {logs[i]}", ColorEngine);
                _lastEngineLogCount = logs.Length;
            }
        }
        catch { /* GameLogger may not be initialized */ }
    }

    public static void Execute(World? world, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        // Add to history
        if (_history.Count == 0 || _history[^1] != input)
            _history.Add(input);
        _historyIndex = -1;

        // Log the command
        AddEntry($"> {input}", ColorCommand);

        // Built-in commands
        var trimmed = input.Trim();
        if (trimmed.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp(null);
            return;
        }
        if (trimmed.StartsWith("help ", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp(trimmed[5..].Trim());
            return;
        }
        if (trimmed.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            Clear();
            return;
        }

        if (world == null)
        {
            AddEntry("No active world.", ColorError);
            return;
        }

        try
        {
            var result = CommandServices.Parse(world, input);
            if (!string.IsNullOrEmpty(result))
                AddEntry(result, ColorOutput);
        }
        catch (Exception ex)
        {
            // Unwrap TargetInvocationException from reflection calls
            var inner = ex.InnerException ?? ex;
            AddEntry($"Command error: {inner.Message}", ColorError);
        }
    }

    private static void PrintHelp(string? commandName)
    {
        if (commandName != null)
        {
            if (Commands.TryGetValue(commandName, out var cmd))
            {
                var args = string.Join(" ", cmd.Arguments.Select(a => $"<{a.Name}:{a.Type.Name}>"));
                AddEntry($"{cmd.Name} {args}", ColorInfo);
                AddEntry($"  {cmd.Help}", ColorOutput);
            }
            else
            {
                AddEntry($"Unknown command: {commandName}. Type 'help' to list all.", ColorError);
            }
            return;
        }

        AddEntry("Available commands:", ColorInfo);
        AddEntry("  help              - Show this list", ColorOutput);
        AddEntry("  help <command>    - Show command details", ColorOutput);
        AddEntry("  clear             - Clear console", ColorOutput);
        AddEntry("", ColorOutput);
        foreach (var kv in Commands.OrderBy(k => k.Key))
        {
            var args = kv.Value.Arguments.Length > 0
                ? " " + string.Join(" ", kv.Value.Arguments.Select(a => $"<{a.Name}>"))
                : "";
            AddEntry($"  {kv.Key}{args} - {kv.Value.Help}", ColorOutput);
        }
    }

    public static List<string> GetSuggestions(string partial)
    {
        if (string.IsNullOrEmpty(partial)) return new List<string>();

        return Commands.Keys
            .Where(k => k.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .Take(10)
            .ToList();
    }

    public static string? GetCommandHelp(string name)
    {
        if (Commands.TryGetValue(name, out var cmd))
            return cmd.Help;
        return null;
    }

    public static void AddInfo(string text) => AddEntry(text, ColorInfo);

    public static void Clear()
    {
        _log.Clear();
        _lastEngineLogCount = 0;
    }

    private static void AddEntry(string text, Vector4 color)
    {
        _log.Add(new ConsoleEntry { Text = text, Color = color });
        if (_log.Count > 500)
            _log.RemoveAt(0);
    }
}
