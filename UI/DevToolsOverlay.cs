using DevTools.Core;
using ImGuiNET;
using Murder;
using Murder.Core;
using NeverwayMod.DevTools.Core;
using MurderGame = Murder.Game;

namespace DevTools.UI;

public static class DevToolsOverlay
{
    public static void Render()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(650, 500), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("DevTools (F2)", ref DevToolsMod.ShowOverlay))
        {
            ImGui.End();
            return;
        }

        // Reserve space for the status bar at bottom
        var statusBarHeight = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;

        // Tab bar + content fills everything except the status bar
        if (ImGui.BeginChild("TabContent", new System.Numerics.Vector2(0, -statusBarHeight)))
        {
            if (ImGui.BeginTabBar("DevToolsTabs"))
            {
                if (ImGui.BeginTabItem("Console"))
                {
                    ConsolePanel.Render();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Cheats"))
                {
                    CheatsPanel.Render();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Noclip"))
                {
                    NoclipPanel.Render();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Components"))
                {
                    ComponentsPanel.Render();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Scene"))
                {
                    ScenePanel.Render();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
        ImGui.EndChild();

        // Status bar
        ImGui.Separator();
        var scene = MurderGame.Instance?.ActiveScene;
        var world = (scene as GameScene)?.World as MonoWorld;
        var entityCount = world?.EntityCount ?? 0;
        var fps = MurderGame.DeltaTime > 0 ? (1.0 / MurderGame.DeltaTime) : 0;

        var worldName = "";
        if (world != null)
        {
            var asset = MurderGame.Data.TryGetAsset(world.WorldAssetGuid);
            worldName = asset?.Name ?? "?";
        }

        if (NoclipController.IsActive)
            ImGui.TextColored(UIColors.Active, "Noclip: ON");
        else
            ImGui.TextDisabled("Noclip: OFF");
        ImGui.SameLine();
        ImGui.Text($"| FPS: {fps:F0} | Entities: {entityCount} | {worldName}");

        ImGui.End();
    }
}
