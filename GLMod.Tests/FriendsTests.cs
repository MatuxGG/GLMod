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

        [Fact]
        public void EmptyQueueShowsNothing()
        {
            Assert.Null(new FriendNotificationQueue().Current(0f, Duration, false));
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
