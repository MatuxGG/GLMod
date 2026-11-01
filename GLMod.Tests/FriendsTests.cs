using GLMod.Class;
using GLMod.GLEntities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GLMod.Tests
{
    public class FriendsLogicTests
    {
        private static GLFriend Friend(int id, string status, string name = null) => new GLFriend { userId = id, status = status, username = name ?? "user" + id };

        private static GLFriendsState WithFriends(params GLFriend[] friends) => new GLFriendsState { friends = friends.ToList() };

        // k0044: not in a game on another screen, nothing went back to the main menu and Join timed out
        [Theory]
        [InlineData(true, "MainMenu", true)]
        [InlineData(true, "OnlineGame", true)]
        [InlineData(false, "FindAGame", true)]
        [InlineData(false, "MainMenu", false)]
        public void MustGoToMainMenu_InAGameOrOnAnotherScreen(bool inGame, string scene, bool expected)
        {
            Assert.Equal(expected, FriendsLogic.MustGoToMainMenu(inGame, scene));
        }

        // k0046: the game was left before seeing that the friend is on another region
        [Theory]
        [InlineData(true, "GL Modded", "Good Loss", true)]
        [InlineData(true, null, "Good Loss", true)]
        [InlineData(true, "Good Loss", "Good Loss", false)]
        [InlineData(false, "GL Modded", "Good Loss", false)]
        public void MustStayInGame_WhenInAGameAndTheFriendIsOnAnotherRegion(bool inGame, string current, string friendRegion, bool expected)
        {
            Assert.Equal(expected, FriendsLogic.MustStayInGame(inGame, current, new GLFriend { region = friendRegion }));
        }

        // k0051: the network mode stays on free play at the main menu, and every join was refused there
        [Theory]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void FreePlayBlocksJoin_OnlyWhileInAFreePlayGame(bool freePlay, bool inGame, bool expected)
        {
            Assert.Equal(expected, FriendsLogic.FreePlayBlocksJoin(freePlay, inGame));
        }

        [Theory]
        [InlineData("Good Loss", "Good Loss", true)]
        [InlineData("GL Modded", "Good Loss", false)]
        [InlineData(null, "Good Loss", false)]
        public void IsOnRegionOf_ComparesTheRegionNames(string current, string friendRegion, bool expected)
        {
            Assert.Equal(expected, FriendsLogic.IsOnRegionOf(current, new GLFriend { region = friendRegion }));
        }

        [Fact]
        public void IsOnRegionOf_IsFalseWithoutAFriend()
        {
            Assert.False(FriendsLogic.IsOnRegionOf("Good Loss", null));
        }

        [Theory]
        [InlineData("Matux", "Shedconic", "Matux (Shedconic)")]
        [InlineData("Matux", "matux", "Matux")]
        [InlineData("Matux", null, "Matux")]
        [InlineData("Matux", "", "Matux")]
        [InlineData(null, "Shedconic", "Shedconic")]
        public void DisplayName_ShowsTheAccountAndTheInGameName(string username, string playerName, string expected)
        {
            Assert.Equal(expected, FriendsLogic.DisplayName(username, playerName));
        }

        [Fact]
        public void Ordered_PutsMyRoomFirstAndOfflineLast()
        {
            List<GLFriend> ordered = FriendsLogic.Ordered(new[]
            {
                Friend(1, "offline", "a"), Friend(2, "game", "b"), Friend(3, "online", "c"), Friend(4, "lobby", "d"), Friend(5, "room", "e"),
            });
            Assert.Equal(new[] { 5, 4, 3, 2, 1 }, ordered.Select(f => f.userId));
        }

        [Fact]
        public void Ordered_SortsByNameInsideAGroup()
        {
            List<GLFriend> ordered = FriendsLogic.Ordered(new[] { Friend(1, "online", "zed"), Friend(2, "online", "Abe") });
            Assert.Equal(new[] { 2, 1 }, ordered.Select(f => f.userId));
        }

        [Fact]
        public void Ordered_AcceptsNull()
        {
            Assert.Empty(FriendsLogic.Ordered(null));
        }

        [Fact]
        public void NewlyOnline_OnlyFriendsWhoWereOffline()
        {
            GLFriendsState previous = WithFriends(Friend(1, "offline"), Friend(2, "online"), Friend(3, "offline"));
            GLFriendsState current = WithFriends(Friend(1, "lobby"), Friend(2, "game"), Friend(3, "offline"));
            Assert.Equal(new[] { 1 }, FriendsLogic.NewlyOnline(previous, current).Select(f => f.userId));
        }

        [Fact]
        public void NewlyOnline_NothingAtTheFirstSync()
        {
            Assert.Empty(FriendsLogic.NewlyOnline(null, WithFriends(Friend(1, "online"))));
        }

        [Fact]
        public void NewlyOnline_ANewFriendIsNotAConnection()
        {
            Assert.Empty(FriendsLogic.NewlyOnline(WithFriends(), WithFriends(Friend(1, "online"))));
        }

        [Fact]
        public void NewRequests_OnlyTheOnesNotSeenBefore()
        {
            var previous = new GLFriendsState { incoming = new List<GLFriendRequest> { new GLFriendRequest { requestId = 1 } } };
            var current = new GLFriendsState { incoming = new List<GLFriendRequest> { new GLFriendRequest { requestId = 1 }, new GLFriendRequest { requestId = 2 } } };
            Assert.Equal(new[] { 2 }, FriendsLogic.NewRequests(previous, current).Select(r => r.requestId));
        }

        [Fact]
        public void NewRequests_AllOfThemAtTheFirstSync()
        {
            var current = new GLFriendsState { incoming = new List<GLFriendRequest> { new GLFriendRequest { requestId = 1 } } };
            Assert.Single(FriendsLogic.NewRequests(null, current));
        }

        [Theory]
        [InlineData(30, "friends.seen.now", 0)]
        [InlineData(60, "friends.seen.minutes", 1)]
        [InlineData(3599, "friends.seen.minutes", 59)]
        [InlineData(3600, "friends.seen.hours", 1)]
        [InlineData(86400, "friends.seen.days", 1)]
        [InlineData(-5, "friends.seen.now", 0)]
        public void SeenAgo_PicksTheUnit(long secondsAgo, string key, long value)
        {
            Assert.Equal((key, value), FriendsLogic.SeenAgo(1000000 - secondsAgo, 1000000));
        }

        // k0048: a failed first sync stayed on "Loading" (500, 401, network) or "Unavailable / Loading your friends..." (404)
        [Fact]
        public void EmptyStateKeys_LoadingOnlyWithoutError()
        {
            Assert.Equal(("state.loading", "friends.loading"), FriendsLogic.EmptyStateKeys(null));
        }

        [Theory]
        [InlineData("HTTP 500")]
        [InlineData("HTTP 404")]
        [InlineData("Network error")]
        [InlineData("Unreadable response")]
        public void EmptyStateKeys_AFailedSyncIsNotLoading(string error)
        {
            Assert.Equal(("state.unavailable", "friends.error"), FriendsLogic.EmptyStateKeys(error));
        }

        // k0029: an error without text gave a raw key on screen ("friends.error.HTTP 0")
        [Theory]
        [InlineData("HTTP 0", "friends.error.network")]
        [InlineData("HTTP 502", "friends.error.network")]
        [InlineData("Invalid token", "friends.error.network")]
        [InlineData("not_met", "friends.error.not_met")]
        [InlineData("friends.error.join", "friends.error.join")]
        public void ErrorKey_NeverGivesAKeyWithoutText(string error, string expected)
        {
            var table = new TranslationTable();
            table.Add("en", new Dictionary<string, string>
            {
                { "friends.error.network", "n" },
                { "friends.error.not_met", "m" },
                { "friends.error.join", "j" },
            });
            Assert.Equal(expected, FriendsLogic.ErrorKey(error, key => table.Find("en", key) != null));
        }
    }

    public class FriendNotificationQueueTests
    {
        private const float Duration = 6f;

        [Fact]
        public void ShowsOneAtATimeForItsDuration()
        {
            var queue = new FriendNotificationQueue();
            queue.Enqueue("a", "1");
            queue.Enqueue("b", "2");

            Assert.Equal("a", queue.Current(0f, Duration, false)?.key);
            Assert.Equal("a", queue.Current(5f, Duration, false)?.key);
            Assert.Equal("b", queue.Current(6f, Duration, false)?.key);
            Assert.Null(queue.Current(12f, Duration, false));
        }

        [Fact]
        public void NothingDuringAGameAndTheRequestWaitsForTheLobby()
        {
            var queue = new FriendNotificationQueue();
            queue.Enqueue("a", "1");

            Assert.Null(queue.Current(0f, Duration, true));
            Assert.Null(queue.Current(100f, Duration, true));
            Assert.Equal("a", queue.Current(200f, Duration, false)?.key);
        }

        // k0036: the request on screen when a game started kept its timer and never came back
        [Fact]
        public void ARequestInterruptedByAGameIsShownAgainInFullInTheLobby()
        {
            var queue = new FriendNotificationQueue();
            queue.Enqueue("a", "1");

            Assert.Equal("a", queue.Current(0f, Duration, false)?.key);
            Assert.Null(queue.Current(0.5f, Duration, true));
            Assert.Equal(1, queue.Count);
            Assert.Equal("a", queue.Current(300f, Duration, false)?.key);
            Assert.Equal("a", queue.Current(305f, Duration, false)?.key);
            Assert.Null(queue.Current(306f, Duration, false));
        }

        // k0036: only an interrupted request starts over, one already over stays over
        [Fact]
        public void ARequestAlreadyOverWhenTheGameStartsDoesNotComeBack()
        {
            var queue = new FriendNotificationQueue();
            queue.Enqueue("a", "1");

            Assert.Equal("a", queue.Current(0f, Duration, false)?.key);
            Assert.Null(queue.Current(6f, Duration, true));
            Assert.Null(queue.Current(300f, Duration, false));
        }

        // k0036: an alert on screen when a game started does not come back after it
        [Fact]
        public void AnAlertInterruptedByAGameDoesNotComeBack()
        {
            var queue = new FriendNotificationQueue();
            queue.EnqueueAlert("online", "1");

            Assert.Equal("online", queue.Current(0f, Duration, false)?.key);
            Assert.Null(queue.Current(0.5f, Duration, true));
            Assert.Null(queue.Current(300f, Duration, false));
        }

        [Fact]
        public void EmptyQueueShowsNothing()
        {
            Assert.Null(new FriendNotificationQueue().Current(0f, Duration, false));
        }

        // k0035: the alerts still waiting when a game started came out one by one after it
        [Fact]
        public void AlertsStillWaitingAreDroppedByAGameButNotRequests()
        {
            var queue = new FriendNotificationQueue();
            queue.EnqueueAlert("online", "1");
            queue.EnqueueAlert("online", "2");
            queue.Enqueue("request", "3");

            Assert.Equal("1", queue.Current(0f, Duration, false)?.name);
            Assert.Null(queue.Current(2f, Duration, true));
            Assert.Equal("3", queue.Current(600f, Duration, false)?.name);
            Assert.Null(queue.Current(606f, Duration, false));
        }

        // k0035: the open menu drops the alerts the same way
        [Fact]
        public void DropAlertsKeepsTheRequestsInOrder()
        {
            var queue = new FriendNotificationQueue();
            queue.Enqueue("request", "1");
            queue.EnqueueAlert("online", "2");
            queue.Enqueue("request", "3");

            queue.DropAlerts();

            Assert.Equal(2, queue.Count);
            Assert.Equal("1", queue.Current(0f, Duration, false)?.name);
            Assert.Equal("3", queue.Current(6f, Duration, false)?.name);
        }

        // k0041: a request answered while its notification waited was still announced in the lobby
        [Fact]
        public void ARequestAnsweredWhileWaitingIsNeverShown()
        {
            var queue = new FriendNotificationQueue();
            queue.EnqueueRequest("request", "C", 7);

            Assert.Null(queue.Current(0f, Duration, true));
            queue.RetainRequests(new int[0]);

            Assert.Equal(0, queue.Count);
            Assert.Null(queue.Current(100f, Duration, false));
        }

        // k0041: same thing for the one on screen when the menu opened or a game started
        [Fact]
        public void ARequestAnsweredAfterBeingInterruptedDoesNotComeBack()
        {
            var queue = new FriendNotificationQueue();
            queue.EnqueueRequest("request", "C", 7);

            Assert.Equal("C", queue.Current(0f, Duration, false)?.name);
            Assert.Null(queue.Current(0.5f, Duration, true));
            queue.RetainRequests(new int[0]);

            Assert.Equal(0, queue.Count);
            Assert.Null(queue.Current(300f, Duration, false));
        }

        // k0041: only the requests no longer pending go, the rest keeps its order
        [Fact]
        public void RetainRequestsKeepsThePendingRequestsAndTheOtherNotifications()
        {
            var queue = new FriendNotificationQueue();
            queue.EnqueueRequest("request", "1", 1);
            queue.EnqueueAlert("online", "2");
            queue.EnqueueRequest("request", "3", 3);
            queue.Enqueue("other", "4");

            queue.RetainRequests(new[] { 3 });

            Assert.Equal(3, queue.Count);
            Assert.Equal("2", queue.Current(0f, Duration, false)?.name);
            Assert.Equal("3", queue.Current(6f, Duration, false)?.name);
            queue.RetainRequests(new[] { 3 });
            Assert.Equal("3", queue.Current(7f, Duration, false)?.name);
            Assert.Equal("4", queue.Current(12f, Duration, false)?.name);
        }
    }

    public class FriendsResponseTests
    {
        // Answer of POST /friends/sync, as sent by the site
        private const string Sync = @"{""friends"":[{""userId"":2,""username"":""Lady"",""playerName"":""Ladybug"",""status"":""lobby"",""region"":""Ranked EU"",""canJoin"":true,""gameCode"":""GVOVVF"",""lastSeen"":1800000000},{""userId"":3,""username"":""Off"",""playerName"":null,""status"":""offline"",""region"":null,""canJoin"":false,""gameCode"":null,""lastSeen"":null}],""incoming"":[{""requestId"":7,""userId"":4,""username"":""New"",""playerName"":null}],""outgoing"":[],""room"":[{""clientId"":12,""userId"":2,""username"":""Lady"",""playerName"":""Ladybug"",""isFriend"":true,""requested"":false}],""blocked"":[{""userId"":9,""username"":""Bad""}],""settings"":{""refuseRequests"":false,""appearOffline"":true,""onlineAlert"":true}}";

        [Fact]
        public void ParsesTheWholeAnswer()
        {
            Assert.True(PlayerApiResponse.TryParse(Sync, out GLFriendsState state));

            Assert.Equal("GVOVVF", state.friends[0].gameCode);
            Assert.True(state.friends[0].canJoin);
            Assert.Equal(1800000000, state.friends[0].lastSeen);
            Assert.Null(state.friends[1].lastSeen);
            Assert.Equal(7, state.incoming[0].requestId);
            Assert.Equal(12, state.room[0].clientId);
            Assert.True(state.room[0].isFriend);
            Assert.Equal("Bad", state.blocked[0].username);
            Assert.True(state.settings.appearOffline);
        }

        [Fact]
        public void ParsesRecentPlayers()
        {
            Assert.True(PlayerApiResponse.TryParse(@"{""players"":[{""userId"":2,""username"":""Lady"",""playerName"":null,""isFriend"":false}]}", out GLRecentPlayers recent));
            Assert.Equal(2, recent.players[0].userId);
        }
    }

    public class PresenceServersTests
    {
        [Theory]
        [InlineData("impranked.goodloss.fr", true)]
        [InlineData("betaranked.goodloss.fr", true)]
        [InlineData("imp.goodloss.fr", true)]
        [InlineData("matchmaker-eu.among.us", false)]
        [InlineData("other.example", false)]
        [InlineData(null, false)]
        public void OnlyGoodLossServersGiveAGameCode(string pingServer, bool expected)
        {
            Assert.Equal(expected, GoodLossServers.IsPresenceServer(pingServer));
        }

        // k0055: no room section outside the presence servers, the site has no list of the room there
        [Theory]
        [InlineData("ABCDEF", "matchmaker-eu.among.us", false)]
        [InlineData("ABCDEF", "other.example", false)]
        [InlineData("ABCDEF", null, false)]
        [InlineData("ABCDEF", "imp.goodloss.fr", true)]
        [InlineData(null, "imp.goodloss.fr", false)]
        public void RoomListOnlyInAGameOnAPresenceServer(string gameCode, string pingServer, bool expected)
        {
            Assert.Equal(expected, GoodLossServers.HasRoomList(gameCode, pingServer));
        }
    }
}
