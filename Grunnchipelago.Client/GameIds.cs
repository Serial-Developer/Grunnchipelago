using System.Collections.Generic;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Constants that tie the mod to the apworld's ids.json (design/ids.json).
    /// Ghost and gulden checks are sent by numeric id computed from a position sort
    /// (x then z), matching the frozen order in ids.json.
    /// </summary>
    public static class GameIds
    {
        // ids.json: "Calm Ghost #1" = 478661301, "Gulden #1" = 478661401. Frozen.
        public const long GhostBaseId = 478661301;
        public const long GuldenBaseId = 478661401;

        // Ghost / gulden checks are identified by their FROZEN scene path from ids.json
        // (captured at dump time). A runtime position sort would be unreliable: ghosts
        // drift around their spawn point and consumed pickups can vanish from the scene,
        // shifting indices. Paths never change. Index i -> location id Base + i.
        public static readonly Dictionary<string, int> GhostIndexByPath = new Dictionary<string, int>
        {
            { "*** GHOSTS ***/ghost0_bridge", 0 },                          // #1 (Road)
            { "*** GHOSTS ***/ghost0_bunker", 1 },                          // #2 (Bunker)
            { "*** GHOSTS ***/ghost0_redCar0", 2 },                         // #3 (Road)
            { "Main/Interactions/scooterCrash0/ghost0_scooterCrash0", 3 },  // #4 (WindyPath)
            { "*** GHOSTS ***/ghost0_void", 4 },                            // #5 (Void)
            { "*** GHOSTS ***/ghost0_gnomeForest", 5 },                     // #6 (GnomeForest)
            { "*** GHOSTS ***/ghost0_pillarspace", 6 },                     // #7 (PillarSpace)
            { "*** GHOSTS ***/ghost0_backup", 7 },                          // #8 (PillarSpace)
        };

        public static readonly Dictionary<string, int> GuldenIndexByPath = new Dictionary<string, int>
        {
            { "Main/Gulden/gulden0 (4)", 0 },                               // #1 (GasStationOffice)
            { "Main/Gulden/gulden0 (1)", 1 },                               // #2 (Unknown/Road)
            { "Main/Gulden/gulden0 (3)", 2 },                               // #3 (Bunker)
            { "Main/Gulden/gulden0", 3 },                                   // #4 (StartGarden)
            { "Main/BusArriving0/busArrivingContainer/gulden0_arriveBus0", 4 },  // #5 (spawn bus)
            { "Main/Gulden/gulden0 (2)", 5 },                               // #6 (StartGarden)
            { "Main/Areas/Road/Container/Hide_RoadMemorial/Pylonnen (1)/pylon0_gulden_Flipped/gulden0_pylon", 6 },  // #7
            { "Main/Gulden/GuldenPotContainer0/gulden0_pot", 7 },           // #8 (Unknown/Road)
            { "Main/Gulden/MolehillGuldenContainer0/gulden0 (6)", 8 },      // #9 (Church)
            { "Main/Areas/StartGardenArea/Container/Hide_HouseDetails/gardenGnome0/gardengnome_Destroyed/gulden0_gardenGnome", 9 },  // #10
            { "Main/Gulden/gulden0 (5)", 10 },                              // #11 (Park)
            { "Main/Areas/Park/Container/Hide_Park/BranchHoleContainer/BranchHoleSearched/gulden0 (6)", 11 },  // #12
            { "Main/Gulden/gulden0 (6)", 12 },                              // #13 (PillarSpace)
            { "NonEuclidian/Ferry/Hide_Ferry/gulden_ferry0", 13 },          // #14 (Ferry)
            { "Main/Gulden/gulden_intratuin0", 14 },                        // #15 (Intratuin)
        };

        // The 6 tools exist both as a KeyItem (ObtainKeyItem) and as an Item (AddTool).
        // Only Shears/Scissor differ in name. An AP tool item grants both.
        public static readonly Dictionary<Item, KeyItem> ToolToKeyItem = new Dictionary<Item, KeyItem>
        {
            { Item.Trowel, KeyItem.Trowel },
            { Item.Scissor, KeyItem.Shears },
            { Item.WateringCan, KeyItem.WateringCan },
            { Item.Hammer, KeyItem.Hammer },
            { Item.Trumpet, KeyItem.Trumpet },
            { Item.MagicSword, KeyItem.MagicSword },
        };

        public static readonly Dictionary<KeyItem, Item> KeyItemToTool = new Dictionary<KeyItem, Item>
        {
            { KeyItem.Trowel, Item.Trowel },
            { KeyItem.Shears, Item.Scissor },
            { KeyItem.WateringCan, Item.WateringCan },
            { KeyItem.Hammer, Item.Hammer },
            { KeyItem.Trumpet, Item.Trumpet },
            { KeyItem.MagicSword, Item.MagicSword },
        };

        // The 11 goal-relevant endings (DemoEnding excluded), as the EndingType enum names.
        public static readonly HashSet<EndingType> AllEndings = new HashSet<EndingType>
        {
            EndingType.Mist, EndingType.Bus, EndingType.SacredFlowers, EndingType.Drown,
            EndingType.Darkness, EndingType.LongHallway, EndingType.HedgeMaze,
            EndingType.WorldEnd, EndingType.GoodEnd, EndingType.Dog, EndingType.Picnic,
        };

        // slot_data keys (fill_slot_data in worlds/grunn/__init__.py).
        public const string SlotGoal = "goal";                       // 0 good / 1 true / 2 all
        public const string SlotCoinsanity = "coinsanity";
        public const string SlotPersistentShortcuts = "persistent_shortcuts";
        public const string SlotDeathLink = "death_link";

        public const int GoalGoodEnding = 0;
        public const int GoalTrueEnding = 1;
        public const int GoalAllEndings = 2;
    }
}
