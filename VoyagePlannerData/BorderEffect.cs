namespace DeepwaterEngagementSuite.VoyagePlannerData;

public record BorderEffect(
    string Name,
    ModifierTag Tags,
    double Multiplier,
    bool PerConnection,
    bool AffectsPlacedChart);
