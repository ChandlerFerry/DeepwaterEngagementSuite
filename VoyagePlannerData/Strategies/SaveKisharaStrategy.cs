using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public sealed class SaveKisharaStrategy()
    : SaveStrategyBase("SaveKishara", SaveCountKeys.Kishara, StrategyOrders.SaveKishara)
{
    protected override int MaxSave(VoyageStrategyOptions options) => options.SaveKishara;
    protected override bool Matches(MapPiece piece) => ChartPredicates.IsKishara(piece);

    /// <summary>
    /// Always active: either holds Kishara out when saving, or caps placeable count to one when not.
    /// </summary>
    public override bool IsEnabled(VoyageStrategyOptions options) => true;

    public override void Apply(PlacementContext ctx)
    {
        var maxSave = EffectiveMaxSave(ctx.Options);
        if (maxSave > 0)
        {
            ctx.AddSaved(SaveKey,
                ctx.RemoveUnused(Matches, SaveScore, maxSave: maxSave, force: true));
            return;
        }

        // Not saving: only one Kishara's Rest may be placed in the voyage.
        var candidates = ctx.Working
            .Where(p => !ctx.UsedPieceIds.Contains(p.Id) && Matches(p))
            .OrderByDescending(p => p.LocalModifier + p.GlobalModifier)
            .ThenBy(p => p.Id)
            .ToList();

        var keep = ChartIds.MaxPlacedKisharaWhenNotSaving;
        if (candidates.Count <= keep)
            return;

        // Keep the best chart(s) for the solver; hold the rest out of placement.
        var held = 0;
        foreach (var piece in candidates.Skip(keep))
        {
            if (!ctx.TrySavePiece(piece.Id, force: true))
                break;
            held++;
        }

        ctx.AddSaved(SaveKey, held);
    }
}
