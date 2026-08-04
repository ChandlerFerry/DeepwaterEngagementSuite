namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class CenterSpecialtyLockStrategy : IVoyageStrategy
{
    public string Id => "CenterSpecialty.Lock";
    public int Order => StrategyOrders.CenterSpecialtyLock;

    public bool IsEnabled(VoyageStrategyOptions options) => options.CenterSpecialty;

    public void Apply(PlacementContext ctx)
    {
        if (!ctx.CellFree(ChartIds.CenterRow, ChartIds.CenterCol))
            return;

        var centerPiece = ctx.TakeBest(ChartPredicates.IsOperativeBoxChart, ChartPredicates.OperativeBoxScore)
                          ?? ctx.TakeBest(ChartPredicates.IsLostMessageChart, ChartPredicates.LostMessageScore)
                          ?? ctx.TakeBest(ChartPredicates.IsUniqueAmulet1Chart, ChartPredicates.UniqueAmuletScore)
                          ?? ctx.TakeBest(ChartPredicates.IsUniqueBeltChart, ChartPredicates.UniqueBeltScore)
                          ?? ctx.TakeBest(ChartPredicates.IsUniqueRingChart, ChartPredicates.UniqueRingScore);
        if (centerPiece != null)
            ctx.LockCell(ChartIds.CenterRow, ChartIds.CenterCol, centerPiece,
                strategy: "Center Specialty", priority: LockPriorities.CenterSpecialty);
    }
}

public sealed class CenterSpecialtySaveStrategy : IVoyageStrategy
{
    public string Id => "CenterSpecialty.Save";
    public int Order => StrategyOrders.CenterSpecialtySave;

    public bool IsEnabled(VoyageStrategyOptions options) => options.CenterSpecialty;

    public void Apply(PlacementContext ctx)
    {
        ctx.AddSaved(SaveCountKeys.OperativeBox,
            ctx.RemoveUnused(ChartPredicates.IsOperativeBoxChart));
        ctx.AddSaved(SaveCountKeys.LostMessage,
            ctx.RemoveUnused(ChartPredicates.IsLostMessageChart));
    }
}
