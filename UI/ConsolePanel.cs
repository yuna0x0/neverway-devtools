using Bang;
using DevTools.Core;
using ImGuiNET;
using Murder.Core;
using NeverwayMod.DevTools.Core;

namespace DevTools.UI;

public static unsafe class ConsolePanel
{
    private static string _input = "";
    private static bool _scrollToBottom = true;
    private static bool _focusInput = true;
    private static List<string> _currentSuggestions = new();
    private static bool _showSuggestions = false;
    private static int _tabIndex = -1;
    private static string _tabPartial = "";  // the original text when Tab cycling started

    public static void Render()
    {
        var world = GameHelper.GetWorld();

        var style = ImGui.GetStyle();
        var bottomReserve = style.ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing() + ImGui.GetTextLineHeightWithSpacing();

        ImGui.BeginChild("ConsoleOutput", new System.Numerics.Vector2(0, -bottomReserve), ImGuiChildFlags.None);

        // Check if user is at the bottom BEFORE rendering (uses last frame's scroll state)
        var wasAtBottom = ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 1f;

        foreach (var entry in ConsoleEngine.Log)
        {
            ImGui.PushTextWrapPos();
            ImGui.TextColored(entry.Color, entry.Text);
            ImGui.PopTextWrapPos();
        }

        // Auto-scroll if user was at bottom or we explicitly requested it (e.g. after sending a command)
        if (_scrollToBottom || wasAtBottom)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottom = false;
        }
        ImGui.EndChild();

        ImGui.Separator();

        var inputFlags = ImGuiInputTextFlags.EnterReturnsTrue
            | ImGuiInputTextFlags.CallbackHistory
            | ImGuiInputTextFlags.CallbackCompletion;

        if (_focusInput)
        {
            ImGui.SetKeyboardFocusHere();
            _focusInput = false;
        }

        var avail = ImGui.GetContentRegionAvail().X;
        var buttonWidth = avail * 0.12f;
        var inputWidth = avail - buttonWidth * 2 - style.ItemSpacing.X * 2;

        ImGui.PushItemWidth(inputWidth);
        bool submitted = ImGui.InputText("##ConInput", ref _input, 512, inputFlags, InputCallback);
        ImGui.PopItemWidth();

        ImGui.SameLine();
        submitted |= ImGui.Button("Send", new System.Numerics.Vector2(buttonWidth, 0));
        ImGui.SameLine();
        if (ImGui.Button("Clear", new System.Numerics.Vector2(buttonWidth, 0)))
            ConsoleEngine.Clear();

        if (submitted && !string.IsNullOrWhiteSpace(_input))
        {
            ConsoleEngine.Execute(world, _input.Trim());
            _input = "";
            _scrollToBottom = true;
            _focusInput = true;
            ResetTab();
        }

        // Update suggestions from the original partial (not the Tab-inserted text)
        var lookupText = _tabIndex >= 0 ? _tabPartial : _input;
        if (lookupText.Length > 0 && !lookupText.Contains(' '))
        {
            _currentSuggestions = ConsoleEngine.GetSuggestions(lookupText);
            _showSuggestions = _currentSuggestions.Count > 0;
        }
        else
        {
            _showSuggestions = false;
            ResetTab();
        }

        // Clamp tab index if suggestions changed
        if (_tabIndex >= _currentSuggestions.Count)
            _tabIndex = -1;

        if (_showSuggestions)
        {
            var parts = new List<string>();
            for (int i = 0; i < _currentSuggestions.Count; i++)
                parts.Add(_tabIndex == i ? $"[{_currentSuggestions[i]}]" : _currentSuggestions[i]);
            ImGui.TextDisabled($"Tab: {string.Join(", ", parts)}");
        }
        else
        {
            ImGui.TextDisabled("Type 'help' to list commands. Tab to cycle completions.");
        }
    }

    private static void ResetTab()
    {
        _tabIndex = -1;
        _tabPartial = "";
        _showSuggestions = false;
    }

    private static int InputCallback(ImGuiInputTextCallbackData* data)
    {
        var dataPtr = new ImGuiInputTextCallbackDataPtr(data);

        switch (dataPtr.EventFlag)
        {
            case ImGuiInputTextFlags.CallbackHistory:
            {
                var history = ConsoleEngine.History;
                if (history.Count == 0) break;

                if (dataPtr.EventKey == ImGuiKey.UpArrow)
                {
                    if (ConsoleEngine.HistoryIndex < 0)
                        ConsoleEngine.HistoryIndex = history.Count - 1;
                    else if (ConsoleEngine.HistoryIndex > 0)
                        ConsoleEngine.HistoryIndex--;
                }
                else if (dataPtr.EventKey == ImGuiKey.DownArrow)
                {
                    if (ConsoleEngine.HistoryIndex >= 0)
                        ConsoleEngine.HistoryIndex++;
                    if (ConsoleEngine.HistoryIndex >= history.Count)
                        ConsoleEngine.HistoryIndex = -1;
                }

                var historyEntry = ConsoleEngine.HistoryIndex >= 0
                    ? history[ConsoleEngine.HistoryIndex]
                    : "";

                dataPtr.DeleteChars(0, dataPtr.BufTextLen);
                dataPtr.InsertChars(0, historyEntry);
                ResetTab();
                break;
            }

            case ImGuiInputTextFlags.CallbackCompletion:
            {
                if (_currentSuggestions.Count == 0) break;

                // On first Tab, save the partial the user typed
                if (_tabIndex < 0)
                {
                    var buf = (byte*)data->Buf;
                    _tabPartial = System.Text.Encoding.UTF8.GetString(buf, data->BufTextLen);
                }

                _tabIndex = (_tabIndex + 1) % _currentSuggestions.Count;

                dataPtr.DeleteChars(0, dataPtr.BufTextLen);
                dataPtr.InsertChars(0, _currentSuggestions[_tabIndex]);
                break;
            }
        }

        return 0;
    }

}
