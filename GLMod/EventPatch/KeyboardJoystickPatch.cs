using GLMod.Menu;
using HarmonyLib;
using UnityEngine;

namespace GLMod
{
    /// <summary>
    /// The arrow keys navigate the open Good Loss menu: the player must not walk meanwhile.
    /// </summary>
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    public class KeyboardJoystickUpdatePatch
    {
        public static bool Prefix(KeyboardJoystick __instance)
        {
            if (!MenuBehaviour.IsCapturingArrows)
            {
                return true;
            }

            __instance.del = Vector2.zero;
            return false;
        }
    }
}
