using System.Numerics;
using Bang;
using Bang.Entities;
using DevTools.Core;
using ImGuiNET;
using Murder.Core;
using Murder.Utilities;
using MurderGame = Murder.Game;

namespace DevTools.UI;

public static class NoclipPanel
{
    private static float _teleportX;
    private static float _teleportY;

    public static void Render()
    {
        var world = (MurderGame.Instance?.ActiveScene as GameScene)?.World as MonoWorld;

        // Status
        if (NoclipController.IsActive)
        {
            ImGui.TextColored(new Vector4(0.2f, 1f, 0.2f, 1f), "ACTIVE");
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "INACTIVE");
        }
        ImGui.SameLine();
        ImGui.TextDisabled("Toggle: F3");

        ImGui.Separator();

        if (world != null)
        {
            if (ImGui.Button(NoclipController.IsActive ? "Disable Noclip" : "Enable Noclip"))
            {
                NoclipController.Toggle(world);
            }
        }

        ImGui.Spacing();

        // Speed
        ImGui.SetNextItemWidth(200);
        ImGui.SliderFloat("Speed", ref NoclipController.MoveSpeed, 50f, 2000f);
        ImGui.TextDisabled("Hold Shift for 3x speed");

        // Free camera
        ImGui.Checkbox("Free Camera Mode", ref NoclipController.FreeCameraMode);
        ImGui.TextDisabled("Camera moves independently of player");

        ImGui.Separator();

        // Current position
        if (world != null)
        {
            var player = world.TryGetUniqueEntityPlayer();
            if (player != null)
            {
                var pos = player.GetGlobalPosition();
                ImGui.Text($"Player Position: ({pos.X:F1}, {pos.Y:F1})");
            }

            ImGui.Text($"Camera Position: ({world.Camera.Position.X:F1}, {world.Camera.Position.Y:F1})");
            ImGui.Text($"Camera Zoom: {world.Camera.Zoom:F2}");
        }

        ImGui.Separator();

        // Teleport
        ImGui.Text("Teleport To:");
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("X##tp", ref _teleportX);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("Y##tp", ref _teleportY);
        ImGui.SameLine();
        if (ImGui.Button("Go") && world != null)
        {
            NoclipController.TeleportTo(world, new Vector2(_teleportX, _teleportY));
        }

        if (NoclipController.IsActive && world != null)
        {
            var saved = NoclipController.GetSavedPosition();
            ImGui.Text($"Saved Position: ({saved.X:F1}, {saved.Y:F1})");
            if (ImGui.Button("Reset to Saved Position"))
            {
                NoclipController.ResetPosition(world);
            }
        }

        ImGui.Separator();
        ImGui.TextDisabled("Controls: WASD / Arrow Keys to move");
        ImGui.TextDisabled("Shift = 3x speed boost");
    }
}
