using System;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public abstract class SaveStrategyBase : IVoyageStrategy
{
    protected SaveStrategyBase(string id, string saveKey, int order)
    {
        Id = id;
        SaveKey = saveKey;
        Order = order;
    }

    public string Id { get; }
    public string SaveKey { get; }
    public int Order { get; }

    public bool IsEnabled(VoyageStrategyOptions options) => EffectiveMaxSave(options) > 0;

    /// <summary>Max charts to hold out; 0 disables. Clamped to <see cref="ChartIds.MaxSaveCap"/>.</summary>
    protected abstract int MaxSave(VoyageStrategyOptions options);

    protected int EffectiveMaxSave(VoyageStrategyOptions options) =>
        Math.Clamp(MaxSave(options), 0, ChartIds.MaxSaveCap);

    protected abstract bool Matches(MapPiece piece);

    /// <summary>Optional priority when choosing which charts to save under the cap.</summary>
    protected virtual Func<MapPiece, double> SaveScore => null;

    public virtual void Apply(PlacementContext ctx)
    {
        if (!IsEnabled(ctx.Options))
            return;

        ctx.AddSaved(SaveKey,
            ctx.RemoveUnused(Matches, SaveScore, maxSave: EffectiveMaxSave(ctx.Options)));
    }
}
