using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Color = SharpDX.Color;

namespace DeepwaterEngagementSuite;

public partial class DeepwaterEngagementSuite
{
    /// <summary>
    ///     True when the plugin is configured to read sleeping entities AND ExileCore is actually collecting them.
    ///     ExileCore only populates <see cref="ExileCore.GameController.SleepingEntityListWrapper" /> when its own
    ///     CollectSleepingEntities debug setting is on, so both have to agree before we read anything.
    /// </summary>
    private bool SleepingEntityParsingActive =>
        Settings.SleepingEntitySettings.Enabled &&
        CoreSleepingCollectionEnabled &&
        GameController.SleepingEntityListWrapper != null;

    private bool CoreSleepingCollectionEnabled =>
        GameController?.Settings?.CoreSettings?.DebugSettings?.CollectSleepingEntities?.Value == true;

    /// <summary>
    ///     Yields the awake entities of the requested types, followed by any sleeping entities of those types that
    ///     were not already present in the awake list. <c>SleepingOnly</c> marks entities that came from the sleeping
    ///     list, whose components are likely unreadable and whose data should be treated as provisional.
    /// </summary>
    /// <remarks>
    ///     The two wrappers maintain completely independent caches, so an entity that has woken up appears in both
    ///     until the next area change. Deduplicating by id keeps the awake copy, which is the one with readable
    ///     components.
    /// </remarks>
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

    /// <summary>
    ///     Same as <see cref="ExpeditionSourceEntitiesTagged" /> for callers that do not care where the entity came from.
    /// </summary>
    private IEnumerable<Entity> ExpeditionSourceEntities(params EntityType[] types)
    {
        foreach (var (entity, _) in ExpeditionSourceEntitiesTagged(types))
        {
            yield return entity;
        }
    }

    /// <summary>
    ///     Fraction of the original alpha kept when drawing an icon for an entity we have only seen asleep.
    /// </summary>
    private const float SleepingIconAlphaScale = 0.45f;

    /// <summary>
    ///     Fraction of the way an icon's color is pushed toward gray when the entity has only been seen asleep.
    /// </summary>
    private const float SleepingIconDesaturation = 0.5f;

    /// <summary>
    ///     Mutes an icon tint so entities known only from the sleeping list read as provisional on the map and in
    ///     the world. Alpha alone is not enough on a bright map, so the color is also pulled toward its own
    ///     luminance.
    /// </summary>
    /// <remarks>
    ///     A null tint is not "no color" to the renderer, which falls back to white, so it is resolved here before
    ///     dimming rather than passed through untouched.
    /// </remarks>
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
