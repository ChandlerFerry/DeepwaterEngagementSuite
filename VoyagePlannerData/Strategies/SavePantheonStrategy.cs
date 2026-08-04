namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SavePantheonStrategy()
    : SaveStrategyBase("SavePantheon", SaveCountKeys.Pantheon, StrategyOrders.SavePantheon)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SavePantheon;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsPantheonChart(piece);
}
