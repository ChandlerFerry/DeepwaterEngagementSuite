using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

/// <summary>
/// Always runs: hold extra center-only belt/ring charts so the solver only keeps one candidate
/// when center is still free and not waiting for Unique Amulet2.
/// </summary>
public sealed class CenterOnlyJewelrySaveStrategy : IVoyageStrategy
{
    public string Id => "CenterOnlyJewelrySave";
    public int Order => StrategyOrders.CenterOnlyJewelrySave;

    public bool IsEnabled(VoyageStrategyOptions options) => true;

    public void Apply(PlacementContext ctx)
    {
        var centerTakenByCenterOnly = ctx.Locks.Any(lp =>
            lp.Row == ChartIds.CenterRow &&
            lp.Col == ChartIds.CenterCol &&
            ctx.OriginalPieces.FirstOrDefault(p => p.Id == lp.PieceId) is { } locked &&
            ChartPredicates.IsCenterOnlyUniqueChart(locked));
        var amulet2Waiting = ctx.Working.Any(p =>
            !ctx.UsedPieceIds.Contains(p.Id) && ChartPredicates.IsUniqueAmulet2Chart(p));
        var keepBeltRing = ctx.CellFree(ChartIds.CenterRow, ChartIds.CenterCol) &&
                           !centerTakenByCenterOnly &&
                           !amulet2Waiting
            ? 1
            : 0;

        var savedBelt = 0;
        var savedRing = 0;
        foreach (var piece in ctx.Working
                     .Where(p => !ctx.UsedPieceIds.Contains(p.Id) &&
                                 (ChartPredicates.IsUniqueBeltChart(p) || ChartPredicates.IsUniqueRingChart(p)))
                     .OrderByDescending(ChartPredicates.CenterOnlyUniqueScore)
                     .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
                     .Skip(keepBeltRing)
                     .ToList())
        {
            if (!ctx.TrySavePiece(piece.Id, force: true))
                break;
            if (ChartPredicates.IsUniqueBeltChart(piece))
                savedBelt++;
            else
                savedRing++;
        }

        ctx.AddSaved(SaveCountKeys.UniqueBelt, savedBelt);
        ctx.AddSaved(SaveCountKeys.UniqueRing, savedRing);
    }
}
