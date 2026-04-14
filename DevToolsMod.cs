using HarmonyLib;
using MurderModLoader.API;

namespace DevTools;

public class DevToolsMod : IMurderMod
{
    private Harmony? _harmony;
    internal static IModLogger? Logger;
    internal static bool ShowOverlay = true;

    public void OnLoad(ModContext context)
    {
        Logger = context.Logger;
        _harmony = new Harmony(context.HarmonyId);

        try
        {
            GameDrawPatch.Apply(_harmony);
        }
        catch (Exception ex)
        {
            Logger.Error($"Harmony patch failed: {ex.Message}");
        }
    }

    public void OnUnload()
    {
        Core.NoclipController.ForceDisable();
        _harmony?.UnpatchAll(_harmony.Id);
    }
}
