using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveGoldenLanternsStrategy()
    : SaveStrategyBase("SaveGoldenLanterns", SaveCountKeys.GoldenLanterns, StrategyOrders.SaveGoldenLanterns)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveGoldenLanterns;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsGoldenLanternsChart(piece);
    protected override Func<MapPiece, double> SaveScore => ChartPredicates.GoldenLanternsSaveScore;
}
