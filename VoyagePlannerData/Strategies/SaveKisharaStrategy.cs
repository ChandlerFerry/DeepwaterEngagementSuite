namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveKisharaStrategy()
    : SaveStrategyBase("SaveKishara", SaveCountKeys.Kishara, StrategyOrders.SaveKishara)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveKishara;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsKishara(piece);
}
