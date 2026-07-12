using HarmonyLib;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Milestone 1 hook.
    ///
    /// GameManager.ObtainKeyItem(KeyItem, bool) - decompiled GameManager.cs:3322.
    /// When connected: a key item picked up in-game becomes an "Obtain X" check and the
    /// vanilla grant is cancelled (the real item comes from the multiworld). Server grants
    /// re-enter this method under GrantGuard and are allowed to run the original.
    /// </summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ObtainKeyItem))]
    public static class ObtainKeyItemPatch
    {
        // Prefix returning false skips the original method.
        private static bool Prefix(KeyItem _keyItem)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;   // not active -> 100 % vanilla
            if (ApClient.GrantGuard) return true;           // server grant -> run original

            ap.SendKeyItemCheck(_keyItem);
            return false;                                    // cancel the vanilla grant
        }
    }
}
