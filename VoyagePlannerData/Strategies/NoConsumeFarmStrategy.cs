using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class NoConsumeFarmLockStrategy : IVoyageStrategy
{
    public string Id => "NoConsumeAnchorfield.Lock";
    public int Order => StrategyOrders.NoConsumeLock;

    public bool IsEnabled(VoyageStrategyOptions options) => options.NoConsumeAnchorfield;

    public void Apply(PlacementContext ctx)
    {
        if (ctx.StrongTreasure || ctx.HasOrbs)
            return;

        foreach (var cell in ChartPredicates.EnumerateCells().Where(c =>
                     ctx.CellFree(c.Row, c.Col) &&
                     ChartPredicates.IsStrongNoConsume(ctx.BordersAt(c.Row, c.Col))))
        {
            var farm = ctx.TakeBest(ChartPredicates.IsSoulEaterChart, ChartPredicates.SoulEaterScore)
                       ?? ctx.TakeBest(ChartPredicates.IsAnchorfieldChart, ChartPredicates.FarmPriority)
                       ?? ctx.TakeBest(ChartPredicates.IsClamChart, ChartPredicates.ClamScore);
            if (farm == null)
                break;
            ctx.LockCell(cell.Row, cell.Col, farm,
                strategy: "No-consume", priority: LockPriorities.NoConsume);
            ctx.NoConsumeActive = true;
        }
    }
}

public sealed class NoConsumeFarmSaveStrategy : IVoyageStrategy
{
    public string Id => "NoConsumeAnchorfield.Save";
    public int Order => StrategyOrders.NoConsumeSave;

    public bool IsEnabled(VoyageStrategyOptions options) => options.NoConsumeAnchorfield;

    public void Apply(PlacementContext ctx)
    {
        ctx.AddSaved(SaveCountKeys.Farm,
            ctx.RemoveUnused(ChartPredicates.IsAnchorfieldChart, ChartPredicates.FarmPriority));
    }
}
