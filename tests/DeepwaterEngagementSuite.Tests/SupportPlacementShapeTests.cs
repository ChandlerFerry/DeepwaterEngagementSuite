using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Xunit;

namespace DeepwaterEngagementSuite.Tests;


public class SupportPlacementShapeTests
{
    private static IReadOnlyList<BorderEffect>[,] BordersWithAnnulAt(int row, int col)
    {
        var borders = new IReadOnlyList<BorderEffect>[3, 3];
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            borders[r, c] = [];
        borders[row, col] = [new BorderEffect(ChartIds.RareAnnul, ModifierTag.All, 1, false, false)];
        return borders;
    }

    private static MapPiece Chart(int id, Direction connections, string name, params Modifier[] mods) =>
        new(id, VoyageStateDump.ClassifyPieceType(connections), connections,
            new List<Modifier> { new("Default", 1) }.Concat(mods).ToList(), name);

    private static Modifier Starfish(int tier, int value1) =>
        new($"{ChartIds.AdjacentStarfishPrefix}{tier}", 10, false, ModifierTag.Monsters, value1);

    
    private static List<MapPiece> BoardWithThreeStarfish()
    {
        var pieces = new List<MapPiece>
        {
            Chart(0, Direction.All, ChartIds.PelagicRoomName),
            Chart(1, Direction.Left | Direction.Right, "Abyssal Plain", Starfish(2, 7)),
            Chart(2, Direction.Up | Direction.Down, "Abyssal Plain", Starfish(2, 7)),
            Chart(3, Direction.Down, "Abyssal Plain", Starfish(1, 4)),
        };

        for (var i = 4; i < 14; i++)
            pieces.Add(Chart(i, Direction.All, $"Filler{i}"));

        return pieces;
    }

    [Fact]
    public void Dead_end_support_is_not_locked_onto_the_grid_centre()
    {
        var result = VoyagePlacementRules.Apply(
            BoardWithThreeStarfish(),
            BordersWithAnnulAt(1, 0),
            VoyageStrategyOptions.AllEnabled);

        var centre = result.Locks.FirstOrDefault(l => l is { Row: 1, Col: 1 });
        Assert.NotNull(centre);
        Assert.NotEqual(3, centre.PieceId);
    }

    [Fact]
    public void All_three_starfish_are_still_locked_adjacent_to_the_orb()
    {
        var result = VoyagePlacementRules.Apply(
            BoardWithThreeStarfish(),
            BordersWithAnnulAt(1, 0),
            VoyageStrategyOptions.AllEnabled);

        
        var starfishCells = result.Locks
            .Where(l => l.PieceId is 1 or 2 or 3)
            .Select(l => $"({l.Row},{l.Col})")
            .OrderBy(x => x, System.StringComparer.Ordinal)
            .ToList();

        Assert.Equal(3, starfishCells.Count);
        Assert.Equal(new[] { "(0,0)", "(1,1)", "(2,0)" }, starfishCells);
    }

    [Fact]
    public void Most_connected_support_takes_the_most_constrained_cell()
    {
        var result = VoyagePlacementRules.Apply(
            BoardWithThreeStarfish(),
            BordersWithAnnulAt(1, 0),
            VoyageStrategyOptions.AllEnabled);

        foreach (var placed in result.Locks.Where(l => l.PieceId is 1 or 2 or 3))
        {
            var degree = ChartPredicates.InGridDegree(placed.Row, placed.Col);
            var connections = placed.PieceId == 3 ? 1 : 2;
            
            if (connections == 1)
                Assert.Equal(2, degree);
        }
    }

    [Fact]
    public void The_captured_board_now_solves()
    {
        var pieces = BoardWithThreeStarfish();
        var borders = BordersWithAnnulAt(1, 0);
        var session = new VoyageSolve();

        VoyageSolutionResult last = null;
        foreach (var r in session.Run(pieces, borders,
                     settings: new VoyagePlannerSettings(TopN: 5),
                     strategyOptions: VoyageStrategyOptions.AllEnabled))
        {
            last = r;
        }

        Assert.NotNull(last);
        Assert.True(last.Solutions.Count > 0, "Expected at least one solution for the annul board.");
        
        Assert.Equal(session.DroppedLockCount, session.DroppedLocks.Count);
        if (session.DroppedLockCount > 0)
            Assert.All(session.DroppedLocks, d => Assert.Contains("Annul", d));
    }

    [Fact]
    public void Unsatisfiable_locks_degrade_instead_of_returning_nothing()
    {
        
        var pieces = new List<MapPiece> { Chart(0, Direction.All, ChartIds.PelagicRoomName) };
        for (var i = 1; i < 12; i++)
            pieces.Add(Chart(i, Direction.Down, "Abyssal Plain", Starfish(2, 7)));

        var session = new VoyageSolve();
        VoyageSolutionResult last = null;
        foreach (var r in session.Run(pieces, BordersWithAnnulAt(1, 0),
                     settings: new VoyagePlannerSettings(TopN: 5),
                     strategyOptions: VoyageStrategyOptions.AllEnabled))
        {
            last = r;
        }

        Assert.NotNull(last);
        Assert.True(session.DroppedLockCount > 0, "Expected the solver to give up at least one lock.");
        Assert.Equal(session.DroppedLockCount, session.DroppedLocks.Count);
        Assert.All(session.DroppedLocks, d => Assert.False(string.IsNullOrWhiteSpace(d)));
    }
}
