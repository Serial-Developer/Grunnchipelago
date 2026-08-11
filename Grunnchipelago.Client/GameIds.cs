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

        // Endings that count as DEATHS for DeathLink (decision Jonath 2026-07-13):
        // every ending EXCEPT Bus, Picnic and GoodEnd (good/true ending).
        public static readonly HashSet<EndingType> DeathLinkEndings = new HashSet<EndingType>
        {
            EndingType.Mist, EndingType.SacredFlowers, EndingType.Drown, EndingType.Darkness,
            EndingType.LongHallway, EndingType.HedgeMaze, EndingType.WorldEnd, EndingType.Dog,
        };

        // Buff / trap item names (ids.json).
        public const string BuffMoveSpeed = "Move Speed Boost";
        public const string BuffCutterRange = "Cutter Range Boost";
        public const string BuffCuttingRate = "Cutting Rate Boost";
        public const string TrapSpeed = "Speed Trap";
        public const string TrapSize = "Size Trap";
        public const string TrapInvertedControls = "Inverted Controls Trap";

        // The 5 world-altering traps were REDESIGNED and RENAMED on 2026-07-27 (demande
        // Jonath): the four "regrow one element in a random zone" traps became three
        // full ZONE RESETS plus a night trap, and the flower trap became the sacred-flower
        // trap. Their ids in ids.json are UNCHANGED (478660304..308) - only the names moved.
        public const string TrapParkReset = "Park Reset Trap";        // id 478660304
        public const string TrapSacredFlower = "Sacred Flower Trap";  // id 478660305
        public const string TrapGardenReset = "Garden Reset Trap";    // id 478660306
        public const string TrapNight = "Night Trap";                 // id 478660307
        public const string TrapChurchReset = "Church Reset Trap";    // id 478660308

        // Pre-rename names, still accepted so a seed generated BEFORE 2026-07-27 keeps
        // working (its datapackage carries the old names for the same ids). They map to
        // the NEW behaviour of the same id - a trap is a trap, and the alternative would
        // be a silently inert item mid-run.
        public const string TrapLegacyRegrowGrass = "Regrow Grass Trap";        // -> Park Reset
        public const string TrapLegacyRewaterFlowers = "Rewater Flowers Trap";  // -> Sacred Flower
        public const string TrapLegacyRegrowHedge = "Regrow Hedge Trap";        // -> Garden Reset
        public const string TrapLegacyReturnTrash = "Return Trash Trap";        // -> Night
        public const string TrapLegacyRegrowMolehills = "Regrow Molehills Trap";// -> Church Reset

        // --- Time features (demande Jonath 2026-07-27) ---------------------------------
        // Frozen scene paths from dump/grunnchipelago_dump.json (interactions of type
        // Wait1Hour). The three PARK benches skip 3 hours instead of 1; the church bench
        // nearest the church door jumps straight to night.
        public static readonly HashSet<string> ParkBenchPaths = new HashSet<string>
        {
            "Main/Areas/Park/Container/Hide_Park/Props/bench0 (4)/bench_waitInteraction0",
            "Main/Areas/Park/Container/Hide_Park/Props/bench0 (5)/bench_waitInteraction0",
            "Main/Areas/Park/Container/Hide_Park/Props/bench0 (6)/bench_waitInteraction0",
        };

        // bench0 (3) at (10.59, 10.0, 1.28): the closest Wait1Hour bench to the church door
        // (~34 m from churchSideDoorKnock0, interaction type ChurchDoor) - the other church
        // bench is ~66 m away. Choice confirmed by Jonath 2026-07-27.
        public const string ChurchNightBenchPath =
            "Main/Areas/ChurchArea/Container/Hide_ChurchMid/Props/bench0 (3)/bench_waitInteraction0";

        // --- "Deed" location names (ids.json block 478661500+, category "deed") ---------
        // Rewarded ACTIONS rather than pickups (demande Jonath 2026-07-28). Names must match
        // ids.json EXACTLY - they are resolved by name through the session datapackage.
        public const string DeedPizzaBox = "Deed: Throw Away PizzaBox";
        public const string DeedPrettyFlower = "Deed: Place PrettyFlower in Vase";
        public const string DeedSchoolBand = "Deed: Complete the School Band";
        public const string DeedFishbowl = "Deed: Place GoldFishAlive in Fishbowl";
        public const string DeedWormHill = "Deed: Return Worm to the Worm Hill";
        public const string DeedSnailMedal = "Deed: Award Medal to the Snail";
        public const string DeedSeveredHand = "Deed: Return SeveredHand";

        // --- "Chore" location names (ids.json block 478661600+, category "chore") -------
        // The five START-GARDEN maintenance jobs. Vanilla paid 2 gulden the first time each
        // was finished in the garden; those payouts are checks now (demande Jonath
        // 2026-07-30) and the pool holds five "Golden Gulden" worth 2 each in exchange.
        public const string ChoreHedges = "Chore: Trim Every Hedge (Garden)";
        public const string ChoreGrass = "Chore: Cut All the Grass (Garden)";
        public const string ChoreMolehills = "Chore: Clear Every Molehill (Garden)";
        public const string ChoreFlowers = "Chore: Water Every Flower (Garden)";
        public const string ChoreLitter = "Chore: Pick Up All the Litter (Garden)";
        /// <summary>The 8 potted plants. Unlike the five above, vanilla pays NO gulden for
        /// this one (achievement only), so it adds no Golden Gulden to the pool.</summary>
        public const string ChorePottedPlants = "Chore: Trim Every Potted Plant";

        /// <summary>The chore coin: worth TWO gulden, exactly what the jobs used to pay
        /// (GameManager.areaCompleteGuldenAdd).</summary>
        public const string ItemGoldenGulden = "Golden Gulden";
        public const int GoldenGuldenValue = 2;

        // slot_data keys (fill_slot_data in worlds/grunn/__init__.py).
        public const string SlotGoal = "goal";                       // 0 good / 1 true / 2 all
        public const string SlotCoinsanity = "coinsanity";
        public const string SlotPersistentShortcuts = "persistent_shortcuts";
        public const string SlotDeathLink = "death_link";
        public const string SlotPolaroidChecks = "polaroid_checks";
        public const string SlotGhostChecks = "ghost_checks";
        public const string SlotLockPlayerHut = "lock_player_hut";
        public const string SlotChoreChecks = "chore_checks";
        public const string SlotExcludeBadEndings = "exclude_bad_endings";
        public const string SlotMaskItems = "mask_items";

        // Full gulden location names (ids.json), indexed like GuldenIndexByPath. Used for
        // check labels and the verbose pickup diagnostic popup.
        public static readonly string[] GuldenLocationNames =
        {
            "Gulden #1 (GasStationOffice)", "Gulden #2 (Unknown)", "Gulden #3 (Bunker)",
            "Gulden #4 (StartGarden)", "Gulden #5 (Road)", "Gulden #6 (StartGarden)",
            "Gulden #7 (Road)", "Gulden #8 (Unknown)", "Gulden #9 (Church)",
            "Gulden #10 (StartGarden)", "Gulden #11 (Park)", "Gulden #12 (Park)",
            "Gulden #13 (PillarSpace)", "Gulden #14 (Ferry)", "Gulden #15 (Intratuin)",
        };

        public const int GoalGoodEnding = 0;
        public const int GoalTrueEnding = 1;
        public const int GoalAllEndings = 2;
    }
}
