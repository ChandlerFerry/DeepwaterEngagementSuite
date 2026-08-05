using System;
using System.Collections.Generic;
using System.Linq;
using static DeepwaterEngagementSuite.VoyagePlannerData.Strategies.SupportPlacement;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class RareMonstersDropLockStrategy : IVoyageStrategy
{
    public string Id => "RareMonstersDrop.Lock";
    public int Order => StrategyOrders.RareMonstersLock;

    /// <summary>
    /// Enabled by setting, or forced whenever a Divine rare-drop orb is on the board
    /// so that strategy is never skipped for the most valuable orb type.
    /// </summary>
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
            else if (savedPelagic < ChartIds.MaxSavedPelagic && ctx.TrySavePiece(pelagic.Id))
            {
                savedPelagic++;
            }
        }

        ctx.AddSaved(SaveCountKeys.Pelagic, savedPelagic);

        var boxPools = new[]
        {
            new SupportPool(ChartPredicates.IsStrongboxCountChart, ChartPredicates.BoxValue1Score),
            new SupportPool(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore),
            new SupportPool(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore),
        };

        // Annul/Ancient use strongbox ranks 4–6 only (second set of three). Ranks 1–3 stay
        // free for the rare-monsters save; charts below rank 6 are not pulled onto the orb.
        // Fall back to starfish / rare combo when the second set is empty or exhausted.
        var secondSetBoxIds = StrongboxIdsByRank(ctx, skip: 3, take: 3);
        var secondSetBoxPools = new[]
        {
            new SupportPool(
                p => ChartPredicates.IsStrongboxCountChart(p) && secondSetBoxIds.Contains(p.Id),
                ChartPredicates.BoxValue1Score),
            new SupportPool(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore),
            new SupportPool(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore),
        };

        foreach (var center in ctx.DivineCenters)
            LockSupportsAround(ctx, center, boxPools, "Divine", LockPriorities.DivineSupport);

        foreach (var center in ctx.AnnulCenters)
            LockSupportsAround(ctx, center, secondSetBoxPools, "Annul", LockPriorities.AnnulSupport);

        foreach (var center in ctx.AncientCenters)
            LockSupportsAround(ctx, center, secondSetBoxPools, "Ancient", LockPriorities.AncientSupport);

        if (ctx.DivineCenters.Count > 0)
        {
            // NOTE: this fills every remaining free cell with a hard lock, which leaves the
            // solver almost no freedom. VoyageSolve's lock-dropping retry is what keeps this
            // from returning zero solutions on a shape-poor board — fill locks use the lowest
            // Divine priority so they are dropped before Pelagic/support Divine locks.
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

    /// <summary>
    /// Strongbox chart ids by descending value, sliced with <paramref name="skip"/>/<paramref name="take"/>
    /// (e.g. skip 3 take 3 → ranks 4–6).
    /// </summary>
    private static HashSet<int> StrongboxIdsByRank(PlacementContext ctx, int skip, int take) =>
        ctx.Working
            .Where(p => !ctx.UsedPieceIds.Contains(p.Id) && ChartPredicates.IsStrongboxCountChart(p))
            .OrderByDescending(ChartPredicates.BoxValue1Score)
            .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
            .Skip(skip)
            .Take(take)
            .Select(p => p.Id)
            .ToHashSet();
}

/// <summary>A candidate source of support charts, tried in order until one yields a piece.</summary>
internal readonly record struct SupportPool(Func<MapPiece, bool> Pred, Func<MapPiece, double> Score);

internal static class SupportPlacement
{
    /// <summary>
    /// Locks support charts onto the free orthogonal neighbours of an orb centre.
    ///
    /// Which charts get chosen is unchanged: pools are drained best-first by their own score.
    /// What changed is where each one lands. Previously charts were zipped onto cells in
    /// <see cref="ChartIds.Ortho"/> order, so the lowest-scoring chart got whatever cell came
    /// last — frequently the grid centre. A one-connection chart in the centre eliminates
    /// almost every valid topology, and combined with two straights it can eliminate all of
    /// them, leaving the solver with nothing.
    ///
    /// So we pair the most connected chart with the most constrained cell. This helps twice:
    /// dead ends become cheap (a corner only has two in-grid directions), and the strongest
    /// chart lands where its adjacency bonus reaches the most tiles.
    /// </summary>
    public static void LockSupportsAround(
        PlacementContext ctx,
        (int Row, int Col) center,
        IReadOnlyList<SupportPool> pools,
        string strategy,
        int priority)
    {
        // Materialise before locking: FreeNeighbors is lazy and reads LockedCells as it goes.
        var cells = ctx.FreeNeighbors(center.Row, center.Col).ToList();
        if (cells.Count == 0)
            return;

        // TakeBest only excludes pieces already in UsedPieceIds, and we have not locked
        // anything yet, so track picks locally to avoid drawing the same chart twice.
        var picked = new HashSet<int>();
        var supports = new List<MapPiece>();
        for (var slot = 0; slot < cells.Count; slot++)
        {
            MapPiece support = null;
            foreach (var pool in pools)
            {
                support = ctx.TakeBest(p => !picked.Contains(p.Id) && pool.Pred(p), pool.Score);
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

        // Rank preserves the value ordering the pools produced, so it breaks ties between
        // charts of equal shape without ever overriding shape itself.
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

        // Shared budget: strongbox > starfish > raremonsters2 (adjacent rare T2).
        // e.g. 3 boxes → 3 starfish/rare2; 4 boxes → 2; 6 boxes → 0.
        var savedBoxes = ctx.RemoveUnused(ChartPredicates.IsStrongboxCountChart, ChartPredicates.BoxValue1Score,
            maxSave: ChartIds.MaxSavedBoxes);
        ctx.AddSaved(SaveCountKeys.Strongbox, savedBoxes);

        var residual = Math.Max(0, ChartIds.MaxSavedRareMonsterSupport - savedBoxes);
        var savedStarfish = residual > 0
            ? ctx.RemoveUnused(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore,
                maxSave: residual)
            : 0;
        ctx.AddSaved(SaveCountKeys.Starfish, savedStarfish);

        var rareSlots = residual - savedStarfish;
        if (rareSlots > 0)
        {
            ctx.AddSaved(SaveCountKeys.AdjacentRare,
                ctx.RemoveUnused(ChartPredicates.IsAdjacentRareSaveChart, ChartPredicates.AdjacentRareScore,
                    maxSave: rareSlots));
        }

        ctx.AddSaved(SaveCountKeys.RareVoyage,
            ctx.RemoveUnused(ChartPredicates.IsRareVoyageChart, ChartPredicates.RareVoyageScore,
                maxSave: ChartIds.MaxSavedRareVoyage));
    }
}
