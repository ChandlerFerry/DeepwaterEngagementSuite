using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveUniqueAmulet1Strategy()
    : SaveStrategyBase("SaveUniqueAmulet1", SaveCountKeys.UniqueAmulet1, StrategyOrders.SaveUniqueAmulet1)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveUniqueAmulet1;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsUniqueAmulet1Chart(piece);
    protected override Func<MapPiece, double> SaveScore => ChartPredicates.UniqueAmuletScore;
}
