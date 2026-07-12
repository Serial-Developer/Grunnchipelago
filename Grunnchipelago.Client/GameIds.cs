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

        public const int GoalGoodEnding = 0;
        public const int GoalTrueEnding = 1;
        public const int GoalAllEndings = 2;
    }
}
