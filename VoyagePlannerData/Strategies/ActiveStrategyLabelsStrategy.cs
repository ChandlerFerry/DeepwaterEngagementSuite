namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class ActiveStrategyLabelsStrategy : IVoyageStrategy
{
    public string Id => "ActiveStrategyLabels";
    public int Order => StrategyOrders.ActiveLabels;

    public bool IsEnabled(VoyageStrategyOptions options) => true;

    public void Apply(PlacementContext ctx)
    {
        // Divine always surfaces when present (rare-monsters path is force-enabled for it).
        if (RareMonstersDropLockStrategy.ShouldRun(ctx))
        {
            if (ctx.DivineCenters.Count > 0)
                ctx.ActiveStrategies.Add("Divine");
            if (ctx.AnnulCenters.Count > 0)
                ctx.ActiveStrategies.Add("Annul");
            if (ctx.AncientCenters.Count > 0)
                ctx.ActiveStrategies.Add("Ancient");
        }

        if (ctx.PelagicLocked)
            ctx.ActiveStrategies.Add("Pelagic");
        if (ctx.AmuletCrossLocked)
            ctx.ActiveStrategies.Add("Amulet Hub");
        else if (ctx.PreferClamsAdjacentToAmulet)
            ctx.ActiveStrategies.Add("Amulet Soft");
        else if (ctx.AmuletCenterLocked)
            ctx.ActiveStrategies.Add("Amulet");
        if (ctx.NoConsumeActive)
            ctx.ActiveStrategies.Add("No-consume");
    }
}
