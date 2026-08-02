using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Xunit;

namespace DeepwaterEngagementSuite.Tests;

public class VoyagePlacementRulesApplyTests
{
    private static IReadOnlyList<BorderEffect>[,] EmptyBorders()
    {
        var borders = new IReadOnlyList<BorderEffect>[3, 3];
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            borders[r, c] = [];
        return borders;
    }

    private static MapPiece Piece(int id, string name = "Chart", params Modifier[] mods) =>
        new(id, PieceType.Cross, Direction.All, mods.ToList(), name);

    private static List<MapPiece> TenPiecesWithGoldenLantern(int goldenId = 99)
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 9; i++)
            pieces.Add(Piece(i, $"Filler{i}"));
        pieces.Add(Piece(
            goldenId,
            "Golden Chart",
            new Modifier(ChartIds.AdjacentGoldenLanternsPrefix, Weight: 95, Tags: ModifierTag.Lanterns)));
        return pieces;
    }

    [Fact]
    public void Apply_saves_GoldenLanterns_when_enabled_and_surplus_pieces()
    {
        var pieces = TenPiecesWithGoldenLantern();
        var options = VoyageStrategyOptions.AllEnabled with { SaveGoldenLanterns = true };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.True(result.SavedGoldenLanternsCount >= 1,
            $"Expected golden lanterns saved, got {result.SavedGoldenLanternsCount}");
        Assert.DoesNotContain(result.Pieces, p => p.Id == 99);
        Assert.NotNull(result.Locks);
        Assert.NotNull(result.ActiveStrategies);
    }

    [Fact]
    public void Apply_keeps_GoldenLanterns_when_save_disabled()
    {
        var pieces = TenPiecesWithGoldenLantern();
        var options = VoyageStrategyOptions.AllEnabled with { SaveGoldenLanterns = false };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(0, result.SavedGoldenLanternsCount);
        Assert.Contains(result.Pieces, p => p.Id == 99);
    }

    [Fact]
    public void Apply_pipeline_labels_orbs_when_RareMonstersDrop_enabled()
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 10; i++)
            pieces.Add(Piece(i, i == 0 ? ChartIds.PelagicRoomName : $"Filler{i}"));

        var borders = EmptyBorders();
        borders[0, 0] =
        [
            new BorderEffect(ChartIds.RareDivine, ModifierTag.All, 1, false, false)
        ];

        var result = VoyagePlacementRules.Apply(pieces, borders, VoyageStrategyOptions.AllEnabled);

        Assert.Contains("Divine", result.ActiveStrategies);
        Assert.Contains(result.Locks, lp => lp.PieceId == 0 && lp.Row == 0 && lp.Col == 0);
    }
}
