using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Xunit;

namespace DeepwaterEngagementSuite.Tests;

public class VoyagePlacementRulesApplyTests
{
    private static readonly Modifier GoldenLanternMod =
        new(ChartIds.AdjacentGoldenLanternsPrefix, Weight: 95, Tags: ModifierTag.Lanterns);

    private static readonly Modifier PantheonMod =
        new(ChartIds.AdjacentPantheonPrefix, Weight: 90, Tags: ModifierTag.None);

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

    private static MapPiece Piece(
        int id,
        PieceType type,
        Direction connections,
        string name = "Chart",
        params Modifier[] mods) =>
        new(id, type, connections, mods.ToList(), name);

    private static List<MapPiece> TenPiecesWithGoldenLantern(int goldenId = 99)
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 9; i++)
            pieces.Add(Piece(i, $"Filler{i}"));
        pieces.Add(Piece(goldenId, "Golden Chart", GoldenLanternMod));
        return pieces;
    }

    private static List<MapPiece> Fillers(int count, int idStart = 0)
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < count; i++)
            pieces.Add(Piece(idStart + i, $"Filler{idStart + i}"));
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
    public void Apply_caps_GoldenLanterns_at_default_max_of_four()
    {
        var pieces = Fillers(9);
        // 6 golden lanterns of the same low-priority shape
        for (var i = 0; i < 6; i++)
            pieces.Add(Piece(100 + i, PieceType.Cross, Direction.All, $"Golden{i}", GoldenLanternMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SaveGoldenLanterns = true,
            MaxSavedGoldenLanterns = ChartIds.MaxSavedGoldenLanterns
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(4, result.SavedGoldenLanternsCount);
        Assert.Equal(2, result.Pieces.Count(ChartPredicates.IsGoldenLanternsChart));
    }

    [Fact]
    public void Apply_respects_adjustable_GoldenLanterns_max()
    {
        var pieces = Fillers(9);
        for (var i = 0; i < 5; i++)
            pieces.Add(Piece(100 + i, PieceType.Cross, Direction.All, $"Golden{i}", GoldenLanternMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SaveGoldenLanterns = true,
            MaxSavedGoldenLanterns = 2
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(2, result.SavedGoldenLanternsCount);
        Assert.Equal(3, result.Pieces.Count(ChartPredicates.IsGoldenLanternsChart));
    }

    [Fact]
    public void Apply_prefers_saving_Tee_GoldenLanterns_over_dead_end_long_cross()
    {
        var pieces = Fillers(9);
        pieces.Add(Piece(200, PieceType.Cross, Direction.All, "GoldenCross", GoldenLanternMod));
        pieces.Add(Piece(201, PieceType.Straight, Direction.Up | Direction.Down, "GoldenLong", GoldenLanternMod));
        pieces.Add(Piece(202, PieceType.Single, Direction.Up, "GoldenDeadEnd", GoldenLanternMod));
        pieces.Add(Piece(203, PieceType.Tee, Direction.Up | Direction.Left | Direction.Right, "GoldenTee", GoldenLanternMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SaveGoldenLanterns = true,
            MaxSavedGoldenLanterns = 1
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(1, result.SavedGoldenLanternsCount);
        Assert.DoesNotContain(result.Pieces, p => p.Id == 203); // Tee saved
        Assert.Contains(result.Pieces, p => p.Id == 200); // Cross kept for solver
        Assert.Contains(result.Pieces, p => p.Id == 201); // Long kept
        Assert.Contains(result.Pieces, p => p.Id == 202); // Dead end kept (lowest shape priority)
    }

    [Fact]
    public void Apply_caps_Pantheon_at_default_max_of_two()
    {
        var pieces = Fillers(9);
        for (var i = 0; i < 5; i++)
            pieces.Add(Piece(100 + i, $"Pantheon{i}", PantheonMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SavePantheon = true,
            MaxSavedPantheon = ChartIds.MaxSavedPantheon
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(2, result.SavedPantheonCount);
        Assert.Equal(3, result.Pieces.Count(ChartPredicates.IsPantheonChart));
    }

    [Fact]
    public void Apply_respects_adjustable_Pantheon_max()
    {
        var pieces = Fillers(9);
        for (var i = 0; i < 4; i++)
            pieces.Add(Piece(100 + i, $"Pantheon{i}", PantheonMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SavePantheon = true,
            MaxSavedPantheon = 1
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(1, result.SavedPantheonCount);
        Assert.Equal(3, result.Pieces.Count(ChartPredicates.IsPantheonChart));
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
