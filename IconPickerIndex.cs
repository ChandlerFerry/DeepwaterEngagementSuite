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
    CurrencyTreasureChestOpulent,
    /// <summary>Gemcutter's Prism currency chest — Metadata/.../CurrencyGemcuttersChest1.</summary>
    CurrencyGemcuttersChest,
    UniqueWeaponChest,
    UniqueArmourChest,
    ScarabChest,
    StackedDecksChest,
    MapsChest,
    AllflameEmbersChest,
    CursedDucatDrop,
    RandomDucatChest,
    IzaroObject,
    AltarCrab,
    AltarOctopus,
    TormentedSpiritEncounter,
    LanternReplenishEncounter,
    GoldenLanternEncounter,
    InfusedCoralEncounter,
    PointerTarget,
}
