using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using Bang.Components;
using ImGuiNET;

namespace DevTools.Core;

public static class ComponentReflector
{
    public struct FieldData
    {
        public FieldInfo Info;
        public object? Value;
        public bool HasSerialize;
    }

    private static Type[]? _componentTypesCache;
    private static string[]? _componentNamesCache;

    public static List<FieldData> GetFields(IComponent component)
    {
        var fields = new List<FieldData>();
        var type = component.GetType();
        var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in allFields)
        {
            // Skip compiler-generated backing fields for properties we'll handle separately
            if (field.Name.Contains("k__BackingField")) continue;

            fields.Add(new FieldData
            {
                Info = field,
                Value = field.GetValue(component),
                HasSerialize = field.GetCustomAttribute(typeof(Bang.SerializeAttribute)) != null
                    || field.IsPublic
            });
        }

        return fields;
    }

    public static IComponent? SetField(IComponent original, FieldInfo field, object? newValue)
    {
        try
        {
            // Box the struct so we can mutate it
            object boxed = original;
            field.SetValue(boxed, newValue);
            return (IComponent)boxed;
        }
        catch (Exception ex)
        {
            DevToolsMod.Logger?.Error($"Failed to set field {field.Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Renders an ImGui editor for a field value. Returns true if the value was changed.
    /// </summary>
    public static bool RenderField(string label, Type fieldType, object? currentValue, out object? newValue)
    {
        newValue = currentValue;

        if (fieldType == typeof(int))
        {
            var v = currentValue is int i ? i : 0;
            if (ImGui.InputInt(label, ref v))
            {
                newValue = v;
                return true;
            }
        }
        else if (fieldType == typeof(float))
        {
            var v = currentValue is float f ? f : 0f;
            if (ImGui.InputFloat(label, ref v))
            {
                newValue = v;
                return true;
            }
        }
        else if (fieldType == typeof(double))
        {
            var v = currentValue is double d ? d : 0.0;
            if (ImGui.InputDouble(label, ref v))
            {
                newValue = v;
                return true;
            }
        }
        else if (fieldType == typeof(bool))
        {
            var v = currentValue is bool b && b;
            if (ImGui.Checkbox(label, ref v))
            {
                newValue = v;
                return true;
            }
        }
        else if (fieldType == typeof(string))
        {
            var v = currentValue as string ?? "";
            if (ImGui.InputText(label, ref v, 256))
            {
                newValue = v;
                return true;
            }
        }
        else if (fieldType == typeof(Vector2))
        {
            var v = currentValue is Vector2 vec ? vec : Vector2.Zero;
            if (ImGui.InputFloat2(label, ref v))
            {
                newValue = v;
                return true;
            }
        }
        else if (fieldType == typeof(Microsoft.Xna.Framework.Vector2))
        {
            var xna = currentValue is Microsoft.Xna.Framework.Vector2 vec ? vec : Microsoft.Xna.Framework.Vector2.Zero;
            var v = new Vector2(xna.X, xna.Y);
            if (ImGui.InputFloat2(label, ref v))
            {
                newValue = new Microsoft.Xna.Framework.Vector2(v.X, v.Y);
                return true;
            }
        }
        else if (fieldType.IsEnum)
        {
            var names = Enum.GetNames(fieldType);
            var values = Enum.GetValues(fieldType);
            var currentIndex = currentValue != null ? Array.IndexOf(values, currentValue) : 0;
            if (currentIndex < 0) currentIndex = 0;

            if (ImGui.Combo(label, ref currentIndex, names, names.Length))
            {
                newValue = values.GetValue(currentIndex);
                return true;
            }
        }
        else if (fieldType == typeof(Guid))
        {
            var v = currentValue is Guid g ? g.ToString() : Guid.Empty.ToString();
            if (ImGui.InputText(label, ref v, 64))
            {
                if (Guid.TryParse(v, out var parsed))
                {
                    newValue = parsed;
                    return true;
                }
            }
        }
        else
        {
            // Read-only display for unsupported types
            ImGui.TextDisabled($"{label}: {currentValue ?? "null"} ({fieldType.Name})");
        }

        return false;
    }

    public static Type[] GetAllComponentTypes()
    {
        if (_componentTypesCache != null) return _componentTypesCache;

        var types = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsAbstract && !type.IsInterface
                        && typeof(IComponent).IsAssignableFrom(type)
                        && type.IsValueType)
                    {
                        types.Add(type);
                    }
                }
            }
            catch { /* Some assemblies may not be loadable */ }
        }

        _componentTypesCache = types.OrderBy(t => t.Name).ToArray();
        _componentNamesCache = _componentTypesCache.Select(t => t.Name).ToArray();
        return _componentTypesCache;
    }

    public static string[] GetAllComponentNames()
    {
        if (_componentNamesCache == null) GetAllComponentTypes();
        return _componentNamesCache!;
    }
}
