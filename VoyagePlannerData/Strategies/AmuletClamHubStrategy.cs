using System;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class AmuletClamHubStrategy : IVoyageStrategy
{
    public string Id => "UniqueAmuletClamCross";
    public int Order => StrategyOrders.AmuletLock;

    public bool IsEnabled(VoyageStrategyOptions options) => true;

    public void Apply(PlacementContext ctx)
    {
        if (!ctx.CellFree(ChartIds.CenterRow, ChartIds.CenterCol))
            return;

        if (ctx.Options.UniqueAmuletClamCross && !ctx.StrongTreasure && !ctx.HasOrbs)
        {
            ctx.AmuletCrossLocked = TryLockAmuletClamHub(ctx);
        }
        else if (!ctx.Options.UniqueAmuletClamCross)
        {
            ctx.PreferClamsAdjacentToAmulet = TryLockUniqueAmulet2Center(ctx);
            ctx.AmuletCenterLocked = ctx.PreferClamsAdjacentToAmulet;
        }
    }

    private static bool TryLockUniqueAmulet2Center(PlacementContext ctx)
    {
        var amulet2 = ctx.TakeBest(ChartPredicates.IsUniqueAmulet2Chart, ChartPredicates.UniqueAmuletScore);
        if (amulet2 == null)
            return false;
        ctx.LockCell(ChartIds.CenterRow, ChartIds.CenterCol, amulet2);
        return true;
    }

    private static bool TryLockAmuletClamHub(PlacementContext ctx)
    {
        var amulet2 = ctx.TakeBest(ChartPredicates.IsUniqueAmulet2Chart, ChartPredicates.UniqueAmuletScore);
        if (amulet2 == null)
            return false;

        var clamCount = ChartPredicates.ClamHubCountForAmulet(amulet2);
        if (clamCount <= 0)
            return false;

        var freeOrtho = ctx.FreeNeighbors(ChartIds.CenterRow, ChartIds.CenterCol).ToList();
        if (freeOrtho.Count < clamCount)
            return false;

        var clamSlots = freeOrtho
            .OrderBy(c => c.Row == ChartIds.CenterRow - 1 && c.Col == ChartIds.CenterCol ? 1 : 0)
            .Take(clamCount)
            .ToList();

        var clams = ctx.Working
            .Where(p => !ctx.UsedPieceIds.Contains(p.Id) && ChartPredicates.IsClamChart(p))
            .OrderByDescending(ChartPredicates.ClamScore)
            .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
            .Take(clamCount)
            .ToList();
        if (clams.Count < clamCount)
            return false;

        ctx.LockCell(ChartIds.CenterRow, ChartIds.CenterCol, amulet2);
        for (var i = 0; i < clamCount; i++)
            ctx.LockCell(clamSlots[i].Row, clamSlots[i].Col, clams[i]);
        return true;
    }
}

public sealed class AmuletClamSaveStrategy : IVoyageStrategy
{
    public string Id => "UniqueAmuletClamCross.Save";
    public int Order => StrategyOrders.AmuletSave;

    public bool IsEnabled(VoyageStrategyOptions options) => true;

    public void Apply(PlacementContext ctx)
    {
        if (ctx.Options.UniqueAmuletClamCross && !ctx.AmuletCrossLocked)
        {
            ctx.AddSaved(SaveCountKeys.UniqueAmulet,
                ctx.RemoveUnused(ChartPredicates.IsUniqueAmulet2Chart, ChartPredicates.UniqueAmuletScore,
                    maxSave: ChartIds.MaxSavedUniqueAmulet2, force: true));
            ctx.AddSaved(SaveCountKeys.Clam,
                ctx.RemoveUnused(ChartPredicates.IsClamChart, ChartPredicates.ClamScore,
                    maxSave: ChartIds.MaxSavedClamsForAmulet, force: true));
        }

        if (!ctx.SurplusClams)
            return;

        if (ctx.PreferClamsAdjacentToAmulet)
        {
            var freeOrtho = ctx.FreeNeighbors(ChartIds.CenterRow, ChartIds.CenterCol).Count();
            var keep = Math.Max(0, freeOrtho);
            var clamCandidates = ctx.Working
                .Where(p => !ctx.UsedPieceIds.Contains(p.Id) && ChartPredicates.IsClamChart(p))
                .OrderByDescending(ChartPredicates.ClamScore)
                .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
                .Select(p => p.Id)
                .ToList();
            var saved = 0;
            foreach (var id in clamCandidates.Skip(keep))
            {
                if (!ctx.TrySavePiece(id, force: true))
                    break;
                saved++;
            }

            ctx.AddSaved(SaveCountKeys.Clam, saved);
        }
        else
        {
            ctx.AddSaved(SaveCountKeys.Clam,
                ctx.RemoveUnused(ChartPredicates.IsClamChart, ChartPredicates.ClamScore, force: true));
        }
    }
}
