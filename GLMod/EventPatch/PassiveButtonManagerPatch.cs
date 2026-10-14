using GLMod.Menu;
using HarmonyLib;

namespace GLMod
{
    /// <summary>
    /// Clicks on the Good Loss menu must not reach the game buttons underneath.
    /// </summary>
    [HarmonyPatch(typeof(PassiveButtonManager), nameof(PassiveButtonManager.Update))]
    public class PassiveButtonManagerUpdatePatch
    {
        public static bool Prefix()
        {
            return !MenuBehaviour.IsPointerOverMenu;
        }
    }
}
