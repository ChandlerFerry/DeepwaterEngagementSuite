using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData;

public static class ChartPredicates
{
    public static bool IsSacrificeCorner(int row, int col) =>
        (row, col) is (2, 0) or (2, 2) or (0, 2);

    public static bool IsClamChart(MapPiece piece) =>
        piece.Name.Contains(ChartIds.ClamRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsAnchorfieldChart(MapPiece piece) =>
        piece.Name.Contains(ChartIds.AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsFarmChart(MapPiece piece) =>
        IsClamChart(piece) || IsAnchorfieldChart(piece);

    public static bool IsPelagic(MapPiece piece) =>
        piece.Name.Contains(ChartIds.PelagicRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsKishara(MapPiece piece) =>
        piece.Name.Contains(ChartIds.KisharaRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsNoEquipmentChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(ChartIds.VoyageNoEquipmentDrops, StringComparison.OrdinalIgnoreCase));

    public static bool IsSoulEaterChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(ChartIds.VoyageSoulEater, StringComparison.OrdinalIgnoreCase));

    public static bool IsRareFractureChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(ChartIds.VoyageRareFracture, StringComparison.OrdinalIgnoreCase));

    public static bool IsRarePossessedChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(ChartIds.VoyageMonstersPossessed, StringComparison.OrdinalIgnoreCase));

    public static bool IsFracturedChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentFracturedPrefix));

    public static bool IsGoldenLanternsChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentGoldenLanternsPrefix));

    public static bool IsPantheonChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentPantheonPrefix));

    public static bool IsAdjacentStrongboxesChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentStrongboxesPrefix));

    public static bool IsPremiumBoxChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, ChartIds.AdjacentDivinerBoxPrefix) ||
            IsFamily(m.Name, ChartIds.AdjacentArcanistBoxPrefix));

    public static bool IsStrongboxCountChart(MapPiece piece) =>
        IsAdjacentStrongboxesChart(piece) || IsPremiumBoxChart(piece);

    public static bool IsOperativeBoxChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentOperativeBoxPrefix));

    public static bool IsStarfishChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentStarfishPrefix));

    public static bool IsAdjacentRareChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentIncreasedRarePrefix));

    public static bool IsAdjacentRareSaveChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, ChartIds.AdjacentIncreasedRarePrefix) &&
            TierFromFamily(m.Name, ChartIds.AdjacentIncreasedRarePrefix) >= 2);

    public static bool IsRareVoyageChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(ChartIds.VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase));

    public static bool IsLostMessageChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentLostMessagePrefix));

    public static bool IsUniqueAmuletChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentUniqueAmuletPrefix));

    public static bool IsUniqueAmulet1Chart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, ChartIds.AdjacentUniqueAmuletPrefix) &&
            TierFromFamily(m.Name, ChartIds.AdjacentUniqueAmuletPrefix) == 1);

    public static bool IsUniqueAmulet2Chart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, ChartIds.AdjacentUniqueAmuletPrefix) &&
            TierFromFamily(m.Name, ChartIds.AdjacentUniqueAmuletPrefix) == 2);

    public static bool IsUniqueBeltChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentUniqueBeltPrefix));

    public static bool IsUniqueRingChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, ChartIds.AdjacentUniqueRingPrefix));

    public static bool IsCenterOnlyUniqueChart(MapPiece piece) =>
        IsUniqueAmulet2Chart(piece) || IsUniqueBeltChart(piece) || IsUniqueRingChart(piece);

    public static bool IsOrbRareGlobalChart(MapPiece piece) =>
        IsRareVoyageChart(piece);

    public static bool IsOrbRareComboChart(MapPiece piece) =>
        IsAdjacentRareChart(piece) || IsRareVoyageChart(piece);

    public static bool IsSpecialtyComboModifier(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return IsFamily(rawName, ChartIds.AdjacentStrongboxesPrefix)
               || IsFamily(rawName, ChartIds.AdjacentDivinerBoxPrefix)
               || IsFamily(rawName, ChartIds.AdjacentArcanistBoxPrefix)
               || IsFamily(rawName, ChartIds.AdjacentOperativeBoxPrefix)
               || IsFamily(rawName, ChartIds.AdjacentStarfishPrefix)
               || IsFamily(rawName, ChartIds.AdjacentLostMessagePrefix)
               || (IsFamily(rawName, ChartIds.AdjacentUniqueAmuletPrefix) &&
                   TierFromFamily(rawName, ChartIds.AdjacentUniqueAmuletPrefix) == 2)
               || IsFamily(rawName, ChartIds.AdjacentUniqueBeltPrefix)
               || IsFamily(rawName, ChartIds.AdjacentUniqueRingPrefix);
    }

    public static bool IsIncreasedRareStrategyModifier(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        if (rawName.Equals(ChartIds.VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
            return true;
        return IsFamily(rawName, ChartIds.AdjacentIncreasedRarePrefix) &&
               TierFromFamily(rawName, ChartIds.AdjacentIncreasedRarePrefix) >= 2;
    }

    public static bool HasStrategyOrb(IEnumerable<string> borderNames)
    {
        if (borderNames == null)
            return false;
        foreach (var name in borderNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;
            if (name.Equals(ChartIds.RareDivine, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(ChartIds.RareAnnul, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(ChartIds.RareAncient, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsStrategyBorder(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return rawName.Equals(ChartIds.RareDivine, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(ChartIds.RareAnnul, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(ChartIds.RareAncient, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTreasureAnchorsBorder(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return rawName.Equals(ChartIds.TreasureAnchors1, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(ChartIds.TreasureAnchors2, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInfiniteLanternsBorder(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return rawName.Equals(ChartIds.InfiniteLanterns, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsStrongNoConsume(IReadOnlyList<BorderEffect> borders)
    {
        var t1 = 0;
        var t2 = 0;
        if (borders == null)
            return false;
        foreach (var b in borders)
        {
            if (b.Name.Equals(ChartIds.NotConsume1, StringComparison.OrdinalIgnoreCase))
                t1++;
            else if (b.Name.Equals(ChartIds.NotConsume2, StringComparison.OrdinalIgnoreCase))
                t2++;
        }

        return t2 >= 1 || t1 >= 2;
    }

    public static bool IsStrongTreasureAnchorsCounts(int t1, int t2) =>
        (t2 >= 1 && t1 >= 2) || t1 >= 3 || t2 >= 2;

    public static bool IsStrongTreasureAnchors(IEnumerable<string> borderNames)
    {
        var t1 = 0;
        var t2 = 0;
        if (borderNames == null)
            return false;

        foreach (var name in borderNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;
            if (name.Equals(ChartIds.TreasureAnchors1, StringComparison.OrdinalIgnoreCase))
                t1++;
            else if (name.Equals(ChartIds.TreasureAnchors2, StringComparison.OrdinalIgnoreCase))
                t2++;
        }

        return IsStrongTreasureAnchorsCounts(t1, t2);
    }

    public static bool IsStrongTreasureAnchors(IReadOnlyList<BorderEffect> borders) =>
        IsStrongTreasureAnchors(borders?.Select(b => b.Name));

    public static bool BoardHasStrongTreasureAnchors(IReadOnlyList<BorderEffect>[,] tileBorders)
    {
        var treasureT1 = 0;
        var treasureT2 = 0;
        foreach (var (row, col) in EnumerateCells())
        {
            foreach (var b in BordersAt(tileBorders, row, col))
            {
                if (b.Name.Equals(ChartIds.TreasureAnchors1, StringComparison.OrdinalIgnoreCase))
                    treasureT1++;
                else if (b.Name.Equals(ChartIds.TreasureAnchors2, StringComparison.OrdinalIgnoreCase))
                    treasureT2++;
            }
        }

        return IsStrongTreasureAnchorsCounts(treasureT1, treasureT2);
    }

    
    public static bool IsStrongInfiniteLanternsCount(int count) => count >= 2;

    public static bool IsStrongInfiniteLanterns(IEnumerable<string> borderNames)
    {
        if (borderNames == null)
            return false;

        var count = 0;
        foreach (var name in borderNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;
            if (name.Equals(ChartIds.InfiniteLanterns, StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return IsStrongInfiniteLanternsCount(count);
    }

    public static bool IsStrongInfiniteLanterns(IReadOnlyList<BorderEffect> borders) =>
        IsStrongInfiniteLanterns(borders?.Select(b => b.Name));

    public static bool BoardHasStrongInfiniteLanterns(IReadOnlyList<BorderEffect>[,] tileBorders)
    {
        var count = 0;
        foreach (var (row, col) in EnumerateCells())
        {
            foreach (var b in BordersAt(tileBorders, row, col))
            {
                if (b.Name.Equals(ChartIds.InfiniteLanterns, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
        }

        return IsStrongInfiniteLanternsCount(count);
    }

    
    public static int InGridDegree(int row, int col)
    {
        var degree = 0;
        foreach (var (dr, dc) in ChartIds.Ortho)
        {
            var nr = row + dr;
            var nc = col + dc;
            if (nr is >= 0 and <= 2 && nc is >= 0 and <= 2)
                degree++;
        }

        return degree;
    }

    public static int OrbPriority(IReadOnlyList<BorderEffect> borders)
    {
        var best = 0;
        foreach (var b in borders)
        {
            if (b.Name.Equals(ChartIds.RareDivine, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 3);
            else if (b.Name.Equals(ChartIds.RareAnnul, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 2);
            else if (b.Name.Equals(ChartIds.RareAncient, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 1);
        }

        return best;
    }

    public static bool TrySpecialtyRoomLabel(string roomName, out string label)
    {
        label = null;
        if (string.IsNullOrEmpty(roomName))
            return false;
        if (roomName.Contains(ChartIds.PelagicRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Pelagic";
            return true;
        }

        if (roomName.Contains(ChartIds.KisharaRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Kishara";
            return true;
        }

        if (roomName.Contains(ChartIds.AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Anchorfield";
            return true;
        }

        if (roomName.Contains(ChartIds.ClamRoomName, StringComparison.OrdinalIgnoreCase))
        {
            label = "Clam Shelf";
            return true;
        }

        return false;
    }

    public static HashSet<int> SelectInventorySpecialtyIndices(
        IReadOnlyList<string> roomNames,
        IReadOnlyList<IReadOnlyList<(string RawName, int Value1)>> modsPerChart)
    {
        var marked = new HashSet<int>();
        var starfish = new List<(int Index, int Value1)>();
        var adjRareT2 = new List<(int Index, double Score)>();
        var voyageRare = new List<(int Index, double Score)>();
        var boxes = new List<(int Index, int Value1)>();
        var count = Math.Min(roomNames.Count, modsPerChart.Count);

        for (var i = 0; i < count; i++)
        {
            if (TrySpecialtyRoomLabel(roomNames[i], out _))
                marked.Add(i);

            var mods = modsPerChart[i];
            if (mods == null || mods.Count == 0)
                continue;

            var always = false;
            foreach (var (raw, _) in mods)
            {
                if (string.IsNullOrEmpty(raw))
                    continue;
                if (IsFamily(raw, ChartIds.AdjacentLostMessagePrefix) ||
                    IsFamily(raw, ChartIds.AdjacentOperativeBoxPrefix) ||
                    (IsFamily(raw, ChartIds.AdjacentUniqueAmuletPrefix) &&
                     TierFromFamily(raw, ChartIds.AdjacentUniqueAmuletPrefix) == 2) ||
                    IsFamily(raw, ChartIds.AdjacentUniqueBeltPrefix) ||
                    IsFamily(raw, ChartIds.AdjacentUniqueRingPrefix))
                {
                    always = true;
                    break;
                }
            }

            if (always)
                marked.Add(i);

            var starfishV = MaxFamilyValue1(mods, ChartIds.AdjacentStarfishPrefix);
            if (starfishV > 0)
                starfish.Add((i, starfishV));

            var adjRareTier = 0;
            foreach (var (raw, _) in mods)
            {
                if (IsFamily(raw, ChartIds.AdjacentIncreasedRarePrefix))
                    adjRareTier = Math.Max(adjRareTier, TierFromFamily(raw, ChartIds.AdjacentIncreasedRarePrefix));
            }

            if (adjRareTier >= 2)
                adjRareT2.Add((i, adjRareTier * 1_000.0));

            foreach (var (raw, _) in mods)
            {
                if (raw.Equals(ChartIds.VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
                {
                    voyageRare.Add((i, 1));
                    break;
                }
            }

            var boxV = BoxPoolValue1(mods);
            if (boxV > 0)
                boxes.Add((i, boxV));
        }

        
        var boxesMarked = 0;
        foreach (var (index, _) in boxes
                     .OrderByDescending(x => x.Value1)
                     .Take(ChartIds.MaxSavedBoxes))
        {
            marked.Add(index);
            boxesMarked++;
        }

        var residual = Math.Max(0, ChartIds.MaxSavedRareMonsterSupport - boxesMarked);
        var starfishMarked = 0;
        foreach (var (index, _) in starfish
                     .OrderByDescending(x => x.Value1)
                     .Take(residual))
        {
            marked.Add(index);
            starfishMarked++;
        }

        var rareSlots = residual - starfishMarked;
        if (rareSlots > 0)
        {
            foreach (var (index, _) in adjRareT2
                         .OrderByDescending(x => x.Score)
                         .Take(rareSlots))
                marked.Add(index);
        }

        
        var extraStarfish = ChartIds.MaxSavedStarfish;
        if (extraStarfish > 0)
        {
            foreach (var (index, _) in starfish
                         .OrderByDescending(x => x.Value1)
                         .Where(x => !marked.Contains(x.Index))
                         .Take(extraStarfish))
                marked.Add(index);
        }

        foreach (var (index, _) in voyageRare.Take(ChartIds.MaxSavedRareVoyage))
            marked.Add(index);

        return marked;
    }

    public static double FarmPriority(MapPiece p) =>
        p.LocalModifier + p.GlobalModifier;

    
    public static double GoldenLanternsSaveScore(MapPiece p)
    {
        var shape = p.Type switch
        {
            PieceType.Tee => 4_000,      
            PieceType.Corner => 3_000,
            PieceType.Cross => 2_000,
            PieceType.Straight => 1_000, 
            PieceType.Single => 0,        
            _ => 0
        };
        return shape + MaxFamilyTierScore(p, ChartIds.AdjacentGoldenLanternsPrefix);
    }

    public static double SoulEaterScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Equals(ChartIds.VoyageSoulEater, StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight)
        + p.LocalModifier + p.GlobalModifier;

    public static double ClamScore(MapPiece p) =>
        p.LocalModifier + p.GlobalModifier;

    public static double BoxValue1Score(MapPiece p) =>
        Math.Max(
            MaxFamilyValue1Score(p, ChartIds.AdjacentStrongboxesPrefix),
            Math.Max(
                MaxFamilyValue1Score(p, ChartIds.AdjacentDivinerBoxPrefix),
                MaxFamilyValue1Score(p, ChartIds.AdjacentArcanistBoxPrefix)));

    public static double OperativeBoxScore(MapPiece p) =>
        MaxFamilyValue1Score(p, ChartIds.AdjacentOperativeBoxPrefix);

    public static double StarfishScore(MapPiece p) =>
        MaxFamilyValue1Score(p, ChartIds.AdjacentStarfishPrefix);

    public static double AdjacentRareScore(MapPiece p) =>
        MaxFamilyTierScore(p, ChartIds.AdjacentIncreasedRarePrefix);

    public static double RareVoyageScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Equals(ChartIds.VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    public static double LostMessageScore(MapPiece p) =>
        MaxFamilyTierScore(p, ChartIds.AdjacentLostMessagePrefix);

    public static double UniqueAmuletScore(MapPiece p) =>
        MaxFamilyTierScore(p, ChartIds.AdjacentUniqueAmuletPrefix);

    public static double UniqueBeltScore(MapPiece p) =>
        MaxFamilyTierScore(p, ChartIds.AdjacentUniqueBeltPrefix);

    public static double UniqueRingScore(MapPiece p) =>
        MaxFamilyTierScore(p, ChartIds.AdjacentUniqueRingPrefix);

    public static double CenterOnlyUniqueScore(MapPiece p)
    {
        if (IsUniqueAmulet2Chart(p))
            return 3_000 + UniqueAmuletScore(p);
        if (IsUniqueBeltChart(p))
            return 2_000 + UniqueBeltScore(p);
        if (IsUniqueRingChart(p))
            return 1_000 + UniqueRingScore(p);
        return 0;
    }

    public static double OrbRareComboScore(MapPiece p)
    {
        double score = 0;
        if (IsAdjacentRareChart(p))
            score = Math.Max(score, AdjacentRareScore(p) + 2_000);
        if (IsRareVoyageChart(p))
            score = Math.Max(score, RareVoyageScore(p));
        return score;
    }

    public static bool IsFamily(string rawName, string prefix) =>
        !string.IsNullOrEmpty(rawName) &&
        rawName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    public static int TierFromFamily(string rawName, string prefix)
    {
        if (!IsFamily(rawName, prefix) || rawName.Length <= prefix.Length)
            return 0;
        return int.TryParse(rawName.AsSpan(prefix.Length), out var tier) ? tier : 0;
    }

    public static double MaxFamilyTierScore(MapPiece p, string prefix)
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

    public static double MaxFamilyValue1Score(MapPiece p, string prefix)
    {
        double best = 0;
        foreach (var m in p.Modifiers)
        {
            if (!IsFamily(m.Name, prefix))
                continue;
            best = Math.Max(best, m.Value1 * 1_000_000.0 + m.Weight);
        }

        return best;
    }

    public static int MaxFamilyValue1(IEnumerable<(string Name, int Value1)> mods, string prefix)
    {
        var best = 0;
        foreach (var m in mods)
        {
            if (!IsFamily(m.Name, prefix))
                continue;
            if (m.Value1 > best)
                best = m.Value1;
        }

        return best;
    }

    public static int BoxPoolValue1(IEnumerable<(string Name, int Value1)> mods) =>
        Math.Max(
            MaxFamilyValue1(mods, ChartIds.AdjacentStrongboxesPrefix),
            Math.Max(
                MaxFamilyValue1(mods, ChartIds.AdjacentDivinerBoxPrefix),
                MaxFamilyValue1(mods, ChartIds.AdjacentArcanistBoxPrefix)));

    public static IReadOnlyList<BorderEffect> BordersAt(
        IReadOnlyList<BorderEffect>[,] tileBorders, int row, int col) =>
        tileBorders?[row, col] ?? [];

    public static IEnumerable<(int Row, int Col)> EnumerateCells()
    {
        for (var r = 0; r < 3; r++)
        for (var c = 0; c < 3; c++)
            yield return (r, c);
    }
}
