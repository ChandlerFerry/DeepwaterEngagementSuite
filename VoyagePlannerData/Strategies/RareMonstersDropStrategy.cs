using System;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

/// <summary>
/// Lock Pelagic on orb tiles, fill orb neighbors with support charts, divine global rare fill,
/// then hold unused boxes/starfish/adj-rare/voyage rares.
/// </summary>
public sealed class RareMonstersDropLockStrategy : IVoyageStrategy
{
    public string Id => "RareMonstersDrop.Lock";
    public int Order => StrategyOrders.RareMonstersLock;

    public bool IsEnabled(VoyageStrategyOptions options) => options.RareMonstersDrop;

    public void Apply(PlacementContext ctx)
    {
        var savedPelagic = 0;
        foreach (var pelagic in ctx.Working.Where(ChartPredicates.IsPelagic)
                     .OrderByDescending(p => p.LocalModifier + p.GlobalModifier).ToList())
        {
            if (ctx.UsedPieceIds.Contains(pelagic.Id))
                continue;

            var target = ctx.OrbCenters.FirstOrDefault(c => ctx.CellFree(c.Row, c.Col));
            if (target.Priority > 0)
            {
                ctx.LockCell(target.Row, target.Col, pelagic);
                ctx.OrbCenters.RemoveAll(c => c.Row == target.Row && c.Col == target.Col);
                ctx.PelagicLocked = true;
            }
            else if (savedPelagic < ChartIds.MaxSavedPelagic && ctx.TrySavePiece(pelagic.Id))
            {
                savedPelagic++;
            }
        }

        ctx.AddSaved(SaveCountKeys.Pelagic, savedPelagic);

        foreach (var center in ctx.DivineCenters)
        {
            foreach (var n in ctx.FreeNeighbors(center.Row, center.Col))
            {
                var support = ctx.TakeBest(ChartPredicates.IsStrongboxCountChart, ChartPredicates.BoxValue1Score)
                              ?? ctx.TakeBest(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore)
                              ?? ctx.TakeBest(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore);
                if (support == null)
                    break;
                ctx.LockCell(n.Row, n.Col, support);
            }
        }

        foreach (var center in ctx.AnnulCenters)
        {
            foreach (var n in ctx.FreeNeighbors(center.Row, center.Col))
            {
                var support = ctx.TakeBest(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore)
                              ?? ctx.TakeBest(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore);
                if (support == null)
                    break;
                ctx.LockCell(n.Row, n.Col, support);
            }
        }

        foreach (var center in ctx.AncientCenters)
        {
            foreach (var n in ctx.FreeNeighbors(center.Row, center.Col))
            {
                var support = ctx.TakeBest(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore)
                              ?? ctx.TakeBest(ChartPredicates.IsOrbRareComboChart, ChartPredicates.OrbRareComboScore);
                if (support == null)
                    break;
                ctx.LockCell(n.Row, n.Col, support);
            }
        }

        if (ctx.DivineCenters.Count > 0)
        {
            foreach (var cell in ChartPredicates.EnumerateCells().Where(c => ctx.CellFree(c.Row, c.Col)))
            {
                var rare = ctx.TakeBest(ChartPredicates.IsOrbRareGlobalChart, ChartPredicates.OrbRareComboScore);
                if (rare == null)
                    break;
                ctx.LockCell(cell.Row, cell.Col, rare);
            }
        }
    }
}

public sealed class RareMonstersDropSaveStrategy : IVoyageStrategy
{
    public string Id => "RareMonstersDrop.Save";
    public int Order => StrategyOrders.RareMonstersSave;

    public bool IsEnabled(VoyageStrategyOptions options) => options.RareMonstersDrop;

    public void Apply(PlacementContext ctx)
    {
        ctx.AddSaved(SaveCountKeys.Strongbox,
            ctx.RemoveUnused(ChartPredicates.IsStrongboxCountChart, ChartPredicates.BoxValue1Score,
                maxSave: ChartIds.MaxSavedBoxes));
        var savedStarfish = ctx.RemoveUnused(ChartPredicates.IsStarfishChart, ChartPredicates.StarfishScore,
            maxSave: ChartIds.MaxSavedStarfish);
        ctx.AddSaved(SaveCountKeys.Starfish, savedStarfish);

        var supportSlotsLeft = Math.Max(0, ChartIds.MaxSavedStarfish - savedStarfish);
        if (supportSlotsLeft > 0)
        {
            ctx.AddSaved(SaveCountKeys.AdjacentRare,
                ctx.RemoveUnused(ChartPredicates.IsAdjacentRareSaveChart, ChartPredicates.AdjacentRareScore,
                    maxSave: supportSlotsLeft));
        }

        ctx.AddSaved(SaveCountKeys.RareVoyage,
            ctx.RemoveUnused(ChartPredicates.IsRareVoyageChart, ChartPredicates.RareVoyageScore,
                maxSave: ChartIds.MaxSavedRareVoyage));
    }
}
