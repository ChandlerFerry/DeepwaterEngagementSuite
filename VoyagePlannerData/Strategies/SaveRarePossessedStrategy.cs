namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveRarePossessedStrategy()
    : SaveStrategyBase("SaveRarePossessed", SaveCountKeys.RarePossessed, StrategyOrders.SaveRarePossessed)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveRarePossessed;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsRarePossessedChart(piece);
}
