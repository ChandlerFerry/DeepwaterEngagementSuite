using System;
using System.Collections.Generic;
using System.Linq;
using static DeepwaterEngagementSuite.VoyagePlannerData.Strategies.SupportPlacement;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class RareMonstersDropLockStrategy : IVoyageStrategy
{
    public string Id => "RareMonstersDrop.Lock";
    public int Order => StrategyOrders.RareMonstersLock;

    public bool IsEnabled(VoyageStrategyOptions options) => options.RareMonstersDrop;

    public static bool ShouldRun(PlacementContext ctx) =>
        ctx.Options.RareMonstersDrop || ctx.DivineCenters.Count > 0;

    public void Apply(PlacementContext ctx)
    {
        if (!ShouldRun(ctx))
            return;

        var savedPelagic = 0;
        foreach (var pelagic in ctx.Working.Where(ChartPredicates.IsPelagic)
                     .OrderByDescending(p => p.LocalModifier + p.GlobalModifier).ToList())
        {
            if (ctx.UsedPieceIds.Contains(pelagic.Id))
                continue;

            var target = ctx.OrbCenters.FirstOrDefault(c => ctx.CellFree(c.Row, c.Col));
            if (target.Priority > 0)
            {
                var (strategy, priority) = OrbLockMeta(target.Priority, isPelagic: true);
                ctx.LockCell(target.Row, target.Col, pelagic, strategy: strategy, priority: priority);
                ctx.OrbCenters.RemoveAll(c => c.Row == target.Row && c.Col == target.Col);
                ctx.PelagicLocked = true;
            }
            else if (savedPelagic < ChartIds.MaxSavedPelagic && ctx.TrySavePiece(pelagic.Id, force: true))
            {
                savedPelagic++;
            }
        }

        ctx.AddSaved(SaveCountKeys.Pelagic, savedPelagic);

        // Hold top strongbox charts before any non-Divine placement. Only Divine may spend
        // these reservations (allowReserved). Annul/Ancient/solver never touch them.
        ctx.ReserveUnused(
            ChartPredicates.IsStrongboxCountChart,
            ChartPredicates.BoxValue1Score,
            maxReserve: ChartIds.MaxSavedBoxes);

        // Divine is explicitly allowed to consume reserved strongboxes (incl. value1=5).
        var divinePools = new[]
        {
            new SupportPool(ChartPredicates.IsStrongboxCountChart, ChartPredicates.BoxValue1Score),
            new SupportPool(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore),
            new SupportPool(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore),
        };

        // Annul/Ancient never burn reserved boxes — starfish / rare combo only.
        var nonDivinePools = new[]
        {
            new SupportPool(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore),
            new SupportPool(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore),
        };

        foreach (var center in ctx.DivineCenters)
            LockSupportsAround(ctx, center, divinePools, "Divine", LockPriorities.DivineSupport,
                allowReserved: true);

        foreach (var center in ctx.AnnulCenters)
            LockSupportsAround(ctx, center, nonDivinePools, "Annul", LockPriorities.AnnulSupport);

        foreach (var center in ctx.AncientCenters)
            LockSupportsAround(ctx, center, nonDivinePools, "Ancient", LockPriorities.AncientSupport);

        if (ctx.DivineCenters.Count > 0)
        {
            foreach (var cell in ChartPredicates.EnumerateCells().Where(c => ctx.CellFree(c.Row, c.Col)))
            {
                var rare = ctx.TakeBest(ChartPredicates.IsOrbRareGlobalChart, ChartPredicates.OrbRareComboScore);
                if (rare == null)
                    break;
                ctx.LockCell(cell.Row, cell.Col, rare,
                    strategy: "Divine fill", priority: LockPriorities.DivineFill);
            }
        }
    }

    private static (string Strategy, int Priority) OrbLockMeta(int orbPriority, bool isPelagic) =>
        orbPriority switch
        {
            3 => ("Divine", isPelagic ? LockPriorities.DivinePelagic : LockPriorities.DivineSupport),
            2 => ("Annul", isPelagic ? LockPriorities.AnnulPelagic : LockPriorities.AnnulSupport),
            1 => ("Ancient", isPelagic ? LockPriorities.AncientPelagic : LockPriorities.AncientSupport),
            _ => ("Rare Monsters", LockPriorities.Default),
        };
}


internal readonly record struct SupportPool(Func<MapPiece, bool> Pred, Func<MapPiece, double> Score);

internal static class SupportPlacement
{
    public static void LockSupportsAround(
        PlacementContext ctx,
        (int Row, int Col) center,
        IReadOnlyList<SupportPool> pools,
        string strategy,
        int priority,
        bool allowReserved = false)
    {
        var cells = ctx.FreeNeighbors(center.Row, center.Col).ToList();
        if (cells.Count == 0)
            return;

        var picked = new HashSet<int>();
        var supports = new List<MapPiece>();
        for (var slot = 0; slot < cells.Count; slot++)
        {
            MapPiece support = null;
            foreach (var pool in pools)
            {
                support = ctx.TakeBest(
                    p => !picked.Contains(p.Id) && pool.Pred(p),
                    pool.Score,
                    allowReserved: allowReserved);
                if (support != null)
                    break;
            }

            if (support == null)
                break;

            picked.Add(support.Id);
            supports.Add(support);
        }

        if (supports.Count == 0)
            return;

        var orderedCells = cells
            .OrderByDescending(c => ChartPredicates.InGridDegree(c.Row, c.Col))
            .ThenBy(c => c.Row)
            .ThenBy(c => c.Col)
            .ToList();

        var orderedSupports = supports
            .Select((piece, rank) => (Piece: piece, Rank: rank))
            .OrderByDescending(x => x.Piece.BaseConnections.CountConnections())
            .ThenBy(x => x.Rank)
            .Select(x => x.Piece)
            .ToList();

        for (var i = 0; i < orderedSupports.Count; i++)
            ctx.LockCell(orderedCells[i].Row, orderedCells[i].Col, orderedSupports[i],
                strategy: strategy, priority: priority);
    }
}

public sealed class RareMonstersDropSaveStrategy : IVoyageStrategy
{
    public string Id => "RareMonstersDrop.Save";
    public int Order => StrategyOrders.RareMonstersSave;

    public bool IsEnabled(VoyageStrategyOptions options) => options.RareMonstersDrop;

    public void Apply(PlacementContext ctx)
    {
        if (!RareMonstersDropLockStrategy.ShouldRun(ctx))
            return;

        // Force holds out of the solver pool. Floor-of-9 must not reintroduce reserved boxes
        // when only a short inventory page is loaded.
        var savedBoxes = ctx.RemoveUnused(
            ChartPredicates.IsStrongboxCountChart,
            ChartPredicates.BoxValue1Score,
            maxSave: ChartIds.MaxSavedBoxes,
            force: true);
        ctx.AddSaved(SaveCountKeys.Strongbox, savedBoxes);

        var residual = Math.Max(0, ChartIds.MaxSavedRareMonsterSupport - savedBoxes);
        var savedStarfish = residual > 0
            ? ctx.RemoveUnused(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore,
                maxSave: residual, force: true)
            : 0;
        ctx.AddSaved(SaveCountKeys.Starfish, savedStarfish);

        var rareSlots = residual - savedStarfish;
        if (rareSlots > 0)
        {
            ctx.AddSaved(SaveCountKeys.AdjacentRare,
                ctx.RemoveUnused(ChartPredicates.IsAdjacentRareSaveChart, ChartPredicates.AdjacentRareScore,
                    maxSave: rareSlots, force: true));
        }

        ctx.AddSaved(SaveCountKeys.RareVoyage,
            ctx.RemoveUnused(ChartPredicates.IsRareVoyageChart, ChartPredicates.RareVoyageScore,
                maxSave: ChartIds.MaxSavedRareVoyage, force: true));
    }
}
