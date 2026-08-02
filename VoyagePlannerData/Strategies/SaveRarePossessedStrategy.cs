namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveRarePossessedStrategy()
    : SaveStrategyBase("SaveRarePossessed", SaveCountKeys.RarePossessed, StrategyOrders.SaveRarePossessed)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveRarePossessed;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsRarePossessedChart(piece);
}
