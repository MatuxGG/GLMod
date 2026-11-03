using GLMod.Services.Implementations;
using UnityEngine;

namespace GLMod.Class
{
    /// <summary>
    /// Whether the main menu shows its first screen. Play, online, create game, account, credits... stay in the
    /// MainMenu scene, and the banners would cover their buttons. Main thread only.
    /// </summary>
    public static class MainMenuScreen
    {
        private static MainMenuManager _manager;
        private static string _lastScreen;

        /// <summary>
        /// Main menu of the scene, given by the MainMenuManager.Start postfix
        /// </summary>
        public static void Track(MainMenuManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// True on the first screen of the main menu: its buttons shown, no sub-screen nor pop-up open
        /// </summary>
        public static bool IsFirstScreen()
        {
            MainMenuManager manager = _manager;
            if (manager == null || !PlannedLobbyService.IsAtMainMenu())
            {
                return false;
            }

            string screen = OpenScreen(manager);
            if (screen != _lastScreen)
            {
                _lastScreen = screen;
                GLMod.log("[Menu] Main menu screen: " + screen);
            }
            return screen == "first";
        }

        /// <summary>
        /// Name of what the main menu shows: "first", or the sub-screen open on top of it
        /// </summary>
        private static string OpenScreen(MainMenuManager manager)
        {
            if (!IsShown(manager.mainMenuUI))
            {
                return "mainMenuUI hidden";
            }
            if (IsShown(manager.gameModeButtons))
            {
                return "gameModeButtons";
            }
            if (IsShown(manager.onlineButtons))
            {
                return "onlineButtons";
            }
            if (IsShown(manager.enterCodeButtons))
            {
                return "enterCodeButtons";
            }
            if (IsShown(manager.accountButtons))
            {
                return "accountButtons";
            }
            if (IsShown(manager.creditsScreen))
            {
                return "creditsScreen";
            }
            if (IsShown(manager.createGameScreen))
            {
                return "createGameScreen";
            }
            if (IsShown(manager.announcementPopUp))
            {
                return "announcementPopUp";
            }
            // screenTint is not a sign: it is also active on the first screen
            return "first";
        }

        private static bool IsShown(GameObject gameObject)
        {
            return gameObject != null && gameObject.activeInHierarchy;
        }

        private static bool IsShown(Component component)
        {
            return component != null && component.gameObject.activeInHierarchy;
        }
    }
}
