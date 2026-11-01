using GLMod.Class;
using GLMod.GLEntities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// Friends list of the Good Loss account: declares where the player is and keeps the list up to date.
    /// Main thread only.
    /// </summary>
    public interface IFriendsService
    {
        /// <summary>
        /// Last answer of the site, null before the first one or when logged out.
        /// </summary>
        GLFriendsState State { get; }

        /// <summary>
        /// Requests waiting for an answer (the badge of the menu).
        /// </summary>
        int PendingRequestCount { get; }

        /// <summary>
        /// False when the API has no friends endpoints.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Why the last sync failed, null when it succeeded, before the first answer or when logged out.
        /// </summary>
        string Error { get; }

        /// <summary>
        /// Accounts met in the last games, null until ReloadRecentPlayers answered.
        /// </summary>
        IReadOnlyList<GLRecentPlayer> RecentPlayers { get; }

        /// <summary>
        /// Notifications to show: requests received, friends coming online.
        /// </summary>
        FriendNotificationQueue Notifications { get; }

        /// <summary>
        /// Declares the presence of the player and reads the list (POST /friends/sync).
        /// </summary>
        IEnumerator Sync();

        /// <summary>
        /// Endless coroutine syncing regularly while the player is logged in and the Friends tab exists.
        /// </summary>
        IEnumerator KeepInSync();

        /// <summary>
        /// Loads the accounts met in the last games.
        /// </summary>
        IEnumerator ReloadRecentPlayers();

        /// <summary>
        /// Sends a friend request. onDone receives null on success, otherwise an error code of the API.
        /// </summary>
        IEnumerator SendRequest(int userId, Action<string> onDone = null);

        /// <summary>
        /// Accepts or refuses a request addressed to the player.
        /// </summary>
        IEnumerator Answer(int requestId, bool accept, Action<string> onDone = null);

        /// <summary>
        /// Removes a friend.
        /// </summary>
        IEnumerator Remove(int userId, Action<string> onDone = null);

        /// <summary>
        /// Blocks or unblocks a player. Blocking also ends the friendship and the requests between them.
        /// </summary>
        IEnumerator SetBlocked(int userId, bool blocked, Action<string> onDone = null);

        /// <summary>
        /// Changes the settings given, the others stay as they are.
        /// </summary>
        IEnumerator UpdateSettings(bool? refuseRequests, bool? appearOffline, bool? onlineAlert, Action<string> onDone = null);

        /// <summary>
        /// Joins the lobby of a friend (friend.canJoin), leaving the current game first.
        /// onDone receives null when the join started, otherwise a translation key.
        /// </summary>
        IEnumerator Join(GLFriend friend, Action<string> onDone = null);
    }
}
