using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData;

/// <summary>
/// Hard placement preferences for the voyage board.
/// Priority (high → low): Divine → Annul → Scarab farm → Ancient → no-consume farm.
/// Chart names come from DeepwaterChart.Room.Name.
/// </summary>
public static class VoyagePlacementRules
{
    public const string NotConsume1 = "DeepwaterBorderChanceToNotConsumeChart1";
    public const string NotConsume2 = "DeepwaterBorderChanceToNotConsumeChart2";
    public const string RareDivine = "DeepwaterBorderRareMonsterDivine";
    public const string RareAnnul = "DeepwaterBorderRareMonsterAnnulment";
    public const string RareAncient = "DeepwaterBorderRareMonsterAncient";
    public const string MoreScarabs1 = "DeepwaterBorderMoreScarabs1";
    public const string MoreScarabs2 = "DeepwaterBorderMoreScarabs2";
    public const string MoreScarabs3 = "DeepwaterBorderMoreScarabs3";

    // DeepwaterChart.Room.Name values
    private const string PelagicRoomName = "Pelagic Abyss";
    private const string ClamRoomName = "Clam-infested Shelf";
    private const string AnchorfieldRoomName = "Anchorfield";

    private static readonly (int Dr, int Dc)[] Ortho = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    public sealed record Result(
        List<MapPiece> Pieces,
        List<LockedPlacement> Locks,
        int SavedPelagicCount,
        int SavedOperativeCount,
        int SavedStrongboxCount,
        int SavedStarfishCount,
        int SavedRareVoyageCount);

    public static Result Apply(
        IReadOnlyList<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders)
    {
        var working = pieces.ToList();
        var locks = new List<LockedPlacement>();
        var usedPieceIds = new HashSet<int>();
        var lockedCells = new HashSet<(int Row, int Col)>();

        void LockCell(int row, int col, MapPiece piece)
        {
            if (!usedPieceIds.Add(piece.Id))
                return;
            locks.Add(new LockedPlacement(row, col, piece.Id));
            lockedCells.Add((row, col));
        }

        bool CellFree(int row, int col) => !lockedCells.Contains((row, col));

        // --- Identify centers ---
        var divineCenters = EnumerateCells()
            .Where(c => OrbPriority(BordersAt(tileBorders, c.Row, c.Col)) == 3)
            .Select(c => (c.Row, c.Col))
            .ToList();

        var annulCenters = EnumerateCells()
            .Where(c => OrbPriority(BordersAt(tileBorders, c.Row, c.Col)) == 2)
            .Select(c => (c.Row, c.Col))
            .ToList();

        var ancientCenters = EnumerateCells()
            .Where(c => OrbPriority(BordersAt(tileBorders, c.Row, c.Col)) == 1)
            .Select(c => (c.Row, c.Col))
            .ToList();

        var scarabCenters = EnumerateCells()
            .Select(c => (c.Row, c.Col, Tier: ScarabTier(BordersAt(tileBorders, c.Row, c.Col))))
            .Where(x => x.Tier > 0)
            .OrderByDescending(x => x.Tier)
            .ToList();

        // --- 1. Pelagic on Divine > Annul > Ancient ---
        var orbCenters = divineCenters.Select(c => (c.Row, c.Col, Priority: 3))
            .Concat(annulCenters.Select(c => (c.Row, c.Col, Priority: 2)))
            .Concat(ancientCenters.Select(c => (c.Row, c.Col, Priority: 1)))
            .OrderByDescending(x => x.Priority)
            .ToList();

        var savedPelagic = 0;
        foreach (var pelagic in working.Where(IsPelagic).OrderByDescending(p => p.LocalModifier + p.GlobalModifier).ToList())
        {
            if (usedPieceIds.Contains(pelagic.Id))
                continue;

            var target = orbCenters.FirstOrDefault(c => CellFree(c.Row, c.Col));
            if (target.Priority > 0)
            {
                LockCell(target.Row, target.Col, pelagic);
                orbCenters.RemoveAll(c => c.Row == target.Row && c.Col == target.Col);
            }
            else
            {
                working.RemoveAll(p => p.Id == pelagic.Id);
                savedPelagic++;
            }
        }

        // --- 2. Divine surrounds: strongbox > starfish > adjacent rares ---
        foreach (var center in divineCenters)
        {
            foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
            {
                var support = TakeBest(working, usedPieceIds, IsStrongboxCountChart, StrongboxCountScore)
                              ?? TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                              ?? TakeBest(working, usedPieceIds, IsAdjacentRareChart, AdjacentRareScore);
                if (support == null) break;
                LockCell(n.Row, n.Col, support);
            }
        }

        // --- 3. Divine free tiles: voyage increased rares > solver ---
        if (divineCenters.Count > 0)
        {
            foreach (var cell in EnumerateCells().Where(c => CellFree(c.Row, c.Col)))
            {
                var rare = TakeBest(working, usedPieceIds, IsRareVoyageChart, RareVoyageScore);
                if (rare == null)
                    break; // remaining free cells → solver
                LockCell(cell.Row, cell.Col, rare);
            }
        }

        // --- 4. Annul surrounds (above scarab): starfish > adjacent rares ---
        foreach (var center in annulCenters)
        {
            foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
            {
                var support = TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                              ?? TakeBest(working, usedPieceIds, IsAdjacentRareChart, AdjacentRareScore);
                if (support == null) break;
                LockCell(n.Row, n.Col, support);
            }
        }

        // --- 5. Scarab surrounds (above Ancient): Operative boxes ---
        // MoreScarabs2 + MoreScarabs3 both trigger (T3 preferred). Round-robin neighbors so
        // several scarab tiles each get Operatives instead of one tile taking them all.
        {
            var pending = scarabCenters
                .Select(c => (
                    c.Tier,
                    Neighbors: new Queue<(int Row, int Col)>(FreeNeighbors(c.Row, c.Col, CellFree))))
                .Where(c => c.Neighbors.Count > 0)
                .OrderByDescending(c => c.Tier)
                .ToList();

            while (pending.Count > 0)
            {
                var progressed = false;
                for (var i = 0; i < pending.Count;)
                {
                    var neighbors = pending[i].Neighbors;
                    // One free neighbor for this center this wave (or drop the center).
                    (int Row, int Col)? target = null;
                    while (neighbors.Count > 0)
                    {
                        var n = neighbors.Dequeue();
                        if (CellFree(n.Row, n.Col))
                        {
                            target = n;
                            break;
                        }
                    }

                    if (target is null)
                    {
                        pending.RemoveAt(i);
                        continue;
                    }

                    var op = TakeBest(working, usedPieceIds, IsOperativeChart, OperativeScore);
                    if (op == null)
                    {
                        pending.Clear();
                        break;
                    }

                    LockCell(target.Value.Row, target.Value.Col, op);
                    progressed = true;
                    if (neighbors.Count == 0)
                        pending.RemoveAt(i);
                    else
                        i++;
                }

                if (!progressed)
                    break;
            }
        }

        // --- 6. Ancient surrounds (below scarab): starfish > adjacent rares ---
        foreach (var center in ancientCenters)
        {
            foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
            {
                var support = TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                              ?? TakeBest(working, usedPieceIds, IsAdjacentRareChart, AdjacentRareScore);
                if (support == null) break;
                LockCell(n.Row, n.Col, support);
            }
        }

        // --- 7. No-consume farm maps (lowest priority) ---
        foreach (var cell in EnumerateCells().Where(c =>
                     CellFree(c.Row, c.Col) &&
                     IsStrongNoConsume(BordersAt(tileBorders, c.Row, c.Col))))
        {
            var farm = TakeBest(working, usedPieceIds, IsFarmChart, FarmPriority);
            if (farm == null) break;
            LockCell(cell.Row, cell.Col, farm);
        }

        // --- 8. Save leftovers (never drop below 9 pieces for the solver) ---
        var savedOperative = RemoveUnused(working, usedPieceIds, IsOperativeChart);
        var savedStrongbox = RemoveUnused(working, usedPieceIds, IsStrongboxCountChart);
        var savedStarfish = RemoveUnused(working, usedPieceIds, IsStarfishChart);
        var savedRareVoyage = RemoveUnused(working, usedPieceIds, IsRareVoyageChart);

        return new Result(
            working, locks,
            savedPelagic, savedOperative, savedStrongbox, savedStarfish, savedRareVoyage);
    }

    // --- Chart classification (Room.Name / mod ids) ---

    public static bool IsFarmChart(MapPiece piece) =>
        piece.Name.Contains(ClamRoomName, StringComparison.OrdinalIgnoreCase) ||
        piece.Name.Contains(AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsPelagic(MapPiece piece) =>
        piece.Name.Contains(PelagicRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsOperativeChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Contains("OperativeBox", StringComparison.OrdinalIgnoreCase));

    /// <summary>Raw strongbox count / premium boxes (not Operator — reserved for scarabs).</summary>
    public static bool IsStrongboxCountChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Contains("AdjacentStrongboxes", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("DivinerBox", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("ArcanistBox", StringComparison.OrdinalIgnoreCase));

    public static bool IsStarfishChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Contains("AdjacentStarfish", StringComparison.OrdinalIgnoreCase));

    public static bool IsAdjacentRareChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Contains("AdjacentIncreasedRareMonsters", StringComparison.OrdinalIgnoreCase));

    public static bool IsRareVoyageChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals("MapDeepwaterChartVoyageIncreasedRareMonsters", StringComparison.OrdinalIgnoreCase));

    /// <summary>Anchorfield preferred over Clam-infested Shelf; then total mod weight.</summary>
    private static double FarmPriority(MapPiece p)
    {
        var room = 0.0;
        if (p.Name.Contains(AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase))
            room = 2;
        else if (p.Name.Contains(ClamRoomName, StringComparison.OrdinalIgnoreCase))
            room = 1;
        return room * 1_000_000 + p.LocalModifier + p.GlobalModifier;
    }

    private static double OperativeScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Contains("OperativeBox", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    private static double StrongboxCountScore(MapPiece p)
    {
        double score = 0;
        foreach (var m in p.Modifiers)
        {
            if (m.Name.Contains("AdjacentStrongboxes3", StringComparison.OrdinalIgnoreCase))
                score += 1000 + m.Weight;
            else if (m.Name.Contains("AdjacentStrongboxes2", StringComparison.OrdinalIgnoreCase))
                score += 500 + m.Weight;
            else if (m.Name.Contains("AdjacentStrongboxes1", StringComparison.OrdinalIgnoreCase))
                score += 100 + m.Weight;
            else if (m.Name.Contains("DivinerBox", StringComparison.OrdinalIgnoreCase) ||
                     m.Name.Contains("ArcanistBox", StringComparison.OrdinalIgnoreCase))
                score += m.Weight;
        }

        return score;
    }

    private static double StarfishScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Contains("AdjacentStarfish", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    private static double AdjacentRareScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Contains("AdjacentIncreasedRareMonsters", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    private static double RareVoyageScore(MapPiece p) =>
        p.Modifiers.Where(m =>
                m.Name.Equals("MapDeepwaterChartVoyageIncreasedRareMonsters", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    // --- Border helpers ---

    public static bool IsStrongNoConsume(IReadOnlyList<BorderEffect> borders)
    {
        var t1 = 0;
        var t2 = 0;
        foreach (var b in borders)
        {
            if (b.Name.Equals(NotConsume1, StringComparison.OrdinalIgnoreCase))
                t1++;
            else if (b.Name.Equals(NotConsume2, StringComparison.OrdinalIgnoreCase))
                t2++;
        }

        return t2 >= 1 || t1 >= 2;
    }

    /// <summary>Divine=3, Annul=2, Ancient=1, none=0.</summary>
    public static int OrbPriority(IReadOnlyList<BorderEffect> borders)
    {
        var best = 0;
        foreach (var b in borders)
        {
            if (b.Name.Equals(RareDivine, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 3);
            else if (b.Name.Equals(RareAnnul, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 2);
            else if (b.Name.Equals(RareAncient, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 1);
        }

        return best;
    }

    /// <summary>
    /// MoreScarabs tier: 3 (best) / 2 / 1, or 0 if none.
    /// MoreScarabs2 and MoreScarabs3 both trigger scarab farm (T1 also, lower priority).
    /// </summary>
    public static int ScarabTier(IReadOnlyList<BorderEffect> borders)
    {
        var best = 0;
        foreach (var b in borders)
        {
            if (b.Name.Equals(MoreScarabs3, StringComparison.OrdinalIgnoreCase) ||
                b.Name.Contains("MoreScarabs3", StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 3);
            else if (b.Name.Equals(MoreScarabs2, StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("MoreScarabs2", StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 2);
            else if (b.Name.Equals(MoreScarabs1, StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("MoreScarabs1", StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 1);
        }

        return best;
    }

    // --- Internals ---

    private static MapPiece TakeBest(
        List<MapPiece> working,
        HashSet<int> used,
        Func<MapPiece, bool> pred,
        Func<MapPiece, double> score)
    {
        return working
            .Where(p => !used.Contains(p.Id) && pred(p))
            .OrderByDescending(score)
            .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
            .FirstOrDefault();
    }

    private static int RemoveUnused(List<MapPiece> working, HashSet<int> used, Func<MapPiece, bool> pred)
    {
        var drop = working.Where(p => !used.Contains(p.Id) && pred(p)).Select(p => p.Id).ToList();
        var removed = 0;
        foreach (var id in drop)
        {
            if (working.Count <= 9)
                break;
            working.RemoveAll(p => p.Id == id);
            removed++;
        }

        return removed;
    }

    private static IEnumerable<(int Row, int Col)> FreeNeighbors(
        int row, int col, Func<int, int, bool> cellFree)
    {
        foreach (var (dr, dc) in Ortho)
        {
            var nr = row + dr;
            var nc = col + dc;
            if (nr is < 0 or > 2 || nc is < 0 or > 2) continue;
            if (cellFree(nr, nc))
                yield return (nr, nc);
        }
    }

    private static IReadOnlyList<BorderEffect> BordersAt(
        IReadOnlyList<BorderEffect>[,] tileBorders, int row, int col) =>
        tileBorders?[row, col] ?? [];

    private static IEnumerable<(int Row, int Col)> EnumerateCells()
    {
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            yield return (r, c);
    }
}
