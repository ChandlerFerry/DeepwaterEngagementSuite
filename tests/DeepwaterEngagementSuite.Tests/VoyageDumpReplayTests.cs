using System;
using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;
using Xunit;
using Xunit.Abstractions;

namespace DeepwaterEngagementSuite.Tests;


public class VoyageDumpReplayTests(ITestOutputHelper output)
{
    [Theory]
    [MemberData(nameof(VoyageStateDumpFixture.AllFixtures), MemberType = typeof(VoyageStateDumpFixture))]
    public void Dump_replays_to_the_same_locks_it_recorded(string fixture)
    {
        if (fixture == null)
            return;

        var dump = VoyageStateDumpFixture.Load(fixture);
        var placement = VoyageStateDumpFixture.Replay(dump);
        output.WriteLine(VoyageStateDumpFixture.Explain(dump, placement));

        if (dump.Placement == null)
            return;

        
        static string Cells(IEnumerable<(int Row, int Col)> cells) =>
            string.Join(" ", cells.Select(c => $"({c.Row},{c.Col})").OrderBy(s => s, StringComparer.Ordinal));
        static string Ids(IEnumerable<int> ids) =>
            string.Join(" ", ids.OrderBy(i => i));

        var message = "Replay diverged from the captured run — the dump is likely missing an " +
                      "input the pipeline depends on.\n" + VoyageStateDumpFixture.Explain(dump, placement);

        Assert.True(
            Cells(dump.Placement.Locks.Select(l => (l.Row, l.Col))) ==
            Cells(placement.Locks.Select(l => (l.Row, l.Col))), message);
        Assert.True(
            Ids(dump.Placement.Locks.Select(l => l.PieceId)) ==
            Ids(placement.Locks.Select(l => l.PieceId)), message);
    }

    [Theory]
    [MemberData(nameof(VoyageStateDumpFixture.AllFixtures), MemberType = typeof(VoyageStateDumpFixture))]
    public void Each_orb_center_gets_support_charts_on_its_free_neighbors(string fixture)
    {
        if (fixture == null)
            return;

        var dump = VoyageStateDumpFixture.Load(fixture);
        if (!dump.StrategyOptions.RareMonstersDrop)
            return;

        var (pieces, borders, options) = VoyageStateDumpFixture.Inputs(dump);
        var placement = VoyagePlacementRules.Apply(pieces, borders, options);
        var lockedCells = placement.Locks.Select(l => (l.Row, l.Col)).ToHashSet();

        foreach (var (row, col) in ChartPredicates.EnumerateCells())
        {
            var priority = ChartPredicates.OrbPriority(ChartPredicates.BordersAt(borders, row, col));
            if (priority <= 0)
                continue;

            var neighbors = ChartIds.Ortho
                .Select(d => (Row: row + d.Dr, Col: col + d.Dc))
                .Where(n => n.Row is >= 0 and <= 2 && n.Col is >= 0 and <= 2)
                .ToList();
            var covered = neighbors.Count(n => lockedCells.Contains(n));

            output.WriteLine(
                $"orb p{priority} at ({row},{col}): {covered}/{neighbors.Count} neighbors locked");
        }

        output.WriteLine(VoyageStateDumpFixture.Explain(dump, placement));
    }

    [Theory]
    [MemberData(nameof(VoyageStateDumpFixture.AllFixtures), MemberType = typeof(VoyageStateDumpFixture))]
    public void Dump_produces_at_least_one_solution(string fixture)
    {
        if (fixture == null)
            return;

        var dump = VoyageStateDumpFixture.Load(fixture);
        var (placement, result) = VoyageStateDumpFixture.Solve(dump);

        output.WriteLine(VoyageStateDumpFixture.Explain(dump, placement));
        Assert.NotNull(result);
        Assert.True(result.Solutions.Count > 0,
            "Solver returned no solutions — the strategy locks are likely unsatisfiable for every topology.\n" +
            VoyageStateDumpFixture.Explain(dump, placement));
    }
}
