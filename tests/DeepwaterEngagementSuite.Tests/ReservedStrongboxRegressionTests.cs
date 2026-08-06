using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Xunit;

namespace DeepwaterEngagementSuite.Tests;

/// <summary>
/// Regression: reserved strongboxes (value1=5 "additional strongboxes") must never leak into
/// the solver pool just because inventory is short (e.g. only page 2 loaded).
///
/// Old bug: TrySavePiece refused to hold charts once Working.Count hit 9, so reserved boxes
/// stayed available and the solver placed them as filler. Only Divine may spend them.
/// </summary>
public class ReservedStrongboxRegressionTests
{
    private static IReadOnlyList<BorderEffect>[,] EmptyBorders()
    {
        var borders = new IReadOnlyList<BorderEffect>[3, 3];
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            borders[r, c] = [];
        return borders;
    }

    private static MapPiece Piece(int id, string name, params Modifier[] mods) =>
        new(id, PieceType.Cross, Direction.All, mods.ToList(), name);

    private static Modifier StrongboxValue1(int value1) =>
        new($"{ChartIds.AdjacentStrongboxesPrefix}2", 10, false, ModifierTag.None, value1);

    private static VoyageStrategyOptions RareMonstersOnly =>
        VoyageStrategyOptions.AllEnabled with
        {
            RareMonstersDrop = true,
            SaveStarfish = 0,
            // Keep other holds off so this case is only about strongbox reservation.
            SaveKishara = 0,
            SaveNoEquipment = 0,
            SaveFractured = 0,
            SaveGoldenLanterns = 0,
            SavePantheon = 0,
            SaveSoulEater = 0,
            SaveRareFracture = 0,
            SaveRarePossessed = 0,
            SaveUniqueAmulet2 = 0,
            SaveUniqueAmulet1 = 0,
        };

    /// <summary>
    /// Classic floor-of-9 trap: 6 fillers + 6 reserved value1=5 boxes (12 total).
    /// Without force-save, only 3 boxes could be held (12→9) and 3 would leak to the solver.
    /// </summary>
    [Fact]
    public void Floor_of_nine_must_not_leak_reserved_value1_5_strongboxes_into_working_pool()
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 6; i++)
            pieces.Add(Piece(i, $"Filler{i}"));

        var reservedBoxIds = new HashSet<int>();
        for (var i = 0; i < 6; i++)
        {
            var id = 100 + i;
            reservedBoxIds.Add(id);
            pieces.Add(Piece(id, $"Box{i}", StrongboxValue1(5)));
        }

        var placement = VoyagePlacementRules.Apply(pieces, EmptyBorders(), RareMonstersOnly);

        Assert.Equal(6, placement.SavedStrongboxCount);
        Assert.DoesNotContain(placement.Pieces, p => reservedBoxIds.Contains(p.Id));
        Assert.DoesNotContain(placement.Pieces, ChartPredicates.IsStrongboxCountChart);
        Assert.DoesNotContain(placement.Locks, l => reservedBoxIds.Contains(l.PieceId));
    }

    /// <summary>
    /// Full solve path: reserved box piece ids must never appear on any solution grid cell
    /// when no Divine orb is present (nothing is allowed to spend the reservation).
    /// </summary>
    [Fact]
    public void Solver_must_never_place_reserved_strongboxes_without_divine_opt_in()
    {
        // Page-2 style inventory: exactly 9 placeable fillers + 6 reserved value1=5 boxes.
        // Fillers alone can fill the board; boxes must not be used as cheap extras.
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 9; i++)
            pieces.Add(Piece(i, $"Filler{i}"));

        var reservedBoxIds = new HashSet<int>();
        for (var i = 0; i < 6; i++)
        {
            var id = 100 + i;
            reservedBoxIds.Add(id);
            pieces.Add(Piece(id, $"Box{i}", StrongboxValue1(5)));
        }

        var session = new VoyageSolve();
        VoyageSolutionResult last = null;
        foreach (var result in session.Run(
                     pieces,
                     EmptyBorders(),
                     settings: new VoyagePlannerSettings(TopN: 5, TimeLimitSeconds: 5),
                     strategyOptions: RareMonstersOnly))
        {
            last = result;
        }

        Assert.NotNull(session.Placement);
        Assert.Equal(6, session.Placement.SavedStrongboxCount);
        Assert.DoesNotContain(session.Placement.Pieces, p => reservedBoxIds.Contains(p.Id));
        Assert.DoesNotContain(session.Placement.Locks, l => reservedBoxIds.Contains(l.PieceId));

        Assert.NotNull(last);
        Assert.True(last.Solutions.Count > 0,
            "Expected solutions from the 9 fillers alone; reserved boxes must not be required.");

        foreach (var solution in last.Solutions)
        {
            for (var r = 0; r < 3; r++)
            for (var c = 0; c < 3; c++)
            {
                var placed = solution.Grid[r, c];
                Assert.NotNull(placed);
                Assert.False(
                    reservedBoxIds.Contains(placed.Piece.Id),
                    $"Reserved strongbox #{placed.Piece.Id} was placed at ({r},{c}) — " +
                    "holds must never fill the board unless Divine explicitly spends them.");
            }
        }
    }

    /// <summary>
    /// When fillers alone cannot fill the board, still refuse to burn reserved boxes.
    /// Better: no solution (or solutions without boxes) than silently spending holds.
    /// </summary>
    [Fact]
    public void Short_fillers_still_refuse_to_spend_reserved_strongboxes_as_filler()
    {
        // Only 6 fillers — not enough for a 3x3. Six reserved boxes would "help" if leaked.
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 6; i++)
            pieces.Add(Piece(i, $"Filler{i}"));

        var reservedBoxIds = new HashSet<int>();
        for (var i = 0; i < 6; i++)
        {
            var id = 100 + i;
            reservedBoxIds.Add(id);
            pieces.Add(Piece(id, $"Box{i}", StrongboxValue1(5)));
        }

        var session = new VoyageSolve();
        VoyageSolutionResult last = null;
        foreach (var result in session.Run(
                     pieces,
                     EmptyBorders(),
                     settings: new VoyagePlannerSettings(TopN: 5, TimeLimitSeconds: 5),
                     strategyOptions: RareMonstersOnly))
        {
            last = result;
        }

        Assert.Equal(6, session.Placement.SavedStrongboxCount);
        Assert.DoesNotContain(session.Placement.Pieces, p => reservedBoxIds.Contains(p.Id));

        if (last is { Solutions.Count: > 0 })
        {
            foreach (var solution in last.Solutions)
            {
                for (var r = 0; r < 3; r++)
                for (var c = 0; c < 3; c++)
                {
                    var placed = solution.Grid[r, c];
                    if (placed?.Piece == null)
                        continue;
                    Assert.DoesNotContain(placed.Piece.Id, reservedBoxIds);
                }
            }
        }
    }
}
