using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveGoldenLanternsStrategy()
    : SaveStrategyBase("SaveGoldenLanterns", SaveCountKeys.GoldenLanterns, StrategyOrders.SaveGoldenLanterns)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SaveGoldenLanterns;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsGoldenLanternsChart(piece);

    public override void Apply(PlacementContext ctx)
    {
        if (!IsEnabled(ctx.Options))
            return;

        var maxSave = Math.Max(0, ctx.Options.MaxSavedGoldenLanterns);
        ctx.AddSaved(SaveKey,
            ctx.RemoveUnused(Matches, ChartPredicates.GoldenLanternsSaveScore, maxSave: maxSave));
    }
}
