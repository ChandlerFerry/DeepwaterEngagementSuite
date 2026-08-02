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
            if (!strategy.IsEnabled(ctx.Options))
                continue;
            strategy.Apply(ctx);
        }

        return ctx;
    }
}
