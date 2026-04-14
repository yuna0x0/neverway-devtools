using System.Numerics;
using Bang;
using Bang.Entities;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Murder.Core;
using Murder.Utilities;

namespace DevTools.Core;

public static class NoclipController
{
    public static bool IsActive { get; private set; }
    public static float MoveSpeed = 300f;
    public static bool FreeCameraMode = false;

    private static Vector2 _savedPosition;
    private static readonly List<Type> _deactivatedSystems = new();
    private static object? _lastScene;

    // System types to deactivate during noclip
    private static readonly string[] _systemTypeNames =
    [
        "Murder.Systems.Physics.SATPhysicsSystem",
        "Road.Systems.PlayerInputSystem",
        "Murder.Systems.Agents.AgentMoveToSystem",
    ];

    public static void Toggle(MonoWorld world)
    {
        if (IsActive)
            Disable(world);
        else
            Enable(world);
    }

    public static void Enable(MonoWorld world)
    {
        if (IsActive) return;

        var player = world.TryGetUniqueEntityPlayer();
        if (player == null)
        {
            ConsoleEngine.AddInfo("Noclip: No player entity found.");
            return;
        }

        _savedPosition = player.GetGlobalPosition();
        _lastScene = Murder.Game.Instance?.ActiveScene;

        // Deactivate systems
        _deactivatedSystems.Clear();
        foreach (var typeName in _systemTypeNames)
        {
            var type = FindType(typeName);
            if (type != null)
            {
                try
                {
                    world.DeactivateSystem(type);
                    _deactivatedSystems.Add(type);
                }
                catch { /* System may not be active */ }
            }
        }

        IsActive = true;
        ConsoleEngine.AddInfo($"Noclip: ENABLED at ({_savedPosition.X:F0}, {_savedPosition.Y:F0})");
    }

    public static void Disable(MonoWorld world)
    {
        if (!IsActive) return;

        foreach (var type in _deactivatedSystems)
        {
            try { world.ActivateSystem(type); }
            catch { /* System may not exist in this world */ }
        }
        _deactivatedSystems.Clear();

        IsActive = false;
        ConsoleEngine.AddInfo("Noclip: DISABLED");
    }

    public static void ForceDisable()
    {
        if (!IsActive) return;

        var world = (Murder.Game.Instance?.ActiveScene as GameScene)?.World as MonoWorld;
        if (world != null)
            Disable(world);
        else
            IsActive = false;
    }

    public static void Update(MonoWorld world, float deltaTime)
    {
        if (!IsActive) return;

        // Auto-reset on scene change
        var currentScene = Murder.Game.Instance?.ActiveScene;
        if (currentScene != _lastScene)
        {
            IsActive = false;
            _deactivatedSystems.Clear();
            return;
        }

        // Don't process movement if ImGui wants keyboard
        if (ImGui.GetIO().WantCaptureKeyboard) return;

        var keyboard = Keyboard.GetState();
        var direction = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
            direction.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
            direction.Y += 1;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            direction.X -= 1;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            direction.X += 1;

        if (direction == Vector2.Zero) return;

        // Normalize diagonal movement
        if (direction.LengthSquared() > 1)
            direction = Vector2.Normalize(direction);

        var speed = MoveSpeed;
        if (keyboard.IsKeyDown(Keys.LeftShift))
            speed *= 3f;

        var delta = direction * speed * deltaTime;

        if (FreeCameraMode)
        {
            world.Camera.Position += delta;
        }
        else
        {
            var player = world.TryGetUniqueEntityPlayer();
            if (player != null)
            {
                var pos = player.GetGlobalPosition();
                player.SetGlobalPosition(pos + delta);
            }
        }
    }

    public static void TeleportTo(MonoWorld world, Vector2 position)
    {
        var player = world.TryGetUniqueEntityPlayer();
        if (player != null)
        {
            player.SetGlobalPosition(position);
            ConsoleEngine.AddInfo($"Teleported to ({position.X:F0}, {position.Y:F0})");
        }
    }

    public static void ResetPosition(MonoWorld world)
    {
        TeleportTo(world, _savedPosition);
    }

    public static Vector2 GetSavedPosition() => _savedPosition;

    private static Type? FindType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName);
            if (type != null) return type;
        }
        return null;
    }
}
