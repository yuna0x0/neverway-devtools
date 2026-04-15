using System.Collections.Immutable;
using Bang;
using Bang.Components;
using Bang.Entities;
using DevTools.Core;
using ImGuiNET;
using Murder.Core;
using Murder.Services;
using Murder.Utilities;
using NeverwayMod.DevTools.Core;

namespace DevTools.UI;

public static class ComponentsPanel
{
    private static string _entitySearch = "";
    private static string _componentFilter = "";
    private static int _selectedEntityId = -1;
    private static int _addComponentIndex = 0;

    public static void Render()
    {
        var world = GameHelper.GetMonoWorld();
        if (world == null)
        {
            ImGui.TextColored(UIColors.Error, "No active world.");
            return;
        }

        // Left pane: entity list
        ImGui.BeginChild("EntityList", new System.Numerics.Vector2(280, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX);
        RenderEntityList(world);
        ImGui.EndChild();

        ImGui.SameLine();

        // Right pane: inspector
        ImGui.BeginChild("Inspector", System.Numerics.Vector2.Zero, ImGuiChildFlags.Borders);
        RenderInspector(world);
        ImGui.EndChild();
    }

    private static void RenderEntityList(MonoWorld world)
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search entities...", ref _entitySearch, 128);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##compfilter", "Filter by component...", ref _componentFilter, 128);

        ImGui.Separator();

        var entities = world.GetAllEntities();
        var count = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (entity.IsDestroyed) continue;

            var displayName = GetEntityDisplayName(entity);

            // Apply search filter
            if (_entitySearch.Length > 0 &&
                !displayName.Contains(_entitySearch, StringComparison.OrdinalIgnoreCase) &&
                !entity.EntityId.ToString().Contains(_entitySearch))
                continue;

            // Apply component filter
            if (_componentFilter.Length > 0)
            {
                bool hasMatch = false;
                try
                {
                    foreach (var comp in entity.Components)
                    {
                        if (comp.GetType().Name.Contains(_componentFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            hasMatch = true;
                            break;
                        }
                    }
                }
                catch { }
                if (!hasMatch) continue;
            }

            count++;

            var isSelected = entity.EntityId == _selectedEntityId;
            var label = $"#{entity.EntityId} {displayName}";

            if (entity.HasPosition())
            {
                var pos = entity.GetGlobalPosition();
                label += $" ({pos.X:F0},{pos.Y:F0})";
            }

            if (ImGui.Selectable(label, isSelected))
            {
                _selectedEntityId = entity.EntityId;
            }
        }

        ImGui.Separator();
        ImGui.TextDisabled($"{count} entities");
    }

    private static void RenderInspector(MonoWorld world)
    {
        if (_selectedEntityId < 0)
        {
            ImGui.TextDisabled("Select an entity to inspect.");
            return;
        }

        var entity = world.TryGetEntity(_selectedEntityId);
        if (entity == null)
        {
            ImGui.TextColored(UIColors.Error, $"Entity #{_selectedEntityId} destroyed.");
            _selectedEntityId = -1;
            return;
        }

        // Header
        var name = GetEntityDisplayName(entity);
        ImGui.Text($"Entity #{entity.EntityId}");
        ImGui.SameLine();
        ImGui.TextDisabled(name);
        if (entity.IsDeactivated)
        {
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "[DEACTIVATED]");
        }

        if (entity.HasPosition())
        {
            var pos = entity.GetGlobalPosition();
            ImGui.Text($"Position: ({pos.X:F1}, {pos.Y:F1})");
        }

        ImGui.Separator();

        // Components
        ImmutableArray<IComponent> components;
        try
        {
            components = entity.Components;
        }
        catch
        {
            ImGui.TextColored(UIColors.Error, "Failed to read components.");
            return;
        }

        var componentCount = 0;
        foreach (var component in components)
        {
            componentCount++;
            var componentType = component.GetType();
            var typeName = componentType.Name;

            // Remove button before the header
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.6f, 0.2f, 0.2f, 1f));
            if (ImGui.SmallButton($"X##{componentType.FullName}"))
            {
                try
                {
                    entity.RemoveComponent(componentType);
                    ConsoleEngine.AddInfo($"Removed {typeName} from entity #{entity.EntityId}");
                }
                catch (Exception ex)
                {
                    ConsoleEngine.AddInfo($"Failed to remove {typeName}: {ex.Message}");
                }
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            var nodeOpen = ImGui.CollapsingHeader($"{typeName}##{componentType.FullName}");
            if (!nodeOpen) continue;

            ImGui.PushID(componentType.FullName);
            ImGui.Indent();

            var fields = ComponentReflector.GetFields(component);
            if (fields.Count == 0)
            {
                ImGui.TextDisabled("(no fields)");
            }
            else
            {
                foreach (var field in fields)
                {
                    var label = field.Info.Name;
                    if (!field.HasSerialize)
                        label += " (private)";

                    if (ComponentReflector.RenderField($"{label}##{typeName}", field.Info.FieldType, field.Value, out var newValue))
                    {
                        var modified = ComponentReflector.SetField(component, field.Info, newValue);
                        if (modified != null)
                        {
                            try
                            {
                                entity.ReplaceComponent(modified, componentType);
                            }
                            catch (Exception ex)
                            {
                                ConsoleEngine.AddInfo($"Failed to update {typeName}.{field.Info.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            ImGui.Unindent();
            ImGui.PopID();
        }

        ImGui.Separator();
        ImGui.TextDisabled($"{componentCount} components");

        // Add component
        ImGui.Spacing();
        var compTypes = ComponentReflector.GetAllComponentTypes();
        var compNames = ComponentReflector.GetAllComponentNames();
        if (compNames.Length > 0)
        {
            ImGui.SetNextItemWidth(200);
            ImGui.Combo("##addcomp", ref _addComponentIndex, compNames, compNames.Length);
            ImGui.SameLine();
            if (ImGui.Button("Add") && _addComponentIndex < compTypes.Length)
            {
                var type = compTypes[_addComponentIndex];
                try
                {
                    var instance = Activator.CreateInstance(type) as IComponent;
                    if (instance != null)
                    {
                        entity.AddComponent(instance, type);
                        ConsoleEngine.AddInfo($"Added {type.Name} to entity #{entity.EntityId}");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleEngine.AddInfo($"Failed to add {type.Name}: {ex.Message}");
                }
            }
        }
    }

    private static string GetEntityDisplayName(Entity entity)
    {
        try
        {
            // Use Murder's built-in entity naming via PrefabRefComponent
            var name = EntityServices.TryGetEntityName(entity);
            if (name != null) return name;
        }
        catch { }

        // Fallback: check for known unique component types
        try
        {
            foreach (var comp in entity.Components)
            {
                var typeName = comp.GetType().Name;
                if (typeName == "PlayerComponent") return "Player";
            }
        }
        catch { }

        return "Entity";
    }
}
