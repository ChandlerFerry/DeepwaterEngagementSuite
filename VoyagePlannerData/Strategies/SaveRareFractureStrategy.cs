namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveRareFractureStrategy()
    : SaveStrategyBase("SaveRareFracture", SaveCountKeys.RareFracture, StrategyOrders.SaveRareFracture)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveRareFracture;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsRareFractureChart(piece);
}
