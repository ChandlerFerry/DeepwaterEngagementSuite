namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveGoldenLanternsStrategy()
    : SaveStrategyBase("SaveGoldenLanterns", SaveCountKeys.GoldenLanterns, StrategyOrders.SaveGoldenLanterns)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveGoldenLanterns;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsGoldenLanternsChart(piece);
}
