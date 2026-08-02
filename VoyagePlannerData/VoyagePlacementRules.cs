using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepwaterEngagementSuite.VoyagePlannerData;

public static class VoyagePlacementRules
{
    public const string NotConsume1 = "DeepwaterBorderChanceToNotConsumeChart1";
    public const string NotConsume2 = "DeepwaterBorderChanceToNotConsumeChart2";
    public const string RareDivine = "DeepwaterBorderRareMonsterDivine";
    public const string RareAnnul = "DeepwaterBorderRareMonsterAnnulment";
    public const string RareAncient = "DeepwaterBorderRareMonsterAncient";
    public const string TreasureAnchors1 = "DeepwaterBorderTreasureAnchors1";
    public const string TreasureAnchors2 = "DeepwaterBorderTreasureAnchors2";

    public const string VoyageIncreasedRareMonsters = "MapDeepwaterChartVoyageIncreasedRareMonsters";
    public const string AdjacentStrongboxesPrefix = "MapDeepwaterChartAdjacentStrongboxes";
    public const string AdjacentStarfishPrefix = "MapDeepwaterChartAdjacentStarfish";
    public const string AdjacentIncreasedRarePrefix = "MapDeepwaterChartAdjacentIncreasedRareMonsters";
    public const string AdjacentDivinerBoxPrefix = "MapDeepwaterChartAdjacentDivinerBox";
    public const string AdjacentArcanistBoxPrefix = "MapDeepwaterChartAdjacentArcanistBox";
    public const string AdjacentOperativeBoxPrefix = "MapDeepwaterChartAdjacentOperativeBox";
    public const string AdjacentLostMessagePrefix = "MapDeepwaterChartAdjacentLostMessage";
    public const string AdjacentUniqueAmuletPrefix = "MapDeepwaterChartAdjacentUniqueAmulet";

    public const int CenterRow = 1;
    public const int CenterCol = 1;

    public const int MaxSavedBoxes = 6;
    public const int MaxSavedStarfish = 6;
    public const int MaxSavedRareVoyage = 5;
    public const int MaxSavedPelagic = 2;
    public const int MaxSavedUniqueAmulet2 = 1;
    public const int MaxSavedClamsForAmulet = 3;

    public const string PelagicRoomName = "Pelagic Abyss";
    public const string ClamRoomName = "Clam-infested Shelf";
    public const string AnchorfieldRoomName = "Anchorfield";
    public const string KisharaRoomName = "Kishara's Rest";

    private static readonly (int Dr, int Dc)[] Ortho = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    public sealed record Result(
        List<MapPiece> Pieces,
        List<LockedPlacement> Locks,
        int SavedPelagicCount,
        int SavedFarmCount,
        int SavedStrongboxCount,
        int SavedStarfishCount,
        int SavedRareVoyageCount,
        int SavedAdjacentRareCount,
        int SavedOperativeBoxCount,
        int SavedLostMessageCount,
        int SavedKisharaCount,
        int SavedClamCount,
        int SavedUniqueAmuletCount,
        bool AmuletClamHubActive = false);

    public static readonly (int Row, int Col)[] SacrificeCorners = [(2, 0), (2, 2), (0, 2)];

    public static bool IsSacrificeCorner(int row, int col) =>
        (row, col) is (2, 0) or (2, 2) or (0, 2);

    public static Result Apply(
        IReadOnlyList<MapPiece> pieces,
        IReadOnlyList<BorderEffect>[,] tileBorders,
        VoyageStrategyOptions options = null)
    {
        options ??= VoyageStrategyOptions.AllEnabled;
        var working = pieces.ToList();
        var locks = new List<LockedPlacement>();
        var usedPieceIds = new HashSet<int>();
        var lockedCells = new HashSet<(int Row, int Col)>();

        void LockCell(int row, int col, MapPiece piece, int? rotation = null)
        {
            if (!usedPieceIds.Add(piece.Id))
                return;
            locks.Add(new LockedPlacement(row, col, piece.Id, rotation));
            lockedCells.Add((row, col));
        }

        bool CellFree(int row, int col) => !lockedCells.Contains((row, col));

        var savedKishara = 0;
        if (options.SaveKishara)
        {
            foreach (var boss in working.Where(IsKishara).Select(p => p.Id).ToList())
            {
                if (!TrySavePiece(working, boss))
                    break;
                savedKishara++;
            }
        }

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

        var orbCenters = divineCenters.Select(c => (c.Row, c.Col, Priority: 3))
            .Concat(annulCenters.Select(c => (c.Row, c.Col, Priority: 2)))
            .Concat(ancientCenters.Select(c => (c.Row, c.Col, Priority: 1)))
            .OrderByDescending(x => x.Priority)
            .ToList();

        var clamCountAtStart = working.Count(p => !usedPieceIds.Contains(p.Id) && IsClamChart(p));
        var surplusClams = clamCountAtStart > MaxSavedClamsForAmulet;
        var hasOrbs = orbCenters.Count > 0;
        var strongTreasure = BoardHasStrongTreasureAnchors(tileBorders);

        var amuletCrossLocked = false;
        if (options.UniqueAmuletClamCross &&
            !strongTreasure &&
            !hasOrbs &&
            CellFree(CenterRow, CenterCol))
        {
            amuletCrossLocked = TryLockAmuletClamHub(
                working, usedPieceIds, CellFree, LockCell);
        }

        var savedPelagic = 0;
        if (options.PelagicOnOrbs)
        {
            foreach (var pelagic in working.Where(IsPelagic)
                         .OrderByDescending(p => p.LocalModifier + p.GlobalModifier).ToList())
            {
                if (usedPieceIds.Contains(pelagic.Id))
                    continue;

                var target = orbCenters.FirstOrDefault(c => CellFree(c.Row, c.Col));
                if (target.Priority > 0)
                {
                    LockCell(target.Row, target.Col, pelagic);
                    orbCenters.RemoveAll(c => c.Row == target.Row && c.Col == target.Col);
                }
                else if (savedPelagic < MaxSavedPelagic && TrySavePiece(working, pelagic.Id))
                {
                    savedPelagic++;
                }
            }
        }

        if (options.DivineSupportRing)
        {
            foreach (var center in divineCenters)
            {
                foreach (var n in FreeNeighbors(center.Row, center.Col, CellFree))
                {
                    var support = TakeBest(working, usedPieceIds, IsStrongboxCountChart, BoxValue1Score)
                                  ?? TakeBest(working, usedPieceIds, IsStarfishChart, StarfishScore)
                                  ?? TakeBest(working, usedPieceIds, IsOrbRareComboChart, OrbRareComboScore);
                    if (support == null) break;
                    LockCell(n.Row, n.Col, support);
                }
            }
        }

        if (options.AnnulSupportRing)
        {
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
        }

        if (options.AncientSupportRing)
        {
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
        }

        if (options.CenterSpecialty && CellFree(CenterRow, CenterCol))
        {
            var centerPiece = TakeBest(working, usedPieceIds, IsOperativeBoxChart, OperativeBoxScore)
                              ?? TakeBest(working, usedPieceIds, IsLostMessageChart, LostMessageScore)
                              ?? TakeBest(working, usedPieceIds, IsUniqueAmulet1Chart, UniqueAmuletScore);
            if (centerPiece != null)
                LockCell(CenterRow, CenterCol, centerPiece);
        }

        if (options.DivineRareFill && divineCenters.Count > 0)
        {
            foreach (var cell in EnumerateCells().Where(c => CellFree(c.Row, c.Col)))
            {
                var rare = TakeBest(working, usedPieceIds, IsOrbRareGlobalChart, OrbRareComboScore);
                if (rare == null)
                    break;
                LockCell(cell.Row, cell.Col, rare);
            }
        }

        if (options.NoConsumeAnchorfield &&
            !strongTreasure &&
            !hasOrbs &&
            !amuletCrossLocked &&
            locks.Count == 0)
        {
            foreach (var cell in EnumerateCells().Where(c =>
                         CellFree(c.Row, c.Col) &&
                         IsStrongNoConsume(BordersAt(tileBorders, c.Row, c.Col))))
            {
                var farm = TakeBest(working, usedPieceIds, IsAnchorfieldChart, FarmPriority);
                if (farm == null && surplusClams)
                    farm = TakeBest(working, usedPieceIds, IsClamChart, ClamScore);
                if (farm == null) break;
                LockCell(cell.Row, cell.Col, farm);
            }
        }

        var savedFarm = options.SaveAnchorfield
            ? RemoveUnused(working, usedPieceIds, IsAnchorfieldChart, FarmPriority)
            : 0;
        var savedStrongbox = options.SaveStrongboxes
            ? RemoveUnused(working, usedPieceIds, IsStrongboxCountChart,
                BoxValue1Score, maxSave: MaxSavedBoxes)
            : 0;

        var savedStarfish = 0;
        var savedAdjacentRare = 0;
        if (options.SaveStarfish || options.SaveAdjacentRare)
        {
            if (options.SaveStarfish)
            {
                savedStarfish = RemoveUnused(working, usedPieceIds, IsStarfishChart,
                    StarfishScore, maxSave: MaxSavedStarfish);
            }

            if (options.SaveAdjacentRare)
            {
                var supportSlotsLeft = Math.Max(0, MaxSavedStarfish - savedStarfish);
                if (supportSlotsLeft > 0)
                {
                    savedAdjacentRare = RemoveUnused(working, usedPieceIds, IsAdjacentRareSaveChart,
                        AdjacentRareScore, maxSave: supportSlotsLeft);
                }
            }
        }

        var savedRareVoyage = options.SaveRareVoyage
            ? RemoveUnused(working, usedPieceIds, IsRareVoyageChart,
                RareVoyageScore, maxSave: MaxSavedRareVoyage)
            : 0;
        var savedOperative = options.SaveOperative
            ? RemoveUnused(working, usedPieceIds, IsOperativeBoxChart)
            : 0;
        var savedLostMessage = options.SaveLostMessage
            ? RemoveUnused(working, usedPieceIds, IsLostMessageChart)
            : 0;

        var savedUniqueAmulet = 0;
        var savedClam = 0;
        savedUniqueAmulet = RemoveUnused(working, usedPieceIds, IsUniqueAmulet2Chart,
            UniqueAmuletScore, force: true);
        if (options.SaveUniqueAmuletAndClams && options.UniqueAmuletClamCross && !amuletCrossLocked)
        {
            savedClam = RemoveUnused(working, usedPieceIds, IsClamChart, ClamScore,
                maxSave: MaxSavedClamsForAmulet, force: true);
        }

        if (surplusClams)
        {
            savedClam += RemoveUnused(working, usedPieceIds, IsClamChart, ClamScore, force: true);
        }

        return new Result(
            working, locks,
            savedPelagic, savedFarm, savedStrongbox, savedStarfish, savedRareVoyage,
            savedAdjacentRare, savedOperative, savedLostMessage, savedKishara,
            savedClam, savedUniqueAmulet,
            AmuletClamHubActive: amuletCrossLocked);
    }

    private static bool BoardHasStrongTreasureAnchors(IReadOnlyList<BorderEffect>[,] tileBorders)
    {
        var treasureT1 = 0;
        var treasureT2 = 0;
        foreach (var (row, col) in EnumerateCells())
        {
            foreach (var b in BordersAt(tileBorders, row, col))
            {
                if (b.Name.Equals(TreasureAnchors1, StringComparison.OrdinalIgnoreCase))
                    treasureT1++;
                else if (b.Name.Equals(TreasureAnchors2, StringComparison.OrdinalIgnoreCase))
                    treasureT2++;
            }
        }

        return IsStrongTreasureAnchorsCounts(treasureT1, treasureT2);
    }

    private static bool TryLockAmuletClamHub(
        List<MapPiece> working,
        HashSet<int> usedPieceIds,
        Func<int, int, bool> cellFree,
        Action<int, int, MapPiece, int?> lockCell)
    {
        var amulet2 = TakeBest(working, usedPieceIds, IsUniqueAmulet2Chart, UniqueAmuletScore);
        if (amulet2 == null)
            return false;

        var freeOrtho = FreeNeighbors(CenterRow, CenterCol, cellFree).ToList();
        if (freeOrtho.Count < MaxSavedClamsForAmulet)
            return false;

        var clamSlots = freeOrtho
            .OrderBy(c => c.Row == CenterRow - 1 && c.Col == CenterCol ? 1 : 0)
            .Take(MaxSavedClamsForAmulet)
            .ToList();

        var clams = working
            .Where(p => !usedPieceIds.Contains(p.Id) && IsClamChart(p))
            .OrderByDescending(ClamScore)
            .ThenByDescending(p => p.LocalModifier + p.GlobalModifier)
            .Take(MaxSavedClamsForAmulet)
            .ToList();
        if (clams.Count < MaxSavedClamsForAmulet)
            return false;

        lockCell(CenterRow, CenterCol, amulet2, null);
        for (var i = 0; i < MaxSavedClamsForAmulet; i++)
            lockCell(clamSlots[i].Row, clamSlots[i].Col, clams[i], null);
        return true;
    }

    public static bool IsClamChart(MapPiece piece) =>
        piece.Name.Contains(ClamRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsAnchorfieldChart(MapPiece piece) =>
        piece.Name.Contains(AnchorfieldRoomName, StringComparison.OrdinalIgnoreCase);

    public static bool IsFarmChart(MapPiece piece) =>
        IsClamChart(piece) || IsAnchorfieldChart(piece);

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

    public static bool IsStrongboxCountChart(MapPiece piece) =>
        IsAdjacentStrongboxesChart(piece) || IsPremiumBoxChart(piece);

    public static bool IsOperativeBoxChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentOperativeBoxPrefix));

    public static bool IsStarfishChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentStarfishPrefix));

    public static bool IsAdjacentRareChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentIncreasedRarePrefix));

    public static bool IsAdjacentRareSaveChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, AdjacentIncreasedRarePrefix) &&
            TierFromFamily(m.Name, AdjacentIncreasedRarePrefix) >= 2);

    public static bool IsRareVoyageChart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            m.Name.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase));

    public static bool IsLostMessageChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentLostMessagePrefix));

    public static bool IsUniqueAmuletChart(MapPiece piece) =>
        piece.Modifiers.Any(m => IsFamily(m.Name, AdjacentUniqueAmuletPrefix));

    public static bool IsUniqueAmulet1Chart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, AdjacentUniqueAmuletPrefix) &&
            TierFromFamily(m.Name, AdjacentUniqueAmuletPrefix) == 1);

    public static bool IsUniqueAmulet2Chart(MapPiece piece) =>
        piece.Modifiers.Any(m =>
            IsFamily(m.Name, AdjacentUniqueAmuletPrefix) &&
            TierFromFamily(m.Name, AdjacentUniqueAmuletPrefix) == 2);

    public static bool IsOrbRareGlobalChart(MapPiece piece) =>
        IsRareVoyageChart(piece);

    public static bool IsOrbRareComboChart(MapPiece piece) =>
        IsAdjacentRareChart(piece) || IsRareVoyageChart(piece);

    public static bool IsSpecialtyComboModifier(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return IsFamily(rawName, AdjacentStrongboxesPrefix)
               || IsFamily(rawName, AdjacentDivinerBoxPrefix)
               || IsFamily(rawName, AdjacentArcanistBoxPrefix)
               || IsFamily(rawName, AdjacentOperativeBoxPrefix)
               || IsFamily(rawName, AdjacentStarfishPrefix)
               || IsFamily(rawName, AdjacentLostMessagePrefix)
               || (IsFamily(rawName, AdjacentUniqueAmuletPrefix) &&
                   TierFromFamily(rawName, AdjacentUniqueAmuletPrefix) == 2);
    }

    public static bool IsIncreasedRareStrategyModifier(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        if (rawName.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
            return true;
        return IsFamily(rawName, AdjacentIncreasedRarePrefix) &&
               TierFromFamily(rawName, AdjacentIncreasedRarePrefix) >= 2;
    }

    public static bool HasStrategyOrb(IEnumerable<string> borderNames)
    {
        if (borderNames == null)
            return false;
        foreach (var name in borderNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;
            if (name.Equals(RareDivine, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(RareAnnul, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(RareAncient, StringComparison.OrdinalIgnoreCase))
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
                if (IsFamily(raw, AdjacentLostMessagePrefix) ||
                    IsFamily(raw, AdjacentOperativeBoxPrefix) ||
                    (IsFamily(raw, AdjacentUniqueAmuletPrefix) &&
                     TierFromFamily(raw, AdjacentUniqueAmuletPrefix) == 2))
                {
                    always = true;
                    break;
                }
            }

            if (always)
                marked.Add(i);

            var starfishV = MaxFamilyValue1(mods, AdjacentStarfishPrefix);
            if (starfishV > 0)
                starfish.Add((i, starfishV));

            var adjRareTier = 0;
            foreach (var (raw, _) in mods)
            {
                if (IsFamily(raw, AdjacentIncreasedRarePrefix))
                    adjRareTier = Math.Max(adjRareTier, TierFromFamily(raw, AdjacentIncreasedRarePrefix));
            }

            if (adjRareTier >= 2)
                adjRareT2.Add((i, adjRareTier * 1_000.0));

            foreach (var (raw, _) in mods)
            {
                if (raw.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
                {
                    voyageRare.Add((i, 1));
                    break;
                }
            }

            var boxV = BoxPoolValue1(mods);
            if (boxV > 0)
                boxes.Add((i, boxV));
        }

        var supportMarked = 0;
        foreach (var (index, _) in starfish
                     .OrderByDescending(x => x.Value1)
                     .Take(MaxSavedStarfish))
        {
            marked.Add(index);
            supportMarked++;
        }

        var rareSlots = Math.Max(0, MaxSavedStarfish - supportMarked);
        if (rareSlots > 0)
        {
            foreach (var (index, _) in adjRareT2
                         .OrderByDescending(x => x.Score)
                         .Take(rareSlots))
                marked.Add(index);
        }

        foreach (var (index, _) in voyageRare.Take(MaxSavedRareVoyage))
            marked.Add(index);

        foreach (var (index, _) in boxes
                     .OrderByDescending(x => x.Value1)
                     .Take(MaxSavedBoxes))
            marked.Add(index);

        return marked;
    }

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

    public static bool IsStrategyBorder(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return rawName.Equals(RareDivine, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(RareAnnul, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(RareAncient, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTreasureAnchorsBorder(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        return rawName.Equals(TreasureAnchors1, StringComparison.OrdinalIgnoreCase)
               || rawName.Equals(TreasureAnchors2, StringComparison.OrdinalIgnoreCase);
    }

    private static double FarmPriority(MapPiece p) =>
        p.LocalModifier + p.GlobalModifier;

    private static double ClamScore(MapPiece p) =>
        p.LocalModifier + p.GlobalModifier;

    private static double BoxValue1Score(MapPiece p) =>
        Math.Max(
            MaxFamilyValue1Score(p, AdjacentStrongboxesPrefix),
            Math.Max(
                MaxFamilyValue1Score(p, AdjacentDivinerBoxPrefix),
                MaxFamilyValue1Score(p, AdjacentArcanistBoxPrefix)));

    private static double OperativeBoxScore(MapPiece p) =>
        MaxFamilyValue1Score(p, AdjacentOperativeBoxPrefix);

    private static double StarfishScore(MapPiece p) =>
        MaxFamilyValue1Score(p, AdjacentStarfishPrefix);

    private static double AdjacentRareScore(MapPiece p) =>
        MaxFamilyTierScore(p, AdjacentIncreasedRarePrefix);

    private static double RareVoyageScore(MapPiece p) =>
        p.Modifiers.Where(m => m.Name.Equals(VoyageIncreasedRareMonsters, StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Weight);

    private static double LostMessageScore(MapPiece p) =>
        MaxFamilyTierScore(p, AdjacentLostMessagePrefix);

    private static double UniqueAmuletScore(MapPiece p) =>
        MaxFamilyTierScore(p, AdjacentUniqueAmuletPrefix);

    private static double OrbRareComboScore(MapPiece p)
    {
        double score = 0;
        if (IsAdjacentRareChart(p))
            score = Math.Max(score, AdjacentRareScore(p) + 2_000);
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

    private static double MaxFamilyValue1Score(MapPiece p, string prefix)
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

    private static int MaxFamilyValue1(IEnumerable<(string Name, int Value1)> mods, string prefix)
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

    private static int BoxPoolValue1(IEnumerable<(string Name, int Value1)> mods) =>
        Math.Max(
            MaxFamilyValue1(mods, AdjacentStrongboxesPrefix),
            Math.Max(
                MaxFamilyValue1(mods, AdjacentDivinerBoxPrefix),
                MaxFamilyValue1(mods, AdjacentArcanistBoxPrefix)));

    private static bool IsFamily(string rawName, string prefix) =>
        !string.IsNullOrEmpty(rawName) &&
        rawName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static int TierFromFamily(string rawName, string prefix)
    {
        if (!IsFamily(rawName, prefix) || rawName.Length <= prefix.Length)
            return 0;
        return int.TryParse(rawName.AsSpan(prefix.Length), out var tier) ? tier : 0;
    }

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
            if (name.Equals(TreasureAnchors1, StringComparison.OrdinalIgnoreCase))
                t1++;
            else if (name.Equals(TreasureAnchors2, StringComparison.OrdinalIgnoreCase))
                t2++;
        }

        return IsStrongTreasureAnchorsCounts(t1, t2);
    }

    public static bool IsStrongTreasureAnchors(IReadOnlyList<BorderEffect> borders) =>
        IsStrongTreasureAnchors(borders?.Select(b => b.Name));

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

    private static bool TrySavePiece(List<MapPiece> working, int pieceId, bool force = false)
    {
        if (!force && working.Count <= 9)
            return false;
        return working.RemoveAll(p => p.Id == pieceId) > 0;
    }

    private static int RemoveUnused(
        List<MapPiece> working,
        HashSet<int> used,
        Func<MapPiece, bool> pred,
        Func<MapPiece, double> score = null,
        int? maxSave = null,
        bool force = false)
    {
        IEnumerable<MapPiece> candidates = working.Where(p => !used.Contains(p.Id) && pred(p));
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
            if (!TrySavePiece(working, id, force))
                break;
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
