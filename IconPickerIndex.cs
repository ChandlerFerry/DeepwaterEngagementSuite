using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DeepwaterEngagementSuite;

[JsonConverter(typeof(StringEnumConverter))]
public enum IconPickerIndex
{
    OtherChests,
    BottledItemChest,
    GoldTreasureChest,
    ClamTreasureChest,
    CurrencyTreasureChest,
    /// <summary>Opulent currency chest (divine-tier) — Metadata/.../CurrencyTreasureChestOpulent.</summary>
    CurrencyTreasureChestOpulent,
    /// <summary>Gemcutter's Prism currency chest — Metadata/.../CurrencyGemcuttersChest1.</summary>
    CurrencyGemcuttersChest,
    UniqueWeaponChest,
    UniqueArmourChest,
    UniqueJewelleryChest,
    ScarabChest,
    StackedDecksChest,
    MapsChest,
    AllflameEmbersChest,
    CursedDucatDrop,
    RandomDucatChest,
    /// <summary>Hazard boat chest — Metadata/Chests/LeagueDeepwater/DeepwaterChestHazardBoat.</summary>
    HazardBoatChest,
    IzaroObject,
    AltarCrab,
    AltarOctopus,
    TormentedSpiritEncounter,
    LanternReplenishEncounter,
    GoldenLanternEncounter,
    InfusedCoralEncounter,
    StrongboxDivination,
    StrongboxScarab,
    StrongboxArcanist,
    PointerTarget,
}
