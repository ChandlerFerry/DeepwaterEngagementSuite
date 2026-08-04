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
    public int DroppedLockCount => DroppedLocks.Count;

    /// <summary>Human-readable descriptions of each lock that was dropped (order = drop order).</summary>
    public List<string> DroppedLocks { get; } = [];

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
        DroppedLocks.Clear();

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

            // No topology satisfied the full lock set. Drop the lowest-priority lock
            // (Divine Pelagic/support outrank Divine fill, Annul, etc.) so important
            // strategies are kept as long as possible. Among equal priority, drop the
            // later-appended lock first (fill/support tails).
            var dropIdx = IndexOfLockToDrop(locks);
            var dropped = locks[dropIdx];
            locks.RemoveAt(dropIdx);
            DroppedLocks.Add(FormatDroppedLock(dropped, pieces));
            Placement = Placement with { Locks = locks.ToList() };
        }
    }

    private static int IndexOfLockToDrop(IReadOnlyList<LockedPlacement> locks)
    {
        var bestIdx = locks.Count - 1;
        var bestPriority = locks[bestIdx].Priority;
        for (var i = locks.Count - 2; i >= 0; i--)
        {
            if (locks[i].Priority < bestPriority)
            {
                bestPriority = locks[i].Priority;
                bestIdx = i;
            }
        }

        return bestIdx;
    }

    private static string FormatDroppedLock(LockedPlacement lp, IReadOnlyList<MapPiece> pieces)
    {
        var piece = pieces?.FirstOrDefault(p => p.Id == lp.PieceId);
        var room = string.IsNullOrWhiteSpace(piece?.Name) ? null : piece.Name;
        var strategy = string.IsNullOrWhiteSpace(lp.Strategy) ? "lock" : lp.Strategy;
        var pieceBit = room != null ? $"{room} #{lp.PieceId}" : $"piece #{lp.PieceId}";
        return $"{strategy} @ ({lp.Row},{lp.Col}) [{pieceBit}]";
    }

    private IEnumerable<VoyageSolutionResult> SolveOnce(bool useFastSolver, VoyagePlannerSettings settings)
    {
        if (useFastSolver)
            return new VoyagePlannerFast().Solve(Puzzle, settings);

        _slowPlanner = new VoyagePlanner();
        return _slowPlanner.Solve(Puzzle, settings);
    }
}
