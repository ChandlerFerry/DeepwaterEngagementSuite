namespace DeepwaterEngagementSuite.VoyagePlannerData;

/// <param name="Value1">Game mod magnitude (e.g. starfish/box count from ItemMod.Value1).</param>
public record Modifier(
    string Name,
    double Weight,
    bool IsGlobal = false,
    ModifierTag Tags = ModifierTag.None,
    int Value1 = 0);
