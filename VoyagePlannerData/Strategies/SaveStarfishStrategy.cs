using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

/// <summary>
/// Low-priority starfish hold. Runs after rare-monsters residual saves so it only
/// keeps charts that stronger rules (boxes / residual starfish / rare T2) left behind.
/// </summary>
public sealed class SaveStarfishStrategy()
    : SaveStrategyBase("SaveStarfish", SaveCountKeys.Starfish, StrategyOrders.SaveStarfish)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveStarfish;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsStarfishChart(piece);
    protected override Func<MapPiece, double> SaveScore => ChartPredicates.StarfishScore;
}
