using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SavePantheonStrategy()
    : SaveStrategyBase("SavePantheon", SaveCountKeys.Pantheon, StrategyOrders.SavePantheon)
{
    public override bool IsEnabled(VoyageStrategyOptions options) => options.SavePantheon;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsPantheonChart(piece);

    public override void Apply(PlacementContext ctx)
    {
        if (!IsEnabled(ctx.Options))
            return;

        var maxSave = Math.Max(0, ctx.Options.MaxSavedPantheon);
        ctx.AddSaved(SaveKey, ctx.RemoveUnused(Matches, maxSave: maxSave));
    }
}
