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
    DeadMansSulphurSmall,
    DeadMansSulphurBase,
    DeadMansSulphurLarge,
    DeadMansSulphurHuge,
}
