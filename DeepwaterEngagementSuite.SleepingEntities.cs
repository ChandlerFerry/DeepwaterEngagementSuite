using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Color = SharpDX.Color;

namespace DeepwaterEngagementSuite;

public partial class DeepwaterEngagementSuite
{
    private bool PluginSleepingEnabled =>
        Settings?.IconSettings?.ParseSleepingEntities?.Value == true;

    private bool SleepingEntityParsingActive =>
        PluginSleepingEnabled &&
        CoreSleepingCollectionEnabled &&
        GameController?.SleepingEntityListWrapper != null;

    private bool CoreSleepingCollectionEnabled
    {
        get
        {
            try
            {
                return GameController?.Settings?.CoreSettings?.DebugSettings?.CollectSleepingEntities?.Value == true;
            }
            catch
            {
                return false;
            }
        }
    }

    private IEnumerable<(Entity Entity, bool SleepingOnly)> ExpeditionSourceEntitiesTagged(params EntityType[] types)
    {
        var seen = new HashSet<uint>();
        var awakeByType = GameController?.EntityListWrapper?.ValidEntitiesByType;
        if (awakeByType != null)
        {
            foreach (var type in types)
            {
                if (!awakeByType.TryGetValue(type, out var entities) || entities == null)
                {
                    continue;
                }

                foreach (var entity in entities)
                {
                    if (entity == null || string.IsNullOrEmpty(entity.Path))
                    {
                        continue;
                    }

                    seen.Add(entity.Id);
                    yield return (entity, false);
                }
            }
        }

        if (!SleepingEntityParsingActive)
        {
            yield break;
        }

        var sleepingByType = GameController.SleepingEntityListWrapper?.ValidEntitiesByType;
        if (sleepingByType == null)
        {
            yield break;
        }

        foreach (var type in types)
        {
            if (!sleepingByType.TryGetValue(type, out var entities) || entities == null)
            {
                continue;
            }

            foreach (var entity in entities)
            {
                if (entity == null || string.IsNullOrEmpty(entity.Path))
                {
                    continue;
                }

                if (seen.Add(entity.Id))
                {
                    yield return (entity, true);
                }
            }
        }
    }

    private IEnumerable<Entity> ExpeditionSourceEntities(params EntityType[] types)
    {
        foreach (var (entity, _) in ExpeditionSourceEntitiesTagged(types))
        {
            yield return entity;
        }
    }

    private void DropProvisionalSleepingCacheEntries()
    {
        if (SleepingEntityParsingActive || _cachedEntities.Count == 0)
        {
            return;
        }

        List<uint> remove = null;
        foreach (var (id, item) in _cachedEntities)
        {
            if (!item.SleepingOnly)
            {
                continue;
            }

            remove ??= new List<uint>();
            remove.Add(id);
        }

        if (remove == null)
        {
            return;
        }

        foreach (var id in remove)
        {
            _cachedEntities.Remove(id);
        }
    }

    private const float SleepingIconAlphaScale = 0.45f;
    private const float SleepingIconDesaturation = 0.5f;

    private static Color? DimForSleeping(Color? tint)
    {
        var color = tint ?? Color.White;
        var luminance = (byte)((color.R * 0.299f) + (color.G * 0.587f) + (color.B * 0.114f));

        static byte Blend(byte channel, byte target) =>
            (byte)(channel + ((target - channel) * SleepingIconDesaturation));

        return new Color(
            Blend(color.R, luminance),
            Blend(color.G, luminance),
            Blend(color.B, luminance),
            (byte)(color.A * SleepingIconAlphaScale));
    }

    private void DrawSleepingEntityWarning()
    {
        if (!PluginSleepingEnabled || CoreSleepingCollectionEnabled)
        {
            return;
        }

        ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f),
            "Sleeping entity collection is disabled in ExileCore.");
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f),
            "Enable Core -> Debug -> CollectSleepingEntities for this setting to have any effect.");
    }
}
