using System.Collections.Generic;
using DeepwaterEngagementSuite.VoyagePlannerData;

namespace DeepwaterEngagementSuite;

/// <summary>
/// Single Solve entrypoint used by the optimizer UI.
/// Applies hard placement strategies first, then runs the chosen planner on whatever
/// cells/pieces remain unconstrained. Cancel is honored for the timed planner.
/// </summary>
public sealed class VoyageSolve
{
    private VoyagePlanner _slowPlanner;

    public VoyageScorer Scorer { get; private set; }
    public VoyagePlacementRules.Result Placement { get; private set; }
    public VoyagePuzzle Puzzle { get; private set; }

    public void Cancel() => _slowPlanner?.Cancel();

    /// <summary>
    /// Strategy pass + solver. Yields intermediate results the same way the planners do.
    /// </summary>
    public IEnumerable<VoyageSolutionResult> Run(
        List<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders,
        bool useFastSolver,
        VoyagePlannerSettings settings = null)
    {
        settings ??= new VoyagePlannerSettings();
        _slowPlanner = null;

        // 1) Hard strategies: locks + hold specialty charts off the board
        Placement = VoyagePlacementRules.Apply(pieces, tileBorders);
        Puzzle = new VoyagePuzzle(Placement.Pieces, tileBorders, Placement.Locks);
        Scorer = new VoyageScorer(Puzzle);

        // 2) Planner optimizes free cells / remaining pieces under those constraints
        if (useFastSolver)
            return new VoyagePlannerFast().Solve(Puzzle, settings);

        _slowPlanner = new VoyagePlanner();
        return _slowPlanner.Solve(Puzzle, settings);
    }
}
