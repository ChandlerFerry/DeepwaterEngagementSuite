using System;
using System.Collections.Generic;

namespace DeepwaterEngagementSuite.VoyagePlannerData;


public static class BorderDisplayMap
{
    
    
    
    
    private static readonly (string Needle, string Id)[] PhraseToId =
    [
        ("does not reduce your Lantern count", "DeepwaterBorderInfiniteLanterns"),
        ("Filthscrabble", "DeepwaterBorderGiantOctopus"),
        ("Captainsbane", "DeepwaterBorderCrabMiniboss"),
        ("Pirate's Locker", "DeepwaterBorderRandomDucatChest"),
        ("Brinerot raiding party", "DeepwaterBorderPiratePack"),
        ("Stacked Decks", "DeepwaterBorderCurrencyToStackedDecks"),
        ("Altars to the Goddess", "DeepwaterBorderIzaroObject"),
        ("Golden Lanterns", "DeepwaterBorderGoldenLanterns"),
        ("drop Dead Man's Sulphur", "DeepwaterBorderSulphurDrops"),
        ("are at least Magic", "DeepwaterBorderMonstersAtLeastMagic"),

        ("80% increased explicit modifier", "DeepwaterBorderChartEffect3"),
        ("60% increased explicit modifier", "DeepwaterBorderChartEffect2"),
        ("40% increased explicit modifier", "DeepwaterBorderChartEffect1"),

        ("50% chance to not be consumed", "DeepwaterBorderChanceToNotConsumeChart2"),
        ("30% chance to not be consumed", "DeepwaterBorderChanceToNotConsumeChart1"),

        ("4 additional Treasure Anchors", "DeepwaterBorderTreasureAnchors2"),
        ("2 additional Treasure Anchors", "DeepwaterBorderTreasureAnchors1"),

        ("32% increased Pack Size", "DeepwaterBorderPackSize3"),
        ("24% increased Pack Size", "DeepwaterBorderPackSize2"),
        ("16% increased Pack Size", "DeepwaterBorderPackSize1"),

        ("100% increased number of Rare Monsters", "DeepwaterBorderIncreasedRareMonsters3"),
        ("75% increased number of Rare Monsters", "DeepwaterBorderIncreasedRareMonsters2"),
        ("50% increased number of Rare Monsters", "DeepwaterBorderIncreasedRareMonsters1"),

        ("16 additional packs of Sea Beasts", "DeepwaterBorderAdditionalSeaBeasts3"),
        ("12 additional packs of Sea Beasts", "DeepwaterBorderAdditionalSeaBeasts2"),
        ("8 additional packs of Sea Beasts", "DeepwaterBorderAdditionalSeaBeasts1"),

        ("16 additional packs of Crabs", "DeepwaterBorderAdditionalCrabs3"),
        ("12 additional packs of Crabs", "DeepwaterBorderAdditionalCrabs2"),
        ("8 additional packs of Crabs", "DeepwaterBorderAdditionalCrabs1"),

        ("16 additional packs of the Drowned", "DeepwaterBorderAdditionalDrowned3"),
        ("12 additional packs of the Drowned", "DeepwaterBorderAdditionalDrowned2"),
        ("8 additional packs of the Drowned", "DeepwaterBorderAdditionalDrowned1"),

        
        ("Divine Orb", "DeepwaterBorderRareMonsterDivine"),
        ("Ancient Orb", "DeepwaterBorderRareMonsterAncient"),
        ("Exalted Orb", "DeepwaterBorderRareMonsterExalted"),
        ("Orb of Annulment", "DeepwaterBorderRareMonsterAnnulment"),
        ("Annulment", "DeepwaterBorderRareMonsterAnnulment"),
        ("Chaos Orb", "DeepwaterBorderRareMonsterChaos"),
        ("Vaal Orb", "DeepwaterBorderRareMonsterVaal"),
        ("Gemcutter", "DeepwaterBorderRareMonsterGemcutters"),
        ("Chromatic Orb", "DeepwaterBorderRareMonsterChromatic"),
        ("Orb of Regret", "DeepwaterBorderRareMonsterRegret"),
        ("Blessed Orb", "DeepwaterBorderRareMonsterBlessed"),
        ("Regal Orb", "DeepwaterBorderRareMonsterRegal"),
        ("Support Gem", "DeepwaterBorderRareMonsterSupport"),
        ("drop 1 additional Scarab", "DeepwaterBorderRareMonsterScarab"),
        ("additional Scarabs", "DeepwaterBorderRareMonsterScarab"),

        ("75% increased number of Rare monsters in adjacent Areas per connection",
            "DeepwaterBorderRareMonstersPerConnection2"),
        ("50% increased number of Rare monsters in adjacent Areas per connection",
            "DeepwaterBorderRareMonstersPerConnection1"),
        ("Rare monsters in adjacent Areas per connection",
            "DeepwaterBorderRareMonstersPerConnection1"),

        ("180% increased Quantity of Items found in adjacent Areas",
            "DeepwaterBorderQuantityPerConnection2"),
        ("120% increased Quantity of Items found in adjacent Areas",
            "DeepwaterBorderQuantityPerConnection1"),
        ("reduced quantity of items found in adjacent Areas per connection",
            "DeepwaterBorderQuantityPerConnection1"),

        ("50% of Equipment dropped", "DeepwaterBorderEquipmentToGold2"),
        ("25% of Equipment dropped", "DeepwaterBorderEquipmentToGold1"),

        ("100% more Currency", "DeepwaterBorderMoreCurrency3"),
        ("75% more Currency", "DeepwaterBorderMoreCurrency2"),
        ("50% more Currency", "DeepwaterBorderMoreCurrency1"),

        ("100% more Scarabs", "DeepwaterBorderMoreScarabs3"),
        ("75% more Scarabs", "DeepwaterBorderMoreScarabs2"),
        ("50% more Scarabs", "DeepwaterBorderMoreScarabs1"),

        ("100% more Rarity", "DeepwaterBorderMoreRarity3"),
        ("75% more Rarity", "DeepwaterBorderMoreRarity2"),
        ("50% more Rarity", "DeepwaterBorderMoreRarity1"),

        ("200% increased Experience", "DeepwaterBorderExpGain3"),
        ("150% increased Experience", "DeepwaterBorderExpGain2"),
        ("100% increased Experience", "DeepwaterBorderExpGain1"),

        ("additional modifier", "DeepwaterBorderMagicMonsterMods1"),
    ];

    
    public static string TryResolveId(string displayText)
    {
        if (string.IsNullOrWhiteSpace(displayText))
            return null;

        var text = displayText.Trim();

        
        string bestId = null;
        var bestLen = -1;
        foreach (var (needle, id) in PhraseToId)
        {
            if (needle.Length <= bestLen)
                continue;
            if (!text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                continue;
            bestId = id;
            bestLen = needle.Length;
        }

        return bestId;
    }

    
    public static IReadOnlyList<(string Needle, string Id)> AllMappings => PhraseToId;
}
