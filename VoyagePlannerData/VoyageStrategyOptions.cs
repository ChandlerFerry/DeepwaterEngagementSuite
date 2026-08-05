namespace DeepwaterEngagementSuite.VoyagePlannerData;


public sealed record VoyageStrategyOptions(
    bool UniqueAmuletClamCross = true,
    bool RareMonstersDrop = true,
    bool NoConsumeAnchorfield = true,
    bool CenterSpecialty = true,
    
    
    bool InfiniteLanterns = false,
    int SaveKishara = ChartIds.MaxSavedKishara,
    int SaveNoEquipment = ChartIds.MaxSavedNoEquipment,
    int SaveFractured = ChartIds.MaxSavedFractured,
    int SaveGoldenLanterns = ChartIds.MaxSavedGoldenLanterns,
    int SavePantheon = ChartIds.MaxSavedPantheon,
    int SaveSoulEater = ChartIds.MaxSavedSoulEater,
    int SaveRareFracture = ChartIds.MaxSavedRareFracture,
    int SaveRarePossessed = ChartIds.MaxSavedRarePossessed,
    
    
    int SaveStarfish = ChartIds.MaxSavedStarfish)
{
    public static VoyageStrategyOptions AllEnabled { get; } = new();
}
