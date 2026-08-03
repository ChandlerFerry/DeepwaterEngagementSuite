using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DeepwaterEngagementSuite.VoyagePlannerData;

namespace DeepwaterEngagementSuite;

public class VoyagePlanner
{
    private const int GridSize = 3;

    private static readonly (Direction Dir, int Dr, int Dc)[] Directions =
    [
        (Direction.Up, 1, 0),
        (Direction.Down, -1, 0),
        (Direction.Left, 0, -1),
        (Direction.Right, 0, 1)
    ];

    private MapPiecePlacement[,] _grid;
    private bool[] _pieceUsed;
    private double _bestScore;
    private List<VoyageSolution> _topSolutions;
    private long _nodesExplored;
    private long _nodesPruned;
    private Stopwatch _stopwatch;
    private VoyagePuzzle _puzzle;
    private VoyageScorer _scorer;
    private bool _cancelled;
    private int _filledCount;
    private int[,] _lockedPieceAt;
    private int?[,] _lockedRotationAt;
    private int[] _pieceLockedToCell;

    private record struct PieceOption(int PieceIdx, int Rotation, Direction Connections);
    private PieceOption[][] _pieceOptionsByGroup;
    private int[] _pieceToGroup;
    private int[] _pieceScanOrder;

    public IEnumerable<VoyageSolutionResult> Solve(VoyagePuzzle puzzle, VoyagePlannerSettings settings = null)
    {
        settings ??= new VoyagePlannerSettings();
        _puzzle = puzzle;
        _grid = new MapPiecePlacement[GridSize, GridSize];
        _pieceUsed = new bool[puzzle.AvailablePieces.Count];
        _bestScore = 0;
        _topSolutions = new List<VoyageSolution>(settings.TopN);
        _nodesExplored = 0;
        _nodesPruned = 0;
        _filledCount = 0;
        _stopwatch = Stopwatch.StartNew();
        _cancelled = false;

        _scorer = new VoyageScorer(puzzle);

        var groupMap = new Dictionary<(PieceType, Direction, string), int>();
        var groups = new List<List<int>>();
        _pieceToGroup = new int[puzzle.AvailablePieces.Count];

        for (var i = 0; i < puzzle.AvailablePieces.Count; i++)
        {
            var p = puzzle.AvailablePieces[i];
            var key = (p.Type, p.BaseConnections, GetModifierSignature(p));
            if (!groupMap.TryGetValue(key, out var g))
            {
                g = groups.Count;
                groupMap[key] = g;
                groups.Add([]);
            }
            groups[g].Add(i);
            _pieceToGroup[i] = g;
        }

        _pieceOptionsByGroup = new PieceOption[groups.Count][];
        for (var g = 0; g < groups.Count; g++)
        {
            var piece = puzzle.AvailablePieces[groups[g][0]];
            var opts = new List<PieceOption>();
            for (var rot = 0; rot < piece.DistinctRotations; rot++)
            {
                opts.Add(new PieceOption(groups[g][0], rot, piece.GetConnections(rot)));
            }
            _pieceOptionsByGroup[g] = opts.ToArray();
        }

        _pieceScanOrder = Enumerable.Range(0, puzzle.AvailablePieces.Count)
            .OrderByDescending(i => puzzle.AvailablePieces[i].LocalModifier + puzzle.AvailablePieces[i].GlobalModifier)
            .ToArray();

        _lockedPieceAt = new int[GridSize, GridSize];
        _lockedRotationAt = new int?[GridSize, GridSize];
        _pieceLockedToCell = Enumerable.Repeat(-1, puzzle.AvailablePieces.Count).ToArray();
        for (var r = 0; r < GridSize; r++)
        for (var c = 0; c < GridSize; c++)
            _lockedPieceAt[r, c] = -1;

        var lockedCells = new HashSet<(int, int)>();
        foreach (var lp in puzzle.LockedPlacements)
        {
            var pieceIdx = puzzle.AvailablePieces.FindIndex(p => p.Id == lp.PieceId);
            if (pieceIdx < 0)
                continue;

            lockedCells.Add((lp.Row, lp.Col));
            _lockedPieceAt[lp.Row, lp.Col] = pieceIdx;
            _lockedRotationAt[lp.Row, lp.Col] = lp.Rotation;
            _pieceLockedToCell[pieceIdx] = lp.Row * GridSize + lp.Col;

            if (lp.Rotation is { } fixedRot)
            {
                var piece = puzzle.AvailablePieces[pieceIdx];
                var connections = piece.GetConnections(fixedRot);
                _grid[lp.Row, lp.Col] = new MapPiecePlacement(piece, fixedRot, connections);
                _pieceUsed[pieceIdx] = true;
                _filledCount++;
            }
        }

        var results = Search(settings, lockedCells);

        foreach (var result in results)
        {
            if (_cancelled) yield break;
            yield return result;
        }

        yield return FinalResult();
    }

    public void Cancel() => _cancelled = true;

    public bool WasCancelled => _cancelled;

    private IEnumerable<VoyageSolutionResult> Search(VoyagePlannerSettings settings, HashSet<(int, int)> lockedCells)
    {
        if (_cancelled) yield break;

        if (settings.TimeLimitSeconds.HasValue &&
            _stopwatch.Elapsed.TotalSeconds >= settings.TimeLimitSeconds.Value)
        {
            yield break;
        }

        if (_filledCount == GridSize * GridSize)
        {
            if (IsFullyConnected())
            {
                var score = _scorer.Score(_grid);
                if (score >= _bestScore)
                {
                    if (score > _bestScore)
                    {
                        _bestScore = score;
                        _topSolutions.Clear();
                    }

                    var solution = new VoyageSolution(CloneGrid(), score, true);
                    _topSolutions.Insert(0, solution);
                    if (_topSolutions.Count > settings.TopN)
                        _topSolutions.RemoveAt(_topSolutions.Count - 1);

                    if (settings.YieldIntermediate)
                    {
                        yield return new VoyageSolutionResult(
                            new List<VoyageSolution>(_topSolutions),
                            _nodesExplored,
                            _nodesPruned);
                    }
                }
            }

            yield break;
        }

        if (_scorer.UpperBound(_grid, _pieceUsed, _filledCount) < _bestScore)
        {
            _nodesPruned++;
            yield break;
        }

        var bestCell = (-1, -1);
        var bestOptions = new List<(int PieceIdx, int Rotation, Direction Connections)>();
        var bestOptionCount = int.MaxValue;

        for (var r = 0; r < GridSize; r++)
        {
            for (var c = 0; c < GridSize; c++)
            {
                if (_grid[r, c] != null) continue;

                var options = GetValidOptions(r, c);
                if (options.Count < bestOptionCount)
                {
                    bestOptionCount = options.Count;
                    bestCell = (r, c);
                    bestOptions = options;
                    if (bestOptionCount == 0) break;
                    if (bestOptionCount == 1) break;
                }
            }

            if (bestOptionCount == 0) break;
        }

        if (bestOptionCount == 0)
        {
            _nodesPruned++;
            yield break;
        }

        var (br, bc) = bestCell;
        _nodesExplored++;

        foreach (var (pieceIdx, rotation, connections) in bestOptions)
        {
            if (_cancelled) yield break;

            var piece = _puzzle.AvailablePieces[pieceIdx];
            _grid[br, bc] = new MapPiecePlacement(piece, rotation, connections);
            _pieceUsed[pieceIdx] = true;
            _filledCount++;

            if (IsConnectivityFeasible())
            {
                foreach (var result in Search(settings, lockedCells))
                {
                    yield return result;
                }
            }
            else
            {
                _nodesPruned++;
            }

            _pieceUsed[pieceIdx] = false;
            _grid[br, bc] = null;
            _filledCount--;
        }
    }

    private List<(int PieceIdx, int Rotation, Direction Connections)> GetValidOptions(int r, int c)
    {
        var result = new List<(int, int, Direction)>();
        var triedGroups = new HashSet<int>();
        var requiredPiece = _lockedPieceAt[r, c];
        var requiredRotation = _lockedRotationAt[r, c];
        var cellIndex = r * GridSize + c;

        foreach (var i in _pieceScanOrder)
        {
            if (_pieceUsed[i]) continue;
            if (requiredPiece >= 0 && i != requiredPiece) continue;
            if (_pieceLockedToCell[i] >= 0 && _pieceLockedToCell[i] != cellIndex) continue;

            var g = _pieceToGroup[i];
            if (requiredPiece < 0 && !triedGroups.Add(g)) continue;

            var piece = _puzzle.AvailablePieces[i];
            if (VoyagePlacementRules.IsCenterOnlyUniqueChart(piece) &&
                (r != VoyagePlacementRules.CenterRow || c != VoyagePlacementRules.CenterCol))
                continue;

            for (var rot = 0; rot < piece.DistinctRotations; rot++)
            {
                if (requiredRotation is { } rr && rot != rr) continue;
                var connections = piece.GetConnections(rot);
                if (CheckAdjacency(r, c, connections))
                    result.Add((i, rot, connections));
            }
        }

        return result;
    }

    private bool CheckAdjacency(int r, int c, Direction? connections = null)
    {
        var conn = connections ?? _grid[r, c].Connections;

        foreach (var (dir, dr, dc) in Directions)
        {
            var nr = r + dr;
            var nc = c + dc;

            if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
            if (_grid[nr, nc] == null) continue;

            var neighborConn = _grid[nr, nc].Connections;
            var hasConnection = conn.HasFlag(dir);
            var neighborHasConnection = neighborConn.HasFlag(dir.Opposite());

            if (hasConnection != neighborHasConnection)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsFullyConnected()
    {
        const int startRow = 0;
        const int startCol = 0;

        var allowBorderDeadEnds = _puzzle.AllowSacrificeCornerBorderDeadEnds;
        var visited = new bool[GridSize, GridSize];
        var isolatedSacrifice = new bool[GridSize, GridSize];
        var requiredCount = GridSize * GridSize;

        if (allowBorderDeadEnds)
        {
            for (var r = 0; r < GridSize; r++)
            for (var c = 0; c < GridSize; c++)
            {
                if (!VoyagePlacementRules.IsSacrificeCorner(r, c)) continue;
                if (r == startRow && c == startCol) continue;
                if (_grid[r, c] == null) continue;
                if (!IsBorderFacingDeadEnd(r, c, _grid[r, c].Connections)) continue;
                isolatedSacrifice[r, c] = true;
                requiredCount--;
            }
        }

        if (_grid[startRow, startCol] == null || isolatedSacrifice[startRow, startCol])
            return false;

        var stack = new Stack<(int R, int C)>();
        stack.Push((startRow, startCol));
        visited[startRow, startCol] = true;
        var count = 1;

        while (stack.TryPop(out var pos))
        {
            var (cr, cc) = pos;
            var conn = _grid[cr, cc].Connections;

            foreach (var (dir, dr, dc) in Directions)
            {
                if (!conn.HasFlag(dir)) continue;

                var nr = cr + dr;
                var nc = cc + dc;

                if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
                if (visited[nr, nc]) continue;
                if (_grid[nr, nc] == null) continue;
                if (isolatedSacrifice[nr, nc]) continue;

                var neighborConn = _grid[nr, nc].Connections;
                if (!neighborConn.HasFlag(dir.Opposite())) continue;

                visited[nr, nc] = true;
                count++;
                stack.Push((nr, nc));
            }
        }

        if (count != requiredCount) return false;

        for (var r = 0; r < GridSize; r++)
        for (var c = 0; c < GridSize; c++)
        {
            if (_grid[r, c] == null || isolatedSacrifice[r, c]) continue;
            if (!visited[r, c]) return false;
        }

        return true;
    }

    private static bool IsBorderFacingDeadEnd(int r, int c, Direction conn)
    {
        if (conn == Direction.None) return false;
        foreach (var (dir, dr, dc) in Directions)
        {
            if (!conn.HasFlag(dir)) continue;
            var nr = r + dr;
            var nc = c + dc;
            if (nr >= 0 && nr < GridSize && nc >= 0 && nc < GridSize)
                return false;
        }

        return true;
    }

    private bool IsConnectivityFeasible()
    {
        if (_filledCount <= 1) return true;
        if (_filledCount == GridSize * GridSize) return IsFullyConnected();

        var components = CountConnectedComponents(excludeSacrificeBorderDeadEnds: true);
        if (components <= 1) return true;

        var emptyCells = GridSize * GridSize - _filledCount;

        var mergeCapacities = new List<int>();
        for (var i = 0; i < _pieceUsed.Length; i++)
        {
            if (_pieceUsed[i]) continue;
            var maxConn = _pieceOptionsByGroup[_pieceToGroup[i]]
                .Max(o => o.Connections.CountConnections());
            mergeCapacities.Add(Math.Max(0, maxConn - 1));
        }

        var totalMergeCapacity = mergeCapacities
            .OrderByDescending(x => x)
            .Take(emptyCells)
            .Sum();

        if (totalMergeCapacity < components - 1) return false;

        return true;
    }

    private static string GetModifierSignature(MapPiece piece)
    {
        return string.Join("|", piece.Modifiers
            .OrderBy(m => m.Tags)
            .ThenBy(m => m.IsGlobal)
            .ThenBy(m => m.Weight)
            .Select(m => $"{(int)m.Tags}:{(m.IsGlobal ? 1 : 0)}:{m.Weight:R}"));
    }

    private int CountConnectedComponents(bool excludeSacrificeBorderDeadEnds = false)
    {
        var visited = new bool[GridSize, GridSize];
        var components = 0;
        var allowIso = excludeSacrificeBorderDeadEnds && _puzzle.AllowSacrificeCornerBorderDeadEnds;

        for (var sr = 0; sr < GridSize; sr++)
        {
            for (var sc = 0; sc < GridSize; sc++)
            {
                if (_grid[sr, sc] == null || visited[sr, sc]) continue;
                if (allowIso &&
                    VoyagePlacementRules.IsSacrificeCorner(sr, sc) &&
                    IsBorderFacingDeadEnd(sr, sc, _grid[sr, sc].Connections))
                {
                    visited[sr, sc] = true;
                    continue;
                }

                components++;
                visited[sr, sc] = true;
                var stack = new Stack<(int R, int C)>();
                stack.Push((sr, sc));

                while (stack.TryPop(out var pos))
                {
                    var (cr, cc) = pos;
                    var conn = _grid[cr, cc].Connections;

                    foreach (var (dir, dr, dc) in Directions)
                    {
                        if (!conn.HasFlag(dir)) continue;

                        var nr = cr + dr;
                        var nc = cc + dc;

                        if (nr < 0 || nr >= GridSize || nc < 0 || nc >= GridSize) continue;
                        if (visited[nr, nc] || _grid[nr, nc] == null) continue;
                        if (allowIso &&
                            VoyagePlacementRules.IsSacrificeCorner(nr, nc) &&
                            IsBorderFacingDeadEnd(nr, nc, _grid[nr, nc].Connections))
                            continue;

                        var neighborConn = _grid[nr, nc].Connections;
                        if (!neighborConn.HasFlag(dir.Opposite())) continue;

                        visited[nr, nc] = true;
                        stack.Push((nr, nc));
                    }
                }
            }
        }

        return components;
    }

    private MapPiecePlacement[,] CloneGrid()
    {
        var clone = new MapPiecePlacement[GridSize, GridSize];
        for (var i = 0; i < GridSize; i++)
            for (var j = 0; j < GridSize; j++)
                clone[i, j] = _grid[i, j];
        return clone;
    }

    private VoyageSolutionResult FinalResult()
    {
        return new VoyageSolutionResult(
            [.._topSolutions,],
            _nodesExplored,
            _nodesPruned);
    }
}
