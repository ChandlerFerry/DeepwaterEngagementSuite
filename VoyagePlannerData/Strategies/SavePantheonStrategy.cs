namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SavePantheonStrategy()
    : SaveStrategyBase("SavePantheon", SaveCountKeys.Pantheon, StrategyOrders.SavePantheon)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SavePantheon;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsPantheonChart(piece);
}
