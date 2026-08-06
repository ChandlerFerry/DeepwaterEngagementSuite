namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class ActiveStrategyLabelsStrategy : IVoyageStrategy
{
    public string Id => "ActiveStrategyLabels";
    public int Order => StrategyOrders.ActiveLabels;

    public bool IsEnabled(VoyageStrategyOptions options) => true;

    public void Apply(PlacementContext ctx)
    {
        
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
        if (ctx.NoConsumeActive)
            ctx.ActiveStrategies.Add("No-consume");
    }
}
