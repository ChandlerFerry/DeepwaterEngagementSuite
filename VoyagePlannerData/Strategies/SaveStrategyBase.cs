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

    public abstract bool IsEnabled(VoyageStrategyOptions options);
    protected abstract bool Matches(MapPiece piece);

    public void Apply(PlacementContext ctx)
    {
        if (!IsEnabled(ctx.Options))
            return;
        ctx.AddSaved(SaveKey, ctx.SaveByPredicate(Matches));
    }
}
