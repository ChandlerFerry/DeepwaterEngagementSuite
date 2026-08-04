namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveSoulEaterStrategy()
    : SaveStrategyBase("SaveSoulEater", SaveCountKeys.SoulEater, StrategyOrders.SaveSoulEater)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveSoulEater;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsSoulEaterChart(piece);
}
