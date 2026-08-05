using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Color = SharpDX.Color;

namespace DeepwaterEngagementSuite;

public partial class DeepwaterEngagementSuite
{
    
    
    private bool SleepingEntityParsingActive =>
        Settings.SleepingEntitySettings.Enabled &&
        CoreSleepingCollectionEnabled &&
        GameController.SleepingEntityListWrapper != null;

    private bool CoreSleepingCollectionEnabled =>
        GameController?.Settings?.CoreSettings?.DebugSettings?.CollectSleepingEntities?.Value == true;

    
    private IEnumerable<(Entity Entity, bool SleepingOnly)> ExpeditionSourceEntitiesTagged(params EntityType[] types)
    {
        var seen = new HashSet<uint>();

        foreach (var type in types)
        {
            foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[type])
            {
                seen.Add(entity.Id);
                yield return (entity, false);
            }
        }

        if (!SleepingEntityParsingActive)
        {
            yield break;
        }

        var sleepingByType = GameController.SleepingEntityListWrapper.ValidEntitiesByType;
        foreach (var type in types)
        {
            if (!sleepingByType.TryGetValue(type, out var entities))
            {
                continue;
            }

            foreach (var entity in entities)
            {
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
        if (!Settings.SleepingEntitySettings.Enabled || CoreSleepingCollectionEnabled)
        {
            return;
        }

        ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f),
            "Sleeping entity collection is disabled in ExileCore.");
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f),
            "Enable Core -> Debug -> CollectSleepingEntities for this setting to have any effect.");
    }
}
