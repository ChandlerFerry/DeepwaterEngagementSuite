namespace DeepwaterEngagementSuite.VoyagePlannerData;

public sealed record VoyageStrategyOptions(
    bool SaveKishara = true,
    bool PelagicOnOrbs = true,
    bool UniqueAmuletClamCross = true,
    bool DivineSupportRing = true,
    bool AnnulSupportRing = true,
    bool AncientSupportRing = true,
    bool NoConsumeAnchorfield = true,
    bool CenterSpecialty = true,
    bool DivineRareFill = true,
    bool SaveAnchorfield = true,
    bool SaveStrongboxes = true,
    bool SaveStarfish = true,
    bool SaveAdjacentRare = true,
    bool SaveRareVoyage = true,
    bool SaveOperative = true,
    bool SaveLostMessage = true,
    bool SaveUniqueAmuletAndClams = true)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
