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
    // --- Strategy borders (exact ids) ---
    public const string NotConsume1 = "DeepwaterBorderChanceToNotConsumeChart1";
    public const string NotConsume2 = "DeepwaterBorderChanceToNotConsumeChart2";
    public const string RareDivine = "DeepwaterBorderRareMonsterDivine";
    public const string RareAnnul = "DeepwaterBorderRareMonsterAnnulment";
    public const string RareAncient = "DeepwaterBorderRareMonsterAncient";
    public const string MoreScarabs2 = "DeepwaterBorderMoreScarabs2";
    public const string MoreScarabs3 = "DeepwaterBorderMoreScarabs3";

    // --- Chart mods (exact ids / families) ---
    // Only global rare combo:
    public const string VoyageIncreasedRareMonsters = "MapDeepwaterChartVoyageIncreasedRareMonsters";
    // Adjacent families (trailing tier digit, higher better):
    public const string AdjacentStrongboxesPrefix = "MapDeepwaterChartAdjacentStrongboxes"; // 1 < 2 < 3
    public const string AdjacentStarfishPrefix = "MapDeepwaterChartAdjacentStarfish"; // 1 < 2
    public const string AdjacentIncreasedRarePrefix = "MapDeepwaterChartAdjacentIncreasedRareMonsters"; // 1 < 2
    public const string AdjacentDivinerBoxPrefix = "MapDeepwaterChartAdjacentDivinerBox"; // 1 < 2
    public const string AdjacentArcanistBoxPrefix = "MapDeepwaterChartAdjacentArcanistBox"; // 1 < 2
    public const string AdjacentFractured = "MapDeepwaterChartAdjacentFractured";

    // DeepwaterChart.Room.Name values
    public const string PelagicRoomName = "Pelagic Abyss";
    public const string ClamRoomName = "Clam-infested Shelf";
    public const string AnchorfieldRoomName = "Anchorfield";
    public const string KisharaRoomName = "Kishara's Rest";

    private static readonly (int Dr, int Dc)[] Ortho = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    public sealed record Result(
        List<MapPiece> Pieces,
        List<LockedPlacement> Locks,
        int SavedPelagicCount,
        int SavedStrongboxCount,
        int SavedStarfishCount,
        int SavedRareVoyageCount,
        int SavedAdjacentRareCount,
        int SavedAdjacentFracturedCount,
        int SavedKisharaCount);

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

        // --- 0. Boss chart: always hold for human placement (never auto-lock / solve) ---
        var savedKishara = 0;
        foreach (var boss in working.Where(IsKishara).Select(p => p.Id).ToList())
        {
            working.RemoveAll(p => p.Id == boss);
            savedKishara++;
        }

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

        // --- 2. Divine surrounds: strongbox > starfish > orb rare combo ---
        foreach (var center in divineCenters)
        {
            foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
            {
                var support = TakeBest(working, usedPieceIds, IsStrongboxCountChart, StrongboxCountScore)
                              ?? TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                              ?? TakeBest(working, usedPieceIds, IsOrbRareComboChart, OrbRareComboScore);
                if (support == null) break;
                LockCell(n.Row, n.Col, support);
            }
        }

        // --- 3. Divine free tiles: voyage rare / rare fracture > solver ---
        if (divineCenters.Count > 0)
        {
            foreach (var cell in EnumerateCells().Where(c => CellFree(c.Row, c.Col)))
            {
                // Globals only — adjacent rares need a neighbor seat on the orb.
                var rare = TakeBest(working, usedPieceIds, IsOrbRareGlobalChart, OrbRareComboScore);
                if (rare == null)
                    break; // remaining free cells → solver
                LockCell(cell.Row, cell.Col, rare);
            }
        }

        // --- 4. Annul surrounds (above scarab): starfish > orb rare combo ---
        foreach (var center in annulCenters)
        {
            foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
            {
                var support = TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                              ?? TakeBest(working, usedPieceIds, IsOrbRareComboChart, OrbRareComboScore);
                if (support == null) break;
                LockCell(n.Row, n.Col, support);
            }
        }

        // --- 5. Scarab surrounds (above Ancient): AdjacentStrongboxesN ---
        // Same mod family as divine strongbox support (MapDeepwaterChartAdjacentStrongboxes1/2/…).
        // Divine already took first pick; leftovers go here. Round-robin neighbors across scarab tiles.
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

                    var box = TakeBest(working, usedPieceIds, IsAdjacentStrongboxesChart, AdjacentStrongboxesScore);
                    if (box == null)
                    {
                        pending.Clear();
                        break;
                    }

                    LockCell(target.Value.Row, target.Value.Col, box);
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

        // --- 6. Ancient surrounds (below scarab): starfish > orb rare combo ---
        foreach (var center in ancientCenters)
        {
            foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
            {
                var support = TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                              ?? TakeBest(working, usedPieceIds, IsOrbRareComboChart, OrbRareComboScore);
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
        // Combo pieces for orb / scarab boards: hold off the pool so the solver cannot
        // waste them on filler tiles (or on boards with no matching border).
        var savedStrongbox = RemoveUnused(working, usedPieceIds, IsStrongboxCountChart);
        var savedStarfish = RemoveUnused(working, usedPieceIds, IsStarfishChart);
        var savedAdjacentRare = RemoveUnused(working, usedPieceIds, IsAdjacentRareChart);
        var savedRareVoyage = RemoveUnused(working, usedPieceIds, IsRareVoyageChart);
        var savedAdjacentFractured = RemoveUnused(working, usedPieceIds, IsAdjacentFracturedChart);

        return new Result(
            working, locks,
            savedPelagic, savedStrongbox, savedStarfish, savedRareVoyage,
            savedAdjacentRare, savedAdjacentFractured, savedKishara);
    }

    // --- Chart classification (exact ids from game data) ---

    public static bool IsFarmChart(MapPiece piece) =>
        piece.Name.Contains(ClamRoomName, StringComparison.OrdinalIgnoreCase) ||
        piece.Name.Contains(AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsPelagic(MapPiece piece) =>
        piece.Name.Contains(PelagicRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsKishara(MapPiece piece) =>
        piece.Name.Contains(KisharaRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsAdjacentStrongboxesChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentStrongboxesPrefix));

    public static bool IsPremiumBoxChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, AdjacentDivinerBoxPrefix) ||
            IsFamily(m.Name, AdjacentArcanistBoxPrefix));

    /// <summary>Divine strongbox support: AdjacentStrongboxesN + Diviner/Arcanist boxes.</summary>
    public static bool IsStrongboxCountChart(MapPiece piece) =>
        IsAdjacentStrongboxesChart(piece) || IsPremiumBoxChart(piece);

    public static bool IsStarfishChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentStarfishPrefix));

    public static bool IsAdjacentRareChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentIncreasedRarePrefix));

    /// <summary>Only global rare combo: MapDeepwaterChartVoyageIncreasedRareMonsters.</summary>
    public static bool IsRareVoyageChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase));

    public static bool IsAdjacentFracturedChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(AdjacentFractured, StringComparison.OrdinalIgnoreCase));

    /// <summary>Global only — free tiles next to divine / any orb seat.</summary>
    public static bool IsOrbRareGlobalChart(MapPiece piece) =>
        IsRareVoyageChart(piece);

    /// <summary>Adjacent rares + fractured + voyage global on Divine/Annul/Ancient surrounds.</summary>
    public static bool IsOrbRareComboChart(MapPiece piece) =>
        IsAdjacentRareChart(piece) || IsAdjacentFracturedChart(piece) || IsRareVoyageChart(piece);

    /// <summary>Chart mods we lock/reserve — exact known families only.</summary>
    public static bool IsSpecialtyComboModifier(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return IsFamily(rawName, AdjacentStrongboxesPrefix)
               || IsFamily(rawName, AdjacentDivinerBoxPrefix)
               || IsFamily(rawName, AdjacentArcanistBoxPrefix)
               || IsFamily(rawName, AdjacentStarfishPrefix)
               || IsFamily(rawName, AdjacentIncreasedRarePrefix)
               || rawName.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(AdjacentFractured, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Room names we lock/save (Pelagic, boss, Clam farm, Anchorfield farm).</summary>
    public static bool TrySpecialtyRoomLabel(string roomName, out string label)
    {
        label = null;
        if (string.IsNullOrEmpty(roomName))
            return false;
        if (roomName.Contains(PelagicRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Pelagic";
            return true;
        }

        if (roomName.Contains(KisharaRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Kishara";
            return true;
        }

        if (roomName.Contains(AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Anchorfield";
            return true;
        }

        if (roomName.Contains(ClamRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Clam Shelf";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Borders that drive placement strategy / combo draw:
    /// orbs, no-consume, MoreScarabs2/3 only (T1 ignored for scarab combo).
    /// </summary>
    public static bool IsStrategyBorder(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return rawName.Equals(NotConsume1, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(NotConsume2, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(RareDivine, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(RareAnnul, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(RareAncient, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(MoreScarabs2, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(MoreScarabs3, StringComparison.OrdinalIgnoreCase);
    }

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

    private static double AdjacentStrongboxesScore(MapPiece p) =>
        MaxFamilyTierScore(p, AdjacentStrongboxesPrefix);

    private static double StrongboxCountScore(MapPiece p)
    {
        // Prefer AdjacentStrongboxes 1<2<3, then Diviner/Arcanist 1<2.
        var adj = AdjacentStrongboxesScore(p);
        if (adj > 0)
            return adj + 10_000;

        return Math.Max(
            MaxFamilyTierScore(p, AdjacentDivinerBoxPrefix),
            MaxFamilyTierScore(p, AdjacentArcanistBoxPrefix));
    }

    private static double StarfishScore(MapPiece p) =>
        MaxFamilyTierScore(p, AdjacentStarfishPrefix);

    private static double AdjacentRareScore(MapPiece p) =>
        MaxFamilyTierScore(p, AdjacentIncreasedRarePrefix);

    private static double RareVoyageScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    private static double AdjacentFracturedScore(MapPiece p) =>
        p.Modifiers.Any(m => m.Name.Equals(AdjacentFractured, StringComparison.OrdinalIgnoreCase))
            ? 1 + p.LocalModifier + p.GlobalModifier
            : 0;

    /// <summary>Orb surround last-pick: adj rares (tiered), fractured, voyage global by weight.</summary>
    private static double OrbRareComboScore(MapPiece p)
    {
        double score = 0;
        if (IsAdjacentRareChart(p))
            score = Math.Max(score, AdjacentRareScore(p) + 2_000);
        if (IsAdjacentFracturedChart(p))
            score = Math.Max(score, AdjacentFracturedScore(p) + 1_000);
        if (IsRareVoyageChart(p))
            score = Math.Max(score, RareVoyageScore(p));
        return score;
    }

    private static double MaxFamilyTierScore(MapPiece p, string prefix)
    {
        double best = 0;
        foreach (var m in p.Modifiers)
        {
            if (!IsFamily(m.Name, prefix))
                continue;
            best = Math.Max(best, TierFromFamily(m.Name, prefix) * 1_000 + m.Weight);
        }

        return best;
    }

    private static bool IsFamily(string rawName, string prefix) =>
        !string.IsNullOrEmpty(rawName) &&
        rawName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>…Strongboxes2 → 2; exact non-tiered ids → 0.</summary>
    private static int TierFromFamily(string rawName, string prefix)
    {
        if (!IsFamily(rawName, prefix) || rawName.Length <= prefix.Length)
            return 0;
        return int.TryParse(rawName.AsSpan(prefix.Length), out var tier) ? tier : 0;
    }

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
    /// MoreScarabs combo: only T2 and T3 fire placement (3 > 2). T1 ignored.
    /// </summary>
    public static int ScarabTier(IReadOnlyList<BorderEffect> borders)
    {
        var best = 0;
        foreach (var b in borders)
        {
            if (b.Name.Equals(MoreScarabs3, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 3);
            else if (b.Name.Equals(MoreScarabs2, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 2);
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
