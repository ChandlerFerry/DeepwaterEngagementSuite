namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveSoulEaterStrategy()
    : SaveStrategyBase("SaveSoulEater", SaveCountKeys.SoulEater, StrategyOrders.SaveSoulEater)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveSoulEater;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsSoulEaterChart(piece);
}
