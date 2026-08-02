namespace DeepwaterEngagementSuite.VoyagePlannerData;

public sealed record VoyageStrategyOptions(
    bool UniqueAmuletClamCross = true,
    bool RareMonstersDrop = true,
    bool NoConsumeAnchorfield = true,
    bool CenterSpecialty = true,
    bool SaveKishara = true,
    bool SaveNoEquipment = true,
    bool SaveFractured = true,
    bool SavePantheon = true,
    bool SaveSoulEater = false,
    bool SaveRareFracture = true,
    bool SaveRarePossessed = true)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
