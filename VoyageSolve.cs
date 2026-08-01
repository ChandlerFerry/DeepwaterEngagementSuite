using System.Collections.Generic;
using DeepwaterEngagementSuite.VoyagePlannerData;

namespace DeepwaterEngagementSuite;

public sealed class VoyageSolve
{
    private VoyagePlanner _slowPlanner;

    public VoyageScorer Scorer { get; private set; }
    public VoyagePlacementRules.Result Placement { get; private set; }
    public VoyagePuzzle Puzzle { get; private set; }

    public void Cancel() => _slowPlanner?.Cancel();

    public IEnumerable<VoyageSolutionResult> Run(
        List<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders,
        bool useFastSolver,
        VoyagePlannerSettings settings = null)
    {
        settings ??= new VoyagePlannerSettings();
        _slowPlanner = null;

        Placement = VoyagePlacementRules.Apply(pieces, tileBorders);
        Puzzle = new VoyagePuzzle(Placement.Pieces, tileBorders, Placement.Locks);
        Scorer = new VoyageScorer(Puzzle);

        if (useFastSolver)
            return new VoyagePlannerFast().Solve(Puzzle, settings);

        _slowPlanner = new VoyagePlanner();
        return _slowPlanner.Solve(Puzzle, settings);
    }
}
