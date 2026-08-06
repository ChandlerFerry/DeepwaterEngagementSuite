using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveUniqueAmulet2Strategy()
    : SaveStrategyBase("SaveUniqueAmulet2", SaveCountKeys.UniqueAmulet2, StrategyOrders.SaveUniqueAmulet2)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveUniqueAmulet2;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsUniqueAmulet2Chart(piece);
    protected override Func<MapPiece, double> SaveScore => ChartPredicates.UniqueAmuletScore;
}
