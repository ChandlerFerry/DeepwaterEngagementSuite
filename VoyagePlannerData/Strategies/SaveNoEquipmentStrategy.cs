namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveNoEquipmentStrategy()
    : SaveStrategyBase("SaveNoEquipment", SaveCountKeys.NoEquipment, StrategyOrders.SaveNoEquipment)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveNoEquipment;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsNoEquipmentChart(piece);
}
