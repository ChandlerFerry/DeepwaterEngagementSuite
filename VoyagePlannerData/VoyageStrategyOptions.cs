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
    /// <summary>
    /// When on, highlight Infinite Lanterns borders if the board has at least two.
    /// Alert-only — no locks or chart saves. Default off.
    /// </summary>
    bool InfiniteLanterns = false,
    int SaveKishara = ChartIds.MaxSavedKishara,
    int SaveNoEquipment = ChartIds.MaxSavedNoEquipment,
    int SaveFractured = ChartIds.MaxSavedFractured,
    int SaveGoldenLanterns = ChartIds.MaxSavedGoldenLanterns,
    int SavePantheon = ChartIds.MaxSavedPantheon,
    int SaveSoulEater = ChartIds.MaxSavedSoulEater,
    int SaveRareFracture = ChartIds.MaxSavedRareFracture,
    int SaveRarePossessed = ChartIds.MaxSavedRarePossessed,
    /// <summary>
    /// Low-priority starfish hold (0 = off). Runs after rare-monsters residual saves so it
    /// only keeps starfish that stronger rules did not already claim. Default 2.
    /// </summary>
    int SaveStarfish = ChartIds.MaxSavedStarfish)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
