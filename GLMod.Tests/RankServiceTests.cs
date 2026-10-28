using System;
using System.Collections.Generic;
using GLMod.GLEntities;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    public class RankServiceTests
    {
        private const string RankJson = "{\"id\":3,\"name\":\"Gold\",\"percent\":42}";

        [Fact]
        public void CompleteRank_ValidResponse_ReturnsTheRank()
        {
            GLRank got = null;
            var logs = new List<string>();

            RankService.CompleteRank(RankJson, "mod", logs.Add, rank => got = rank);

            Assert.NotNull(got);
            Assert.Equal(3, got.id);
            Assert.Equal("Gold", got.name);
            Assert.Null(got.error);
            Assert.Empty(logs);
        }

        /// <summary>
        /// Bughunt k0021: onComplete was called inside the deserialization try, so an exception
        /// thrown by the caller's callback was logged as a parse error and the callback was called
        /// a second time with error "Parse error".
        /// </summary>
        [Fact]
        public void CompleteRank_CallbackThrows_InvokedOnce_k0021()
        {
            var ranks = new List<GLRank>();
            var logs = new List<string>();

            Assert.Throws<NullReferenceException>(() =>
                RankService.CompleteRank(RankJson, "mod", logs.Add, rank =>
                {
                    ranks.Add(rank);
                    throw new NullReferenceException();
                }));

            GLRank only = Assert.Single(ranks);
            Assert.Equal(3, only.id);
            Assert.Null(only.error);
            Assert.Empty(logs);
        }
    }
}
