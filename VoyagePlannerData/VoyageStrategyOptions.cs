namespace DeepwaterEngagementSuite.VoyagePlannerData;

public sealed record VoyageStrategyOptions(
    bool SaveKishara = true,
    bool PelagicOnOrbs = true,
    bool UniqueAmuletClamCross = true,
    bool OrbSupport = true,
    bool NoConsumeAnchorfield = true,
    bool CenterSpecialty = true,
    bool SaveSupportCharts = true)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
