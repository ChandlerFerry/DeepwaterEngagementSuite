using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData;

public sealed class PlacementContext
{
    public PlacementContext(
        IReadOnlyList<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders,
        VoyageStrategyOptions options)
    {
        Options = options ?? VoyageStrategyOptions.AllEnabled;
        Working = pieces.ToList();
        OriginalPieces = pieces;
        TileBorders = tileBorders;
        Locks = [];
        UsedPieceIds = [];
        LockedCells = [];
        SavedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        ActiveStrategies = [];

        DivineCenters = ChartPredicates.EnumerateCells()
            .Where(c => ChartPredicates.OrbPriority(BordersAt(c.Row, c.Col)) == 3)
            .Select(c => (c.Row, c.Col))
            .ToList();

        AnnulCenters = ChartPredicates.EnumerateCells()
            .Where(c => ChartPredicates.OrbPriority(BordersAt(c.Row, c.Col)) == 2)
            .Select(c => (c.Row, c.Col))
            .ToList();

        AncientCenters = ChartPredicates.EnumerateCells()
            .Where(c => ChartPredicates.OrbPriority(BordersAt(c.Row, c.Col)) == 1)
            .Select(c => (c.Row, c.Col))
            .ToList();

        OrbCenters = DivineCenters.Select(c => (c.Row, c.Col, Priority: 3))
            .Concat(AnnulCenters.Select(c => (c.Row, c.Col, Priority: 2)))
            .Concat(AncientCenters.Select(c => (c.Row, c.Col, Priority: 1)))
            .OrderByDescending(x => x.Priority)
            .ToList();

        ClamCountAtStart = Working.Count(p => !UsedPieceIds.Contains(p.Id) && ChartPredicates.IsClamChart(p));
        SurplusClams = ClamCountAtStart > ChartIds.MaxSavedClamsForAmulet;
        HasOrbs = OrbCenters.Count > 0;
        StrongTreasure = ChartPredicates.BoardHasStrongTreasureAnchors(tileBorders);
    }

    public VoyageStrategyOptions Options { get; }
    public IReadOnlyList<MapPiece> OriginalPieces { get; }
    public List<MapPiece> Working { get; }
    public List<LockedPlacement> Locks { get; }
    public HashSet<int> UsedPieceIds { get; }
    public HashSet<(int Row, int Col)> LockedCells { get; }
    public IReadOnlyList<BorderEffect>[,] TileBorders { get; }
    public Dictionary<string, int> SavedCounts { get; }
    public List<string> ActiveStrategies { get; }

    public List<(int Row, int Col)> DivineCenters { get; }
    public List<(int Row, int Col)> AnnulCenters { get; }
    public List<(int Row, int Col)> AncientCenters { get; }
    public List<(int Row, int Col, int Priority)> OrbCenters { get; }

    public int ClamCountAtStart { get; }
    public bool SurplusClams { get; }
    public bool HasOrbs { get; }
    public bool StrongTreasure { get; }

    public bool AmuletCrossLocked { get; set; }
    public bool PreferClamsAdjacentToAmulet { get; set; }
    public bool AmuletCenterLocked { get; set; }
    public bool PelagicLocked { get; set; }
    public bool NoConsumeActive { get; set; }

    public void LockCell(int row, int col, MapPiece piece, int? rotation = null)
    {
        if (!UsedPieceIds.Add(piece.Id))
            return;
        Locks.Add(new LockedPlacement(row, col, piece.Id, rotation));
        LockedCells.Add((row, col));
    }

    public bool CellFree(int row, int col) => !LockedCells.Contains((row, col));

    public void AddSaved(string key, int count)
    {
        if (count <= 0)
            return;
        SavedCounts[key] = SavedCounts.GetValueOrDefault(key) + count;
    }

    public int GetSaved(string key) => SavedCounts.GetValueOrDefault(key);

    public int SaveByPredicate(Func<MapPiece, bool> pred)
    {
        var saved = 0;
        foreach (var id in Working.Where(pred).Select(p => p.Id).ToList())
        {
            if (!TrySavePiece(id))
                break;
            saved++;
        }

        return saved;
    }

    public MapPiece TakeBest(Func<MapPiece, bool> pred, Func<MapPiece, double> score) =>
        Working
            .Where(p => !UsedPieceIds.Contains(p.Id) && pred(p))
            .OrderByDescending(score)
            .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
            .FirstOrDefault();

    public bool TrySavePiece(int pieceId, bool force = false)
    {
        if (!force && Working.Count <= 9)
            return false;
        return Working.RemoveAll(p => p.Id == pieceId) > 0;
    }

    public int RemoveUnused(
        Func<MapPiece, bool> pred,
        Func<MapPiece, double> score = null,
        int? maxSave = null,
        bool force = false)
    {
        IEnumerable<MapPiece> candidates = Working.Where(p => !UsedPieceIds.Contains(p.Id) && pred(p));
        if (score != null)
        {
            candidates = candidates
                .OrderByDescending(score)
                .ThenByDescending(p => p.LocalModifier + p.GlobalModifier);
        }

        var drop = candidates.Select(p => p.Id).ToList();
        if (maxSave is int cap && drop.Count > cap)
            drop = drop.Take(cap).ToList();

        var removed = 0;
        foreach (var id in drop)
        {
            if (!TrySavePiece(id, force))
                break;
            removed++;
        }

        return removed;
    }

    public IEnumerable<(int Row, int Col)> FreeNeighbors(int row, int col)
    {
        foreach (var (dr, dc) in ChartIds.Ortho)
        {
            var nr = row + dr;
            var nc = col + dc;
            if (nr is < 0 or > 2 || nc is < 0 or > 2)
                continue;
            if (CellFree(nr, nc))
                yield return (nr, nc);
        }
    }

    public IReadOnlyList<BorderEffect> BordersAt(int row, int col) =>
        ChartPredicates.BordersAt(TileBorders, row, col);
}
