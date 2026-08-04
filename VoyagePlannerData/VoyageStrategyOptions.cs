namespace DeepwaterEngagementSuite.VoyagePlannerData;

/// <summary>
/// Strategy toggles and caps. Every <c>Save*</c> field is a max hold count:
/// 0 disables that save strategy; positive values cap how many matching charts are held out.
/// </summary>
public sealed record VoyageStrategyOptions(
    bool UniqueAmuletClamCross = true,
    bool RareMonstersDrop = true,
    bool NoConsumeAnchorfield = true,
    bool CenterSpecialty = true,
    int SaveKishara = ChartIds.MaxSavedKishara,
    int SaveNoEquipment = ChartIds.MaxSavedNoEquipment,
    int SaveFractured = ChartIds.MaxSavedFractured,
    int SaveGoldenLanterns = ChartIds.MaxSavedGoldenLanterns,
    int SavePantheon = ChartIds.MaxSavedPantheon,
    int SaveSoulEater = ChartIds.MaxSavedSoulEater,
    int SaveRareFracture = ChartIds.MaxSavedRareFracture,
    int SaveRarePossessed = ChartIds.MaxSavedRarePossessed)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
