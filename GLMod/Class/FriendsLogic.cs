using GLMod.Constants;
using GLMod.GLEntities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GLMod.Class
{
    /// <summary>
    /// Pure logic of the friends list: names, order, what changed between two syncs. No IO, no Unity.
    /// </summary>
    public static class FriendsLogic
    {
        private const long Minute = 60;
        private const long Hour = 3600;
        private const long Day = 86400;

        /// <summary>
        /// Joining a friend starts from the main menu: true in a game or on any other screen.
        /// </summary>
        public static bool MustGoToMainMenu(bool inGame, string sceneName)
        {
            return inGame || sceneName != GameConstants.SCENE_MAIN_MENU;
        }

        /// <summary>
        /// "Account (in-game name)", the account alone when the name is unknown or the same.
        /// </summary>
        public static string DisplayName(string username, string playerName)
        {
            if (string.IsNullOrEmpty(playerName) || string.Equals(username, playerName, StringComparison.OrdinalIgnoreCase))
            {
                return username ?? playerName ?? "";
            }
            return string.IsNullOrEmpty(username) ? playerName : $"{username} ({playerName})";
        }

        /// <summary>
        /// Friends in my room first, then the ones who can be reached, offline ones last. By name inside a group.
        /// </summary>
        public static List<GLFriend> Ordered(IEnumerable<GLFriend> friends)
        {
            return (friends ?? Enumerable.Empty<GLFriend>())
                .OrderBy(f => StatusRank(f.status))
                .ThenBy(f => f.username, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int StatusRank(string status)
        {
            switch (status)
            {
                case GameConstants.FRIEND_STATUS_ROOM: return 0;
                case GameConstants.FRIEND_STATUS_LOBBY: return 1;
                case GameConstants.FRIEND_STATUS_ONLINE: return 2;
                case GameConstants.FRIEND_STATUS_GAME: return 3;
                default: return 4;
            }
        }

        public static bool IsOnline(GLFriend friend)
        {
            return friend != null && friend.status != GameConstants.FRIEND_STATUS_OFFLINE;
        }

        /// <summary>
        /// Friends offline at the previous sync and online now. Nothing at the first sync: everyone
        /// already online would count as just connected.
        /// </summary>
        public static List<GLFriend> NewlyOnline(GLFriendsState previous, GLFriendsState current)
        {
            if (previous?.friends == null || current?.friends == null)
            {
                return new List<GLFriend>();
            }

            var wasOnline = new HashSet<int>(previous.friends.Where(IsOnline).Select(f => f.userId));
            var known = new HashSet<int>(previous.friends.Select(f => f.userId));
            return current.friends.Where(f => IsOnline(f) && known.Contains(f.userId) && !wasOnline.Contains(f.userId)).ToList();
        }

        /// <summary>
        /// Requests that were not there at the previous sync.
        /// </summary>
        public static List<GLFriendRequest> NewRequests(GLFriendsState previous, GLFriendsState current)
        {
            if (current?.incoming == null)
            {
                return new List<GLFriendRequest>();
            }

            var known = new HashSet<int>((previous?.incoming ?? new List<GLFriendRequest>()).Select(r => r.requestId));
            return current.incoming.Where(r => !known.Contains(r.requestId)).ToList();
        }

        /// <summary>
        /// Translation keys of the title and text shown while there is no list: loading, or the failure of the last sync.
        /// </summary>
        public static (string title, string text) EmptyStateKeys(string error)
        {
            return error == null ? ("state.loading", "friends.loading") : ("state.unavailable", "friends.error");
        }

        /// <summary>
        /// Translation key and value of "seen X ago".
        /// </summary>
        public static (string key, long value) SeenAgo(long lastSeen, long now)
        {
            long seconds = Math.Max(0, now - lastSeen);
            if (seconds < Minute) return ("friends.seen.now", 0);
            if (seconds < Hour) return ("friends.seen.minutes", seconds / Minute);
            if (seconds < Day) return ("friends.seen.hours", seconds / Hour);
            return ("friends.seen.days", seconds / Day);
        }

        /// <summary>
        /// Translation key of an action error: the key itself or friends.error.&lt;code&gt;. An error without
        /// text (network failure, "HTTP 502", unknown API message) gives the network text.
        /// </summary>
        public static string ErrorKey(string error, Func<string, bool> hasText)
        {
            string key = error.StartsWith("friends.") ? error : "friends.error." + error;
            return hasText(key) ? key : "friends.error.network";
        }
    }
}
