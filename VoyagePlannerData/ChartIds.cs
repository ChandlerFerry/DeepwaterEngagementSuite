namespace DeepwaterEngagementSuite.VoyagePlannerData;

public static class ChartIds
{
    public const string NotConsume1 = "DeepwaterBorderChanceToNotConsumeChart1";
    public const string NotConsume2 = "DeepwaterBorderChanceToNotConsumeChart2";
    public const string RareDivine = "DeepwaterBorderRareMonsterDivine";
    public const string RareAnnul = "DeepwaterBorderRareMonsterAnnulment";
    public const string RareAncient = "DeepwaterBorderRareMonsterAncient";
    public const string TreasureAnchors1 = "DeepwaterBorderTreasureAnchors1";
    public const string TreasureAnchors2 = "DeepwaterBorderTreasureAnchors2";

    public const string VoyageIncreasedRareMonsters = "MapDeepwaterChartVoyageIncreasedRareMonsters";
    public const string VoyageNoEquipmentDrops = "MapDeepwaterChartVoyageNoEquipmentDrops";
    public const string VoyageSoulEater = "MapDeepwaterChartVoyageSoulEater";
    public const string VoyageRareFracture = "MapDeepwaterChartVoyageRareFracture";
    public const string VoyageMonstersPossessed = "MapDeepwaterChartVoyageMonstersPossessed";

    public const string AdjacentFracturedPrefix = "MapDeepwaterChartAdjacentFractured";
    public const string AdjacentGoldenLanternsPrefix = "MapDeepwaterChartAdjacentGoldenLanterns";
    public const string AdjacentPantheonPrefix = "MapDeepwaterChartAdjacentPantheon";
    public const string AdjacentStrongboxesPrefix = "MapDeepwaterChartAdjacentStrongboxes";
    public const string AdjacentStarfishPrefix = "MapDeepwaterChartAdjacentStarfish";
    public const string AdjacentIncreasedRarePrefix = "MapDeepwaterChartAdjacentIncreasedRareMonsters";
    public const string AdjacentDivinerBoxPrefix = "MapDeepwaterChartAdjacentDivinerBox";
    public const string AdjacentArcanistBoxPrefix = "MapDeepwaterChartAdjacentArcanistBox";
    public const string AdjacentOperativeBoxPrefix = "MapDeepwaterChartAdjacentOperativeBox";
    public const string AdjacentLostMessagePrefix = "MapDeepwaterChartAdjacentLostMessage";
    public const string AdjacentUniqueAmuletPrefix = "MapDeepwaterChartAdjacentUniqueAmulet";
    public const string AdjacentUniqueBeltPrefix = "MapDeepwaterChartAdjacentUniqueBelt";
    public const string AdjacentUniqueRingPrefix = "MapDeepwaterChartAdjacentUniqueRing";

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

    public const double ClamAdjacentToAmuletMultiplier = 1_000_000d;

    public static readonly (int Row, int Col)[] SacrificeCorners = [(2, 0), (2, 2), (0, 2)];
    public static readonly (int Dr, int Dc)[] Ortho = [(1, 0), (-1, 0), (0, 1), (0, -1)];
}
