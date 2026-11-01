using GLMod.Menu;
using GLMod.Services.Interfaces;
using System;
using System.Reflection;
using Xunit;

namespace GLMod.Tests
{
    public class FriendsMenuTabTests
    {
        public class Fake<T> : DispatchProxy
        {
            public Func<MethodInfo, object> Handler;

            protected override object Invoke(MethodInfo method, object[] args) => Handler(method);
        }

        private static T Create<T>(Func<MethodInfo, object> handler)
        {
            T proxy = DispatchProxy.Create<T, Fake<T>>();
            ((Fake<T>)(object)proxy).Handler = handler;
            return proxy;
        }

        // k0032: the recent players were requested once per launch of the game, even when that call failed
        [Fact]
        public void OnShown_ReloadsRecentPlayersEachTime()
        {
            int reloads = 0;
            IFriendsService friends = Create<IFriendsService>(method =>
            {
                if (method.Name == nameof(IFriendsService.ReloadRecentPlayers))
                {
                    reloads++;
                }
                return Array.Empty<object>().GetEnumerator();
            });
            IAuthenticationService auth = Create<IAuthenticationService>(method => true);
            ITranslationService translations = Create<ITranslationService>(method => null);
            var tab = new FriendsMenuTab(friends, auth, translations, routine => { });

            tab.OnShown();
            tab.OnShown();

            Assert.Equal(2, reloads);
        }
    }
}
