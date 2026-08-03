using System.Collections.Generic;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;

namespace DeepwaterEngagementSuite;

public sealed class VoyageSolve
{
    private VoyagePlanner _slowPlanner;

    public VoyageScorer Scorer { get; private set; }
    public VoyagePlacementRules.Result Placement { get; private set; }
    public VoyagePuzzle Puzzle { get; private set; }

    /// <summary>How many strategy locks had to be given up before the board became solvable.</summary>
    public int DroppedLockCount { get; private set; }

    public void Cancel() => _slowPlanner?.Cancel();

    public IEnumerable<VoyageSolutionResult> Run(
        List<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders,
        bool useFastSolver,
        VoyagePlannerSettings settings = null,
        VoyageStrategyOptions strategyOptions = null)
    {
        settings ??= new VoyagePlannerSettings();
        _slowPlanner = null;
        DroppedLockCount = 0;

        Placement = VoyagePlacementRules.Apply(pieces, tileBorders, strategyOptions);
        var locks = Placement.Locks.ToList();

        while (true)
        {
            Puzzle = new VoyagePuzzle(
                Placement.Pieces,
                tileBorders,
                locks,
                AllowSacrificeCornerBorderDeadEnds: Placement.AmuletClamHubActive,
                PreferClamsAdjacentToAmulet: Placement.PreferClamsAdjacentToAmulet);
            Scorer = new VoyageScorer(Puzzle);

            VoyageSolutionResult last = null;
            foreach (var result in SolveOnce(useFastSolver, settings))
            {
                last = result;
                yield return result;
            }

            if (last is { Solutions.Count: > 0 })
                yield break;

            // A cancelled or timed-out run tells us nothing about whether the locks are
            // satisfiable, so don't burn another solve on it.
            if (_slowPlanner is { WasCancelled: true })
                yield break;

            if (locks.Count == 0)
                yield break;

            // Every topology was rejected, which means the strategy locks cannot all hold at
            // once. Give up the last one — locks are appended lowest-value-first within a
            // centre, so the tail is the cheapest to lose — and try again. A degraded board
            // beats "No solutions found".
            locks.RemoveAt(locks.Count - 1);
            DroppedLockCount++;
            Placement = Placement with { Locks = locks.ToList() };
        }
    }

    private IEnumerable<VoyageSolutionResult> SolveOnce(bool useFastSolver, VoyagePlannerSettings settings)
    {
        if (useFastSolver)
            return new VoyagePlannerFast().Solve(Puzzle, settings);

        _slowPlanner = new VoyagePlanner();
        return _slowPlanner.Solve(Puzzle, settings);
    }
}
