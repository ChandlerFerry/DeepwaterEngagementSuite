namespace DeepwaterEngagementSuite.VoyagePlannerData;

public record Modifier(
    string Name,
    double Weight,
    bool IsGlobal = false,
    ModifierTag Tags = ModifierTag.None,
    int Value1 = 0);
