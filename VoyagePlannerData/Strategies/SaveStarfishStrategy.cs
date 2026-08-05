using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;


public sealed class SaveStarfishStrategy()
    : SaveStrategyBase("SaveStarfish", SaveCountKeys.Starfish, StrategyOrders.SaveStarfish)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveStarfish;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsStarfishChart(piece);
    protected override Func<MapPiece, double> SaveScore => ChartPredicates.StarfishScore;
}
