using System;
using GLMod.Class;
using GLMod.Constants;
using Xunit;

namespace GLMod.Tests
{
    public class ServerClockTests
    {
        private static readonly DateTimeOffset LocalSent = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);
        private static readonly DateTimeOffset LocalReceived = LocalSent.AddMilliseconds(200);

        [Fact]
        public void ComputeOffset_NullWithoutDateHeader()
        {
            Assert.Null(ServerClock.ComputeOffsetMs(null, LocalSent, LocalReceived));
        }

        [Fact]
        public void ComputeOffset_ZeroWithinTolerance()
        {
            // Server date truncated to the second, same instant as the request
            DateTimeOffset serverDate = DateTimeOffset.FromUnixTimeSeconds(LocalSent.ToUnixTimeSeconds());

            Assert.Equal(0, ServerClock.ComputeOffsetMs(serverDate, LocalSent, LocalReceived));
        }

        [Fact]
        public void ComputeOffset_DetectsLocalClockTwoHoursBehind()
        {
            DateTimeOffset serverDate = DateTimeOffset.FromUnixTimeSeconds(LocalSent.AddHours(2).ToUnixTimeSeconds());

            long? offset = ServerClock.ComputeOffsetMs(serverDate, LocalSent, LocalReceived);

            Assert.NotNull(offset);
            long twoHours = (long)TimeSpan.FromHours(2).TotalMilliseconds;
            Assert.InRange(offset.Value, twoHours - GameConstants.CLOCK_SKEW_TOLERANCE_MS, twoHours + GameConstants.CLOCK_SKEW_TOLERANCE_MS);
        }

        [Fact]
        public void ComputeOffset_DetectsLocalClockAhead()
        {
            DateTimeOffset serverDate = DateTimeOffset.FromUnixTimeSeconds(LocalSent.AddMinutes(-10).ToUnixTimeSeconds());

            Assert.True(ServerClock.ComputeOffsetMs(serverDate, LocalSent, LocalReceived) < -GameConstants.CLOCK_SKEW_TOLERANCE_MS);
        }

        [Fact]
        public void Update_ShiftsNowMs()
        {
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                ServerClock.Update(now.AddHours(1), now, now);

                long expected = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds();
                Assert.InRange(ServerClock.NowMs(), expected - 2000, expected + 2000);

                // A response without Date keeps the previous offset
                long offset = ServerClock.OffsetMs;
                ServerClock.Update(null, now, now);
                Assert.Equal(offset, ServerClock.OffsetMs);
            }
            finally
            {
                ServerClock.Reset();
            }
        }

        [Fact]
        public void Update_IgnoresMeasurementWithinDateResolution_k0022()
        {
            // Local clock ~2.2s behind: the 1s Date header resolution makes the measurement cross the
            // tolerance, which used to reset the offset to 0 mid-game and make NowMs go back
            try
            {
                ServerClock.Reset();
                DateTimeOffset first = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_300);
                ServerClock.Update(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_002_000), first, first);
                Assert.Equal(2200, ServerClock.OffsetMs);

                DateTimeOffset second = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_010_600);
                ServerClock.Update(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_012_000), second, second);
                Assert.Equal(2200, ServerClock.OffsetMs);
            }
            finally
            {
                ServerClock.Reset();
            }
        }
    }
}
