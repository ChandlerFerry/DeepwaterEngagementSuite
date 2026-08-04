namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveFracturedStrategy()
    : SaveStrategyBase("SaveFractured", SaveCountKeys.Fractured, StrategyOrders.SaveFractured)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveFractured;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsFracturedChart(piece);
}
