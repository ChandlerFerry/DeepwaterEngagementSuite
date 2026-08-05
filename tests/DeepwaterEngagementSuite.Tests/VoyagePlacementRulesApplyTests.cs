using System;
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
        var options = VoyageStrategyOptions.AllEnabled with
        {
            SaveGoldenLanterns = ChartIds.MaxSavedGoldenLanterns
        };

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
        var options = VoyageStrategyOptions.AllEnabled with { SaveGoldenLanterns = 0 };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(0, result.SavedGoldenLanternsCount);
        Assert.Contains(result.Pieces, p => p.Id == 99);
    }

    [Fact]
    public void Apply_caps_GoldenLanterns_at_default_max_of_four()
    {
        var pieces = Fillers(9);
        
        for (var i = 0; i < 6; i++)
            pieces.Add(Piece(100 + i, PieceType.Cross, Direction.All, $"Golden{i}", GoldenLanternMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SaveGoldenLanterns = ChartIds.MaxSavedGoldenLanterns
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

        var options = VoyageStrategyOptions.AllEnabled with { SaveGoldenLanterns = 2 };

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

        var options = VoyageStrategyOptions.AllEnabled with { SaveGoldenLanterns = 1 };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(1, result.SavedGoldenLanternsCount);
        Assert.DoesNotContain(result.Pieces, p => p.Id == 203); 
        Assert.Contains(result.Pieces, p => p.Id == 200); 
        Assert.Contains(result.Pieces, p => p.Id == 201); 
        Assert.Contains(result.Pieces, p => p.Id == 202); 
    }

    [Fact]
    public void Apply_caps_Pantheon_at_default_max_of_two()
    {
        var pieces = Fillers(9);
        for (var i = 0; i < 5; i++)
            pieces.Add(Piece(100 + i, $"Pantheon{i}", PantheonMod));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            SavePantheon = ChartIds.MaxSavedPantheon
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

        var options = VoyageStrategyOptions.AllEnabled with { SavePantheon = 1 };

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
        Assert.Contains(result.Locks, lp => lp.Strategy == "Divine" && lp.Priority == LockPriorities.DivinePelagic);
    }

    [Fact]
    public void Apply_forces_Divine_rare_monsters_even_when_option_disabled()
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 10; i++)
            pieces.Add(Piece(i, i == 0 ? ChartIds.PelagicRoomName : $"Filler{i}"));

        var borders = EmptyBorders();
        borders[0, 0] =
        [
            new BorderEffect(ChartIds.RareDivine, ModifierTag.All, 1, false, false)
        ];

        var options = VoyageStrategyOptions.AllEnabled with { RareMonstersDrop = false };
        var result = VoyagePlacementRules.Apply(pieces, borders, options);

        Assert.Contains("Divine", result.ActiveStrategies);
        Assert.Contains(result.Locks, lp => lp.PieceId == 0 && lp.Strategy == "Divine");
    }

    private static Modifier Strongbox(int tier, int value1) =>
        new($"{ChartIds.AdjacentStrongboxesPrefix}{tier}", 10, false, ModifierTag.None, value1);

    private static Modifier Starfish(int tier, int value1) =>
        new($"{ChartIds.AdjacentStarfishPrefix}{tier}", 10, false, ModifierTag.Monsters, value1);

    private static Modifier RareMonsters2() =>
        new($"{ChartIds.AdjacentIncreasedRarePrefix}2", 10, false, ModifierTag.RareMonsters, 1);

    
    [Theory]
    [InlineData(3, 3)]
    [InlineData(4, 2)]
    [InlineData(6, 0)]
    public void RareMonsters_save_residual_is_six_minus_boxes(int boxCount, int expectedResidual)
    {
        var pieces = Fillers(12);
        for (var i = 0; i < boxCount; i++)
            pieces.Add(Piece(200 + i, $"Box{i}", Strongbox(2, 10 + i)));
        for (var i = 0; i < 5; i++)
            pieces.Add(Piece(300 + i, $"Star{i}", Starfish(2, 7)));
        for (var i = 0; i < 5; i++)
            pieces.Add(Piece(400 + i, $"Rare{i}", RareMonsters2()));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            RareMonstersDrop = true,
            SaveStarfish = 0,
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(boxCount, result.SavedStrongboxCount);
        Assert.Equal(expectedResidual,
            result.SavedStarfishCount + result.SavedAdjacentRareCount);
        
        Assert.Equal(Math.Min(5, expectedResidual), result.SavedStarfishCount);
        Assert.Equal(Math.Max(0, expectedResidual - Math.Min(5, expectedResidual)),
            result.SavedAdjacentRareCount);
    }

    [Fact]
    public void SaveStarfish_holds_leftover_when_boxes_consume_residual()
    {
        var pieces = Fillers(12);
        for (var i = 0; i < 6; i++)
            pieces.Add(Piece(200 + i, $"Box{i}", Strongbox(2, 10 + i)));
        for (var i = 0; i < 4; i++)
            pieces.Add(Piece(300 + i, $"Star{i}", Starfish(2, 7)));

        var options = VoyageStrategyOptions.AllEnabled with
        {
            RareMonstersDrop = true,
            SaveStarfish = 2,
        };

        var result = VoyagePlacementRules.Apply(pieces, EmptyBorders(), options);

        Assert.Equal(6, result.SavedStrongboxCount);
        Assert.Equal(2, result.SavedStarfishCount);
        Assert.Equal(0, result.SavedAdjacentRareCount);
    }

    [Fact]
    public void Annul_uses_strongbox_ranks_4_through_6()
    {
        
        
        var pieces = new List<MapPiece>
        {
            Piece(0, ChartIds.PelagicRoomName),
        };
        for (var i = 1; i <= 9; i++)
            pieces.Add(Piece(i, $"Box{i}", Strongbox(2, 20 - i)));
        for (var i = 10; i <= 12; i++)
            pieces.Add(Piece(i, $"Star{i}", Starfish(2, 7)));
        for (var i = 13; i < 20; i++)
            pieces.Add(Piece(i, $"Filler{i}"));

        var borders = EmptyBorders();
        borders[1, 0] = [new BorderEffect(ChartIds.RareAnnul, ModifierTag.All, 1, false, false)];

        var result = VoyagePlacementRules.Apply(pieces, borders, VoyageStrategyOptions.AllEnabled);

        var annulSupports = result.Locks
            .Where(l => l.Strategy == "Annul" && l.PieceId != 0)
            .ToList();
        Assert.Equal(3, annulSupports.Count);
        Assert.All(annulSupports, l => Assert.InRange(l.PieceId, 4, 6));
        Assert.DoesNotContain(result.Locks, l => l.PieceId is >= 7 and <= 12);
    }
}
