using Bang;
using DevTools.Core;
using ImGuiNET;
using Murder.Core;
using MurderGame = Murder.Game;

namespace DevTools.UI;

public static class CheatsPanel
{
    private static int _healAmount = 15;
    private static int _hurtAmount = 5;
    private static int _moneyAmount = 1000;
    private static int _gemsAmount = 50;
    private static int _mortgageAmount = 100;
    private static float _timeValue = 0.5f;
    private static int _dayValue = 1;
    private static float _staminaValue = 6f;
    private static float _rainAmount = 0.6f;
    private static float _glitchAmount = 0f;
    private static float _noiseAmount = 0f;
    private static int _growDays = 5;
    private static int _levelValue = 5;
    private static string _npcName = "";
    private static string _bbName = "";
    private static string _bbValue = "";
    private static int _questProgress = 1;
    private static bool _godmodeActive = false;
    private static bool _timePaused = false;

    public static void Render()
    {
        var world = GetWorld();
        if (world == null)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0.5f, 1), "No active world.");
            return;
        }

        if (ImGui.CollapsingHeader("Health & Combat", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.Button(_godmodeActive ? "Godmode: ON" : "Godmode: OFF"))
            {
                RunCommand(world, "godmode");
                _godmodeActive = !_godmodeActive;
            }
            ImGui.SameLine();
            if (ImGui.Button("Kill All"))
                RunCommand(world, "kill");
            ImGui.SameLine();
            if (ImGui.Button("Suicide"))
                RunCommand(world, "suicide");

            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("Heal##val", ref _healAmount, 1, 1, 999);
            ImGui.SameLine();
            if (ImGui.Button("Heal"))
                RunCommand(world, $"heal {_healAmount}");

            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("Hurt##val", ref _hurtAmount, 1, 1, 999);
            ImGui.SameLine();
            if (ImGui.Button("Hurt"))
                RunCommand(world, $"hurt {_hurtAmount}");
        }

        if (ImGui.CollapsingHeader("Economy", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("##money", ref _moneyAmount, 10, 0, 99999);
            ImGui.SameLine();
            if (ImGui.Button("Add Money"))
                RunCommand(world, $"money {_moneyAmount}");

            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("##gems", ref _gemsAmount, 1, 0, 9999);
            ImGui.SameLine();
            if (ImGui.Button("Add Gems"))
                RunCommand(world, $"gems {_gemsAmount}");

            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("##mortgage", ref _mortgageAmount, 1, 0, 9999);
            ImGui.SameLine();
            if (ImGui.Button("Add Mortgage"))
                RunCommand(world, $"mortgage {_mortgageAmount}");
        }

        if (ImGui.CollapsingHeader("Time"))
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.SliderFloat("##time", ref _timeValue, 0f, 1f, "Time: %.2f"))
                RunCommand(world, $"time {_timeValue:F2}");

            if (ImGui.Button("Skip Time Slot"))
                RunCommand(world, "skip");
            ImGui.SameLine();
            if (ImGui.Button("End Day"))
                RunCommand(world, "end");
            ImGui.SameLine();
            if (ImGui.Button(_timePaused ? "Pause: ON" : "Pause: OFF"))
            {
                RunCommand(world, "pause");
                _timePaused = !_timePaused;
            }

            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("##day", ref _dayValue, 1, 1, 999);
            ImGui.SameLine();
            if (ImGui.Button("Set Day"))
                RunCommand(world, $"day {_dayValue}");
        }

        if (ImGui.CollapsingHeader("Stamina"))
        {
            ImGui.TextDisabled("Base max: 6 (+ modifiers)");
            ImGui.SetNextItemWidth(-1);
            ImGui.SliderFloat("##stamina", ref _staminaValue, 0f, 10f, "Stamina: %.1f");
            if (ImGui.Button("Set Stamina"))
                RunCommand(world, $"stamina {_staminaValue:F1}");
        }

        if (ImGui.CollapsingHeader("Inventory"))
        {
            if (ImGui.Button("Debug Inventory"))
                RunCommand(world, "gimme");
            ImGui.SameLine();
            if (ImGui.Button("Sword"))
                RunCommand(world, "sword");
            ImGui.SameLine();
            if (ImGui.Button("Learn All Recipes"))
                RunCommand(world, "learn all");
        }

        if (ImGui.CollapsingHeader("Levels"))
        {
            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("##level", ref _levelValue, 1, 1, 99);
            ImGui.SameLine();
            if (ImGui.Button("Set Level (all traces)"))
                RunCommand(world, $"level {_levelValue}");
        }

        if (ImGui.CollapsingHeader("NPCs"))
        {
            if (ImGui.Button("Meet All"))
                RunCommand(world, "meet all");
            ImGui.SameLine();
            if (ImGui.Button("Know All"))
                RunCommand(world, "know all");
            ImGui.SameLine();
            if (ImGui.Button("Unlock Birthdays"))
                RunCommand(world, "birthdays");

            ImGui.Spacing();
            ImGui.SetNextItemWidth(150);
            ImGui.InputText("NPC##name", ref _npcName, 64);
            if (_npcName.Length > 0)
            {
                if (ImGui.Button("Meet"))
                    RunCommand(world, $"meet {_npcName}");
                ImGui.SameLine();
                if (ImGui.Button("Know"))
                    RunCommand(world, $"know {_npcName}");
                ImGui.SameLine();
                if (ImGui.Button("Job"))
                    RunCommand(world, $"job {_npcName}");

                ImGui.SetNextItemWidth(120);
                ImGui.DragInt("##quest", ref _questProgress, 1, 1, 99);
                ImGui.SameLine();
                if (ImGui.Button("Advance Quest"))
                    RunCommand(world, $"quest {_npcName} {_questProgress}");
            }
        }

        if (ImGui.CollapsingHeader("Blackboard"))
        {
            if (ImGui.Button("List All Variables"))
                RunCommand(world, "list");

            ImGui.SetNextItemWidth(150);
            ImGui.InputText("Name##bb", ref _bbName, 128);
            ImGui.SetNextItemWidth(150);
            ImGui.InputText("Value##bb", ref _bbValue, 128);
            if (ImGui.Button("Set Variable") && _bbName.Length > 0)
                RunCommand(world, $"blackboard {_bbName} {_bbValue}");
        }

        if (ImGui.CollapsingHeader("Environment"))
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.SliderFloat("##rain", ref _rainAmount, 0f, 1f, "Rain: %.2f");
            if (ImGui.Button("Set Rain"))
                RunCommand(world, $"rain {_rainAmount:F2}");
            ImGui.SameLine();
            if (ImGui.Button("Rain Tomorrow"))
                RunCommand(world, "rainTomorrow");

            ImGui.Spacing();
            ImGui.SetNextItemWidth(-1);
            ImGui.SliderFloat("##glitch", ref _glitchAmount, 0f, 1f, "Glitch: %.2f");
            if (ImGui.Button("Set Glitch"))
                RunCommand(world, $"glitch {_glitchAmount:F2}");

            ImGui.SetNextItemWidth(-1);
            ImGui.SliderFloat("##noise", ref _noiseAmount, 0f, 1f, "Noise: %.0f");
            if (ImGui.Button("Set Noise"))
                RunCommand(world, $"noise {_noiseAmount:F0}");

            ImGui.Spacing();
            ImGui.SetNextItemWidth(120);
            ImGui.DragInt("##grow", ref _growDays, 1, 1, 99);
            ImGui.SameLine();
            if (ImGui.Button("Grow Plants"))
                RunCommand(world, $"grow {_growDays}");
        }

        if (ImGui.CollapsingHeader("Barduc"))
        {
            if (ImGui.Button("Win"))
                RunCommand(world, "barducWin");
            ImGui.SameLine();
            if (ImGui.Button("Lose"))
                RunCommand(world, "barducLose");
            ImGui.SameLine();
            if (ImGui.Button("Draw"))
                RunCommand(world, "barducDraw");
        }

        if (ImGui.CollapsingHeader("Misc"))
        {
            if (ImGui.Button("Quick Save"))
                RunCommand(world, "save");
        }
    }

    private static void RunCommand(World world, string command)
    {
        ConsoleEngine.Execute(world, command);
    }

    private static World? GetWorld()
    {
        return (MurderGame.Instance?.ActiveScene as GameScene)?.World;
    }
}
