using Bang;
using Murder.Core;
using MurderGame = Murder.Game;

namespace NeverwayMod.DevTools.Core;

internal static class GameHelper
{
    /// <summary>Get the current scene's World, or null if no scene is active.</summary>
    public static World? GetWorld()
    {
        return (MurderGame.Instance?.ActiveScene as GameScene)?.World;
    }

    /// <summary>Get the current scene's MonoWorld, or null if no scene is active.</summary>
    public static MonoWorld? GetMonoWorld()
    {
        return GetWorld() as MonoWorld;
    }
}
