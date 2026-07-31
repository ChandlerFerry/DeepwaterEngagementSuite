using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace DeepwaterEngagementSuite;

public partial class DeepwaterEngagementSuite
{
    private readonly Dictionary<uint, TrailCacheItem> _trailMarkers = new();
    private readonly List<Vector2> _trailPointerTargets = [];
    private readonly List<Vector2> _completedTrailTargets = [];

    private record TrailCacheItem(
        IconPickerIndex Type,
        Vector2 GridPos,
        int MissingTicks = 0);

    private void ResetTrailTracking()
    {
        _trailMarkers.Clear();
        _trailPointerTargets.Clear();
        _completedTrailTargets.Clear();
    }

    private void TrackTrailEntity(Entity entity)
    {
        if (!Settings.TrailSettings.Enabled ||
            GetEntityType(entity.Path) == ExpeditionEntityType.None)
        {
            return;
        }

        var type = GetChestType(entity.Path);
        var gridPos = entity.PosNum.WorldToGrid();
        if (IsTrailEntityCompleted(entity, type))
        {
            _trailMarkers.Remove(entity.Id);
            RememberCompletedTrailTarget(gridPos);
            return;
        }

        var completedAtPosition = _completedTrailTargets
            .Where(x => Vector2.Distance(x, gridPos) <= 5)
            .ToList();
        foreach (var completed in completedAtPosition)
        {
            _completedTrailTargets.Remove(completed);
        }

        _trailMarkers[entity.Id] = new TrailCacheItem(type, gridPos);
    }

    private void UpdateTrailTracking()
    {
        if (!Settings.TrailSettings.Enabled)
        {
            ResetTrailTracking();
            return;
        }

        var rawPointerTargets = ReadRawPointerTargets();
        var seenIds = new HashSet<uint>();

        foreach (var entity in new[] { EntityType.Chest, EntityType.Terrain, EntityType.IngameIcon }
                     .SelectMany(x => GameController.EntityListWrapper.ValidEntitiesByType[x]))
        {
            if (GetEntityType(entity.Path) == ExpeditionEntityType.None)
            {
                continue;
            }

            seenIds.Add(entity.Id);
            TrackTrailEntity(entity);
        }

        foreach (var missing in _trailMarkers
                     .Where(x => !seenIds.Contains(x.Key))
                     .ToList())
        {
            if (!DisappearsWhenConsumed(missing.Value.Type))
            {
                if (missing.Value.MissingTicks != 0)
                {
                    _trailMarkers[missing.Key] = missing.Value with { MissingTicks = 0 };
                }

                continue;
            }

            // A surviving pointer means the encounter was merely unloaded
            // because the player moved away. Preserve the known type and place.
            if (rawPointerTargets.Any(x => Vector2.Distance(x, missing.Value.GridPos) <= 20))
            {
                if (missing.Value.MissingTicks != 0)
                {
                    _trailMarkers[missing.Key] = missing.Value with { MissingTicks = 0 };
                }

                continue;
            }

            var missingTicks = missing.Value.MissingTicks + 1;
            if (missingTicks < 5)
            {
                _trailMarkers[missing.Key] = missing.Value with { MissingTicks = missingTicks };
                continue;
            }

            RememberCompletedTrailTarget(missing.Value.GridPos);
            _trailMarkers.Remove(missing.Key);
        }

        _trailPointerTargets.Clear();
        foreach (var target in rawPointerTargets)
        {
            if (_completedTrailTargets.Any(x => Vector2.Distance(x, target) <= 5) ||
                _trailMarkers.Values.Any(x => Vector2.Distance(x.GridPos, target) <= 20) ||
                _trailPointerTargets.Any(x => Vector2.Distance(x, target) <= 1))
            {
                continue;
            }

            _trailPointerTargets.Add(target);
        }
    }

    private List<Vector2> ReadRawPointerTargets()
    {
        var result = new List<Vector2>();
        foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Terrain]
                     .Where(x => x.Path == "Metadata/Terrain/Leagues/Deepwater/Objects/Pointer"))
        {
            if (!entity.TryGetComponent(out Pointer pointer))
            {
                continue;
            }

            foreach (var target in pointer.Targets)
            {
                if (!result.Any(x => Vector2.Distance(x, target) <= 1))
                {
                    result.Add(target);
                }
            }
        }

        return result;
    }

    private static bool IsTrailEntityCompleted(Entity entity, IconPickerIndex type)
    {
        if (entity.IsOpened ||
            (entity.TryGetComponent(out Chest chest) && chest.IsOpened))
        {
            return true;
        }

        // Targetable is range-dependent and must not be used as completion.
        // Cursed Ducat exposes a stable StateMachine transition instead.
        return type == IconPickerIndex.CursedDucatDrop &&
               entity.TryGetComponent(out StateMachine stateMachine) &&
               stateMachine.States.Any(x => x.Name == "activated" && x.Value == 1);
    }

    private static bool DisappearsWhenConsumed(IconPickerIndex type) => type is
        IconPickerIndex.IzaroObject or
        IconPickerIndex.AltarCrab or
        IconPickerIndex.AltarOctopus or
        IconPickerIndex.TormentedSpiritEncounter or
        IconPickerIndex.LanternReplenishEncounter or
        IconPickerIndex.GoldenLanternEncounter or
        IconPickerIndex.InfusedCoralEncounter;

    private void RememberCompletedTrailTarget(Vector2 gridPos)
    {
        if (!_completedTrailTargets.Any(x => Vector2.Distance(x, gridPos) <= 5))
        {
            _completedTrailTargets.Add(gridPos);
        }
    }

    private void RenderTrailOverlay(bool largePanelsOpen)
    {
        if (!Settings.TrailSettings.Enabled)
        {
            return;
        }

        if (!largePanelsOpen)
        {
            DrawPersistentTrails();
        }

        if (Settings.TrailSettings.ShowLootWindow)
        {
            DrawTrailLootWindow();
        }
    }

    private void DrawPersistentTrails()
    {
        var largeMapVisible = _largeMapOpen;
        var drawMap = Settings.TrailSettings.DrawOnLargeMap && largeMapVisible;
        var drawWorld = Settings.TrailSettings.DrawInWorld && !largeMapVisible;
        if (!drawMap && !drawWorld)
        {
            return;
        }

        var maxDistance = Settings.TrailSettings.MaxDistance.Value;
        var maxDistanceSquared = maxDistance * maxDistance;
        var markers = _trailMarkers.Values
            .Select(x => (Type: x.Type, GridPos: x.GridPos))
            .Concat(_trailPointerTargets.Select(x => (Type: IconPickerIndex.PointerTarget, GridPos: x)));

        foreach (var marker in markers)
        {
            var delta = marker.GridPos - _playerGridPos;
            if (delta.LengthSquared() > maxDistanceSquared)
            {
                continue;
            }

            if (Settings.TrailSettings.OnlyUnreachable && IsTrailTargetReachable(marker.GridPos))
            {
                continue;
            }

            var isPointer = marker.Type == IconPickerIndex.PointerTarget;
            var mapColor = isPointer
                ? Settings.TrailSettings.UndiscoveredColor.Value
                : GetTrailColor(marker.Type, Settings.TrailSettings.DefaultMapColor.Value);
            var worldColor = isPointer
                ? Settings.TrailSettings.UndiscoveredColor.Value
                : GetTrailColor(marker.Type, Settings.TrailSettings.DefaultWorldColor.Value);
            var label = GetTrailName(marker.Type);

            if (drawMap)
            {
                var from = Graphics.GridToMap(_playerGridPos, _playerGridPos);
                var to = Graphics.GridToMap(marker.GridPos, _playerGridPos);
                Graphics.DrawLine(from, to, Settings.TrailSettings.MapLineWidth, mapColor);
                if (Settings.TrailSettings.ShowLabels)
                {
                    Graphics.DrawTextWithBackground(label, (from + to) * 0.5f, mapColor, FontAlign.Center, Color.Black);
                }
            }

            if (drawWorld)
            {
                var from = Camera.WorldToScreen(ExpandWithTerrainHeight(_playerGridPos));
                var to = Camera.WorldToScreen(ExpandWithTerrainHeight(marker.GridPos));
                Graphics.DrawLine(from, to, Settings.TrailSettings.WorldLineWidth, worldColor);
                if (Settings.TrailSettings.ShowLabels)
                {
                    Graphics.DrawTextWithBackground(label, (from + to) * 0.5f, worldColor, FontAlign.Center, Color.Black);
                }
            }
        }
    }

    private Color GetTrailColor(IconPickerIndex type, Color fallback)
    {
        var configuredTint = Settings.IconMapping
            .GetValueOrDefault(type, new IconDisplaySettings())
            .Tint;
        return configuredTint ??
               DeepwaterEngagementSuiteSettings.GetDefaultTint(type) ??
               fallback;
    }

    private bool IsTrailTargetReachable(Vector2 gridPos)
    {
        var target = gridPos.TruncateToVector2I();
        return Bubbles.Any(x => x.Position.DistanceSqr(target) <= x.Radius * x.Radius);
    }

    private void DrawTrailLootWindow()
    {
        ImGui.SetNextWindowSizeConstraints(new Vector2(500, 0), new Vector2(float.MaxValue, float.MaxValue));
        ImGui.SetNextWindowSize(new Vector2(500, 0), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Deepwater Loot", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.End();
            return;
        }

        var maxLanterns = Handler.MaxLanternCount;
        var placedLanterns = Handler.PlacedLanternCount;
        var remainingLanterns = Math.Max(0, maxLanterns - placedLanterns);
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f),
            $"Lanterns: {placedLanterns}/{maxLanterns}  |  Remaining: {remainingLanterns}");
        ImGui.Separator();

        var entries = _trailMarkers.Values
            .Select(x => (
                Type: x.Type,
                Distance: Vector2.Distance(x.GridPos, _playerGridPos),
                Reachable: IsTrailTargetReachable(x.GridPos)))
            .Concat(_trailPointerTargets.Select(x => (
                Type: IconPickerIndex.PointerTarget,
                Distance: Vector2.Distance(x, _playerGridPos),
                Reachable: IsTrailTargetReachable(x))))
            .Where(x => x.Distance <= Settings.TrailSettings.MaxDistance.Value)
            .ToList();

        if (entries.Count == 0)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "No targets discovered yet");
            ImGui.End();
            return;
        }

        var grouped = entries
            .GroupBy(x => x.Type)
            .Select(x => (
                Type: x.Key,
                Total: x.Count(),
                Reachable: x.Count(y => y.Reachable),
                NeedsLantern: x.Count(y => !y.Reachable),
                Nearest: x.Min(y => y.Distance)))
            .OrderByDescending(x => x.NeedsLantern)
            .ThenBy(x => x.Nearest)
            .ToList();

        ImGui.Text($"Found: {entries.Count} ({entries.Count(x => x.Reachable)} reachable, {entries.Count(x => !x.Reachable)} need pylon)");
        ImGui.Separator();

        if (ImGui.BeginTable("DeepwaterLootTable", 4, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 220);
            ImGui.TableSetupColumn("Need", ImGuiTableColumnFlags.WidthFixed, 55);
            ImGui.TableSetupColumn("Ok", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Nearest", ImGuiTableColumnFlags.WidthFixed, 75);

            foreach (var group in grouped)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextColored(
                    group.NeedsLantern > 0
                        ? new Vector4(1f, 0.7f, 0.2f, 1f)
                        : new Vector4(0.3f, 0.9f, 0.3f, 1f),
                    GetTrailName(group.Type));

                ImGui.TableNextColumn();
                ImGui.TextColored(
                    group.NeedsLantern > 0
                        ? new Vector4(1f, 0.4f, 0.4f, 1f)
                        : new Vector4(0.3f, 0.3f, 0.3f, 1f),
                    group.NeedsLantern > 0 ? $"{group.NeedsLantern}" : "-");

                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.3f, 0.9f, 0.3f, 1f), $"{group.Reachable} ok");

                ImGui.TableNextColumn();
                ImGui.Text($"{group.Nearest:0}");
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private static string GetTrailName(IconPickerIndex type) => type switch
    {
        IconPickerIndex.BottledItemChest => "Bottled Item",
        IconPickerIndex.GoldTreasureChest => "Gold Treasure",
        IconPickerIndex.ClamTreasureChest => "Clam Treasure",
        IconPickerIndex.CurrencyTreasureChest => "Currency",
        IconPickerIndex.UniqueWeaponChest => "Unique Weapon",
        IconPickerIndex.UniqueArmourChest => "Unique Armour",
        IconPickerIndex.ScarabChest => "Scarabs",
        IconPickerIndex.StackedDecksChest => "Stacked Decks",
        IconPickerIndex.MapsChest => "Maps",
        IconPickerIndex.AllflameEmbersChest => "Allflame Embers",
        IconPickerIndex.CursedDucatDrop => "Cursed Ducat",
        IconPickerIndex.RandomDucatChest => "Random Ducat",
        IconPickerIndex.IzaroObject => "Izaro",
        IconPickerIndex.AltarCrab => "Altar (Crab)",
        IconPickerIndex.AltarOctopus => "Altar (Octopus)",
        IconPickerIndex.TormentedSpiritEncounter => "Tormented Spirit",
        IconPickerIndex.LanternReplenishEncounter => "Lantern Replenish",
        IconPickerIndex.GoldenLanternEncounter => "Golden Lantern",
        IconPickerIndex.InfusedCoralEncounter => "Infused Coral",
        IconPickerIndex.PointerTarget => "Undiscovered Target",
        _ => "Other",
    };
}
