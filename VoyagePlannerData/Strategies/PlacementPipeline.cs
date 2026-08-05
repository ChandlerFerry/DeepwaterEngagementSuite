using System.Collections.Generic;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData.Strategies;

public static class PlacementPipeline
{
    private static readonly IVoyageStrategy[] Strategies =
    [
        new SaveKisharaStrategy(),
        new SaveNoEquipmentStrategy(),
        new SaveFracturedStrategy(),
        new SaveGoldenLanternsStrategy(),
        new SavePantheonStrategy(),
        new SaveSoulEaterStrategy(),
        new SaveRareFractureStrategy(),
        new SaveRarePossessedStrategy(),
        // Late among pure saves so rare-monsters residual can claim starfish first.
        // Order 1050 still runs after jewelry; listed here with other save strategies.
        new SaveStarfishStrategy(),

        new AmuletClamHubStrategy(),
        new RareMonstersDropLockStrategy(),
        new CenterSpecialtyLockStrategy(),
        new NoConsumeFarmLockStrategy(),

        new NoConsumeFarmSaveStrategy(),
        new RareMonstersDropSaveStrategy(),
        new CenterSpecialtySaveStrategy(),
        new AmuletClamSaveStrategy(),
        new CenterOnlyJewelrySaveStrategy(),

        new ActiveStrategyLabelsStrategy(),
    ];

    public static IReadOnlyList<IVoyageStrategy> All => Strategies;

    public static PlacementContext Run(
        IReadOnlyList<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders,
        VoyageStrategyOptions options = null)
    {
        var ctx = new PlacementContext(pieces, tileBorders, options);
        foreach (var strategy in Strategies.OrderBy(s => s.Order).ThenBy(s => s.Id))
        {
            if (!IsStrategyEnabled(strategy, ctx))
                continue;
            strategy.Apply(ctx);
        }

        return ctx;
    }

    /// <summary>
    /// Normal option gates, plus force-run rare-monsters lock/save when a Divine orb is
    /// present so that strategy is never skipped for the highest-value orb.
    /// </summary>
    private static bool IsStrategyEnabled(IVoyageStrategy strategy, PlacementContext ctx)
    {
        if (strategy.IsEnabled(ctx.Options))
            return true;

        if (ctx.DivineCenters.Count == 0)
            return false;

        return strategy is RareMonstersDropLockStrategy or RareMonstersDropSaveStrategy;
    }
}
