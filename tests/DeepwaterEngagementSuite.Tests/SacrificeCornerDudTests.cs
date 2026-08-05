using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Xunit;

namespace DeepwaterEngagementSuite.Tests;

public class SacrificeCornerDudTests
{
    private static readonly (Direction Dir, int Dr, int Dc)[] Dirs =
    [
        (Direction.Up, 1, 0),
        (Direction.Down, -1, 0),
        (Direction.Left, 0, -1),
        (Direction.Right, 0, 1),
    ];

    private static MapPiece Chart(int id, PieceType type, Direction connections, string name = "Chart") =>
        new(id, type, connections, [new Modifier("Default", 1)], name);

    private static IReadOnlyList<BorderEffect>[,] EmptyBorders()
    {
        var borders = new IReadOnlyList<BorderEffect>[3, 3];
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            borders[r, c] = [];
        return borders;
    }

    private static bool IsBorderFacingDeadEnd(int r, int c, Direction conn)
    {
        if (conn == Direction.None) return false;
        foreach (var (dir, dr, dc) in Dirs)
        {
            if (!conn.HasFlag(dir)) continue;
            var nr = r + dr;
            var nc = c + dc;
            if (nr is >= 0 and < 3 && nc is >= 0 and < 3)
                return false;
        }

        return true;
    }

    private static bool HasMutualInGridConnection(
        MapPiecePlacement[,] grid, int r, int c, Direction conn)
    {
        foreach (var (dir, dr, dc) in Dirs)
        {
            if (!conn.HasFlag(dir)) continue;
            var nr = r + dr;
            var nc = c + dc;
            if (nr is < 0 or >= 3 || nc is < 0 or >= 3) continue;
            var neighbor = grid[nr, nc];
            if (neighbor != null && neighbor.Connections.HasFlag(dir.Opposite()))
                return true;
        }

        return false;
    }

    [Fact]
    public void Sacrifice_duds_cannot_be_tees_straights_or_crosses()
    {
        var pieces = new List<MapPiece>
        {
            Chart(0, PieceType.Cross, Direction.All, "Hub"),
            Chart(1, PieceType.Cross, Direction.All, "CrossA"),
            Chart(2, PieceType.Cross, Direction.All, "CrossB"),
            Chart(3, PieceType.Tee, Direction.Up | Direction.Left | Direction.Right, "TeeA"),
            Chart(4, PieceType.Tee, Direction.Up | Direction.Left | Direction.Right, "TeeB"),
            Chart(5, PieceType.Tee, Direction.Up | Direction.Left | Direction.Right, "TeeC"),
            Chart(6, PieceType.Straight, Direction.Left | Direction.Right, "Straight"),
            Chart(7, PieceType.Corner, Direction.Down | Direction.Right, "CornerDud"),
            Chart(8, PieceType.Single, Direction.Down, "DeadEndDud"),
            Chart(9, PieceType.Corner, Direction.Down | Direction.Left, "CornerSpare"),
            Chart(10, PieceType.Single, Direction.Left, "DeadEndSpare"),
            Chart(11, PieceType.Single, Direction.Right, "DeadEndSpare2"),
        };

        var puzzle = new VoyagePuzzle(
            pieces,
            EmptyBorders(),
            LockedPlacements: null,
            AllowSacrificeCornerBorderDeadEnds: true);

        var last = new VoyagePlannerFast()
            .Solve(puzzle, new VoyagePlannerSettings(TopN: 30))
            .LastOrDefault();

        Assert.NotNull(last);
        Assert.True(last.Solutions.Count > 0, "Expected at least one solution.");

        foreach (var solution in last.Solutions)
        {
            foreach (var (row, col) in ChartIds.SacrificeCorners)
            {
                var placement = solution.Grid[row, col];
                Assert.NotNull(placement);

                var isDud = IsBorderFacingDeadEnd(row, col, placement.Connections)
                            || !HasMutualInGridConnection(solution.Grid, row, col, placement.Connections);

                if (!isDud)
                    continue;

                Assert.True(
                    placement.Piece.Type is PieceType.Corner or PieceType.Single,
                    $"Sacrifice dud at ({row},{col}) was {placement.Piece.Type} ({placement.Piece.Name}); " +
                    "only Corner or Single dead-ends may be duds.");
                Assert.True(
                    IsBorderFacingDeadEnd(row, col, placement.Connections),
                    $"Sacrifice dud at ({row},{col}) still faces into the board ({placement.Connections}).");
            }
        }
    }

    [Fact]
    public void High_value_tee_is_not_used_as_isolated_sacrifice_dud()
    {
        var pieces = new List<MapPiece>();
        for (var i = 0; i < 8; i++)
            pieces.Add(Chart(i, PieceType.Cross, Direction.All, $"Cross{i}"));

        pieces.Add(new MapPiece(
            100,
            PieceType.Tee,
            Direction.Up | Direction.Left | Direction.Right,
            [new Modifier("Default", 1), new Modifier("Fat", 500, true, ModifierTag.All)],
            "ValuableTee"));

        pieces.Add(Chart(101, PieceType.Corner, Direction.Down | Direction.Right, "RealDudCorner"));
        pieces.Add(Chart(102, PieceType.Single, Direction.Down, "RealDudEnd"));
        pieces.Add(Chart(103, PieceType.Single, Direction.Left, "SpareEnd"));

        var puzzle = new VoyagePuzzle(
            pieces,
            EmptyBorders(),
            LockedPlacements: null,
            AllowSacrificeCornerBorderDeadEnds: true);

        var last = new VoyagePlannerFast()
            .Solve(puzzle, new VoyagePlannerSettings(TopN: 20))
            .LastOrDefault();

        Assert.NotNull(last);

        foreach (var solution in last.Solutions)
        {
            foreach (var (row, col) in ChartIds.SacrificeCorners)
            {
                var placement = solution.Grid[row, col];
                if (placement?.Piece.Id != 100) continue;

                Assert.True(
                    HasMutualInGridConnection(solution.Grid, row, col, placement.Connections),
                    "Valuable Tee was placed as an isolated sacrifice dud.");
                Assert.False(IsBorderFacingDeadEnd(row, col, placement.Connections));
            }
        }
    }
}
