using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Frees the mouse cursor on the TITLE SCREEN ONLY, so the Archipelago connection panel
    /// can be clicked (demande Jonath 2026-08-01).
    ///
    /// Grunn plays entirely on the keyboard and pins the cursor: GameManager.HideCursor()
    /// sets CursorLockMode.Locked + visible = false outside the editor
    /// (GameManager.cs:1156-1167), and it is called UNCONDITIONALLY FROM
    /// GameManager.Update() (GameManager.cs:1729), i.e. every single frame - plus on
    /// OnApplicationFocus and Init.
    ///
    /// A first attempt just re-asserted the unlocked state every frame from our own Update.
    /// That fails and looks broken [J 2026-08-01: "effet de clignotement et il reset a chaque
    /// fois au centre de l'ecran"]: the two Updates fight, and CursorLockMode.Locked
    /// re-centres the pointer on every frame the game wins. Whoever writes last wins, which
    /// is a race, not a fix.
    ///
    /// So the game is stopped at the source: a prefix skips HideCursor entirely while the
    /// cursor is wanted. Nothing re-locks it behind our back, and there is no flicker.
    ///
    /// IN GAME NOTHING CHANGES: Wanted is false everywhere except the title screen, so
    /// HideCursor runs exactly as vanilla and the first-person view is untouched.
    /// </summary>
    internal static class CursorUnlock
    {
        /// <summary>True while the mod wants a usable pointer (title screen only). Read by
        /// the HideCursor prefix.</summary>
        public static bool Wanted { get; private set; }

        public static void Free()
        {
            if (Wanted) return;
            Wanted = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Back to vanilla behaviour - once, and only if we had freed it. The next
        /// HideCursor from the game's own Update then keeps it that way.</summary>
        public static void Restore()
        {
            if (!Wanted) return;
            Wanted = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>Suppresses GameManager.HideCursor while the connection panel needs the
    /// pointer. This is the only reliable way: the game calls it from Update, so competing
    /// with it frame by frame just produces a flickering, self-centring cursor.</summary>
    [HarmonyPatch(typeof(GameManager), "HideCursor")]
    public static class HideCursorPatch
    {
        private static bool Prefix() => !CursorUnlock.Wanted;
    }

    /// <summary>Zeroes the analog inputs while one of our panels owns the keyboard.
    ///
    /// InputCapturePatch alone was not enough [J 2026-08-01: "le jeu continue d'intercepter
    /// les input, donc mon perso se deplacait pendant que j'ecrivais"]: OnMove and OnLook do
    /// NOT go through HandleInput, they assign InputManager.moveDirection / lookDirection
    /// directly (InputManager.cs:371-378). And since the new Input System only fires on
    /// CHANGE, a key held down when the console is opened leaves a non-zero vector that
    /// every consumer keeps reading - the character walks on by itself.
    ///
    /// UpdateManager.Update is the game's single update hub: it drives every UpdateNormal()
    /// (UpdateManager.cs:85-94), so clearing the vectors in a prefix there guarantees the
    /// player controller, camera, head bob and readers all see zero this frame.</summary>
    [HarmonyPatch(typeof(UpdateManager), "Update")]
    public static class InputVectorCapturePatch
    {
        private static void Prefix()
        {
            if (!ConsoleUi.Focused && !ConnectUi.CapturingKeyboard) return;
            InputManager.moveDirection = Vector2.zero;
            InputManager.lookDirection = Vector2.zero;
            // Same trap as OnMove/OnLook: OnToolCycleScroll assigns scrollDirection
            // straight from the callback (InputManager.cs:415-417), so it never goes
            // through HandleInput either. Scrolling the console history was cycling the
            // player's tools underneath [J 2026-08-01].
            InputManager.scrollDirection = Vector2.zero;
        }
    }
}
