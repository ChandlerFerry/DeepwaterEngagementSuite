namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveRareFractureStrategy()
    : SaveStrategyBase("SaveRareFracture", SaveCountKeys.RareFracture, StrategyOrders.SaveRareFracture)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveRareFracture;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsRareFractureChart(piece);
}
