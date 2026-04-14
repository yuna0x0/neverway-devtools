using System.Collections.Immutable;
using DevTools.Core;
using ImGuiNET;
using Murder;
using Murder.Assets;
using Murder.Core;
using Murder.Data;
using Murder.Services;
using MurderGame = Murder.Game;

namespace DevTools.UI;

public static class ScenePanel
{
    private static string _worldSearch = "";
    private static (Guid guid, string name)[]? _worldCache;
    private static float _cacheTime;

    public static void Render()
    {
        var scene = MurderGame.Instance?.ActiveScene;
        var world = (scene as GameScene)?.World as MonoWorld;

        // Current scene info
        ImGui.Text("Current Scene:");
        ImGui.SameLine();
        if (scene != null)
            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 1f, 0.5f, 1f), scene.GetType().Name);
        else
            ImGui.TextDisabled("None");

        if (world != null)
        {
            var worldAsset = MurderGame.Data.TryGetAsset(world.WorldAssetGuid);
            var worldName = worldAsset?.Name ?? world.WorldAssetGuid.ToString()[..8];

            ImGui.Text("Current World:");
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 1f, 1f), worldName);

            ImGui.TextDisabled($"GUID: {world.WorldAssetGuid}");
            ImGui.TextDisabled($"Entities: {world.EntityCount}");
        }

        ImGui.Separator();
        ImGui.Spacing();

        // World list
        ImGui.Text("Available Worlds:");

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##worldsearch", "Search worlds...", ref _worldSearch, 128);

        ImGui.Spacing();

        var worlds = GetWorlds();
        if (worlds.Length == 0)
        {
            ImGui.TextDisabled("No world assets found.");
            return;
        }

        if (ImGui.BeginChild("WorldList", System.Numerics.Vector2.Zero, ImGuiChildFlags.Borders))
        {
            var currentGuid = world?.WorldAssetGuid ?? Guid.Empty;

            foreach (var (guid, name) in worlds)
            {
                if (_worldSearch.Length > 0 &&
                    !name.Contains(_worldSearch, StringComparison.OrdinalIgnoreCase) &&
                    !guid.ToString().Contains(_worldSearch, StringComparison.OrdinalIgnoreCase))
                    continue;

                var isCurrent = guid == currentGuid;
                if (isCurrent)
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.5f, 1f, 0.5f, 1f));

                var label = isCurrent ? $"{name} (current)" : name;
                if (ImGui.Selectable(label, false))
                {
                    LevelServices.SwitchScene(guid);
                    ConsoleEngine.AddInfo($"Switching to world: {name}");
                    // Invalidate cache since we're changing scenes
                    _worldCache = null;
                }

                if (isCurrent)
                    ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(guid.ToString());
            }
        }
        ImGui.EndChild();
    }

    private static (Guid guid, string name)[] GetWorlds()
    {
        // Refresh cache every 5 seconds
        var now = MurderGame.Now;
        if (_worldCache != null && now - _cacheTime < 5f)
            return _worldCache;

        try
        {
            var all = MurderGame.Data.FilterAllAssets(typeof(WorldAsset));
            var list = new List<(Guid, string)>();
            foreach (var kv in all)
            {
                list.Add((kv.Key, kv.Value.Name));
            }
            list.Sort((a, b) => string.Compare(a.Item2, b.Item2, StringComparison.OrdinalIgnoreCase));
            _worldCache = list.ToArray();
            _cacheTime = now;
        }
        catch
        {
            _worldCache = [];
        }

        return _worldCache;
    }
}
