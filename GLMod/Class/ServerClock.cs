using System;
using System.Threading;
using GLMod.Constants;

namespace GLMod.Class
{
    /// <summary>
    /// Timestamps aligned on the Good Loss server clock. The server compares action and position
    /// timestamps with its own game start/end times, so a player whose system clock is wrong would
    /// otherwise produce a recap without timeline nor map. The offset is measured from the HTTP
    /// Date header of every API response (see ApiService).
    /// </summary>
    public static class ServerClock
    {
        private static long _offsetMs;

        /// <summary>
        /// Current offset (server - local), in milliseconds. 0 until a response has been received.
        /// </summary>
        public static long OffsetMs => Interlocked.Read(ref _offsetMs);

        /// <summary>
        /// Unix time in milliseconds, corrected by the server offset. Use it for every timestamp sent to the API.
        /// </summary>
        public static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + OffsetMs;
        }

        /// <summary>
        /// Updates the offset from a response Date header. Thread-safe: called from HTTP tasks.
        /// A measurement within the Date header resolution (1s) of the current offset is noise and is
        /// ignored: replacing the offset would make NowMs go back in the middle of a game.
        /// </summary>
        public static void Update(DateTimeOffset? serverDate, DateTimeOffset requestSentAt, DateTimeOffset responseReceivedAt)
        {
            long? offset = ComputeOffsetMs(serverDate, requestSentAt, responseReceivedAt);
            if (!offset.HasValue)
            {
                return;
            }

            if (Math.Abs(RawOffsetMs(serverDate.Value, requestSentAt, responseReceivedAt) - OffsetMs) <= GameConstants.CLOCK_DATE_RESOLUTION_MS)
            {
                return;
            }

            Interlocked.Exchange(ref _offsetMs, offset.Value);
        }

        /// <summary>
        /// Offset between the server and the local clock, or null when the response has no Date header.
        /// The Date header has a 1s resolution, so the server time is taken in the middle of its second
        /// and compared with the middle of the request; offsets within the tolerance count as 0.
        /// </summary>
        public static long? ComputeOffsetMs(DateTimeOffset? serverDate, DateTimeOffset requestSentAt, DateTimeOffset responseReceivedAt)
        {
            if (!serverDate.HasValue)
            {
                return null;
            }

            long offset = RawOffsetMs(serverDate.Value, requestSentAt, responseReceivedAt);

            return Math.Abs(offset) <= GameConstants.CLOCK_SKEW_TOLERANCE_MS ? 0 : offset;
        }

        private static long RawOffsetMs(DateTimeOffset serverDate, DateTimeOffset requestSentAt, DateTimeOffset responseReceivedAt)
        {
            long localMidpoint = (requestSentAt.ToUnixTimeMilliseconds() + responseReceivedAt.ToUnixTimeMilliseconds()) / 2;
            long serverMidpoint = serverDate.ToUnixTimeMilliseconds() + 500;
            return serverMidpoint - localMidpoint;
        }

        /// <summary>
        /// Resets the offset (tests only).
        /// </summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _offsetMs, 0);
        }
    }
}
