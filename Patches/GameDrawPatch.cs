using System.Reflection;
using DevTools.Core;
using DevTools.UI;
using HarmonyLib;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Murder.Core;
using NeverwayMod.DevTools.Core;

namespace DevTools;

public static class GameDrawPatch
{
    private static ImGuiRenderer? _renderer;
    private static bool _prevF2;
    private static bool _prevF3;

    public static void Apply(Harmony harmony)
    {
        var draw = typeof(MurderGame).GetMethod(
            "Draw", BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(GameTime)]);

        if (draw == null)
        {
            DevToolsMod.Logger?.Warning("Could not find Game.Draw");
            return;
        }

        harmony.Patch(draw, postfix: new HarmonyMethod(typeof(GameDrawPatch), nameof(AfterDraw)));

        DevToolsMod.Logger?.Info("Patched Game.Draw - press F2 for DevTools overlay, F3 for noclip");
    }

    public static void AfterDraw(MurderGame __instance, GameTime gameTime)
    {
        try
        {
            AfterDrawInner(__instance, gameTime);
        }
        catch (Exception ex)
        {
            // Don't let mod crashes kill the game. Log and disable overlay.
            DevToolsMod.Logger?.Error($"DevTools error: {ex}");
            try { ConsoleEngine.AddInfo($"DevTools error: {ex.Message}"); } catch { }
            DevToolsMod.ShowOverlay = false;
        }
    }

    private static void AfterDrawInner(MurderGame __instance, GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        // F2 toggle overlay
        var f2 = keyboard.IsKeyDown(Keys.F2);
        if (f2 && !_prevF2)
            DevToolsMod.ShowOverlay = !DevToolsMod.ShowOverlay;
        _prevF2 = f2;

        // F3 toggle noclip (works even when overlay is hidden)
        var f3 = keyboard.IsKeyDown(Keys.F3);
        if (f3 && !_prevF3)
        {
            var world = GameHelper.GetMonoWorld();
            if (world != null)
                NoclipController.Toggle(world);
        }
        _prevF3 = f3;

        // Update noclip movement every frame (even when overlay hidden)
        {
            var world = GameHelper.GetMonoWorld();
            if (world != null)
                NoclipController.Update(world, (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        if (!DevToolsMod.ShowOverlay) return;

        // Force cursor visible every frame while overlay is open.
        // The game's Update() sets IsMouseVisible each frame based on its own
        // logic, so we just override it in Draw postfix. When the overlay closes,
        // we stop overriding and the game's next Update() restores its own state.
        __instance.IsMouseVisible = true;

        // Initialize ImGui on first use
        if (_renderer == null)
        {
            try
            {
                _renderer = new ImGuiRenderer(__instance);
                _renderer.RebuildFontAtlas();
                DevToolsMod.Logger?.Info("ImGui initialized");
            }
            catch (Exception ex)
            {
                DevToolsMod.Logger?.Error($"ImGui init failed: {ex.Message}");
                DevToolsMod.ShowOverlay = false;
                return;
            }
        }

        var dt = gameTime.ElapsedGameTime;
        if (dt.TotalSeconds <= 0)
            dt = TimeSpan.FromMilliseconds(16);
        var safeGameTime = new GameTime(gameTime.TotalGameTime, dt);

        // Poll engine logs before rendering so they appear in the console
        ConsoleEngine.PollEngineLogs();

        _renderer.BeforeLayout(safeGameTime);
        DevToolsOverlay.Render();
        _renderer.AfterLayout();

        // TODO: keyboard input suppression - Game.Update runs before Draw,
        // so setting KeyboardConsumed here is too late for the current frame.
        // Enter key still leaks through to the game. Needs a different approach.
    }
}
