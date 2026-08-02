namespace DeepwaterEngagementSuite.VoyagePlannerData;

public sealed record VoyageStrategyOptions(
    bool SaveKishara = true,
    bool UniqueAmuletClamCross = true,
    bool RareMonstersDrop = true,
    bool NoConsumeAnchorfield = true,
    bool CenterSpecialty = true)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
