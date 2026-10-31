using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GLMod.Enums;
using GLMod.GLEntities;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    /// <summary>
    /// Bughunt k0026: when POST /game/end failed, the host's Step stayed WinnerSet, so the next
    /// games were never started nor sent (and the old game was sent again at their end).
    /// </summary>
    public class EndGameTests
    {
        private static GLGame Game() => new GLGame("XXXXXX", "TheSkeld", false, "Vanilla") { id = "42" };

        [Fact]
        public void PostEndGame_HttpError_ResetsStep_k0026()
        {
            GameStep? step = null;
            var logs = new List<string>();

            RunWithStub("503 Service Unavailable", url =>
                GameStateManager.PostEndGame(url, Game(), logs.Add, s => step = s));

            Assert.Equal(GameStep.Initial, step);
            Assert.Contains(logs, l => l.StartsWith("[EndGame] End Game fail"));
        }

        [Fact]
        public void PostEndGame_NetworkError_ResetsStep_k0026()
        {
            // A port that was free a moment ago: the connection is refused
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            GameStep? step = null;
            Drive(GameStateManager.PostEndGame($"http://127.0.0.1:{port}/", Game(), null, s => step = s));

            Assert.Equal(GameStep.Initial, step);
        }

        [Fact]
        public void PostEndGame_Success_ResetsStep_k0026()
        {
            GameStep? step = null;
            var logs = new List<string>();

            RunWithStub("200 OK", url =>
                GameStateManager.PostEndGame(url, Game(), logs.Add, s => step = s));

            Assert.Equal(GameStep.Initial, step);
            Assert.Contains("Game ended.", logs);
        }

        /// <summary>
        /// k0027: a third-party mod starts the next game while /game/end is pending. The end of
        /// the request must not reset the new game's step, or AddPlayer and SendGame refuse it.
        /// </summary>
        [Theory]
        [InlineData("200 OK")]
        [InlineData("503 Service Unavailable")]
        public void PostEndGame_NewGameStartedWhilePending_KeepsItsStep_k0027(string status)
        {
            GLGame endedGame = Game();
            GLGame currentGame = endedGame;
            GameStep step = GameStep.WinnerSet;

            RunWithStub(status,
                url => GameStateManager.PostEndGame(url, endedGame, null, s => step = s, () => currentGame),
                onRequest: () =>
                {
                    currentGame = Game();
                    step = GameStep.PlayersAdded;
                });

            Assert.Equal(GameStep.PlayersAdded, step);
        }

        [Fact]
        public void PostEndGame_GameStillCurrent_ResetsStep_k0027()
        {
            GLGame game = Game();
            GameStep step = GameStep.WinnerSet;

            RunWithStub("200 OK", url => GameStateManager.PostEndGame(url, game, null, s => step = s, () => game));

            Assert.Equal(GameStep.Initial, step);
        }

        /// <summary>
        /// Serves one HTTP response with the given status and drives the coroutine to its end.
        /// onRequest runs once the request is received, before the response is sent.
        /// </summary>
        private static void RunWithStub(string status, Func<string, IEnumerator> coroutine, Action onRequest = null)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Task server = Task.Run(() => ServeOnce(listener, status, onRequest));

                Drive(coroutine($"http://127.0.0.1:{port}/"));
                server.Wait(TimeSpan.FromSeconds(10));
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void Drive(IEnumerator it)
        {
            var watch = Stopwatch.StartNew();
            while (it.MoveNext())
            {
                // Nested coroutines (yield return CoroutineHelpers.RunAsync) are driven here, like Unity does
                if (it.Current is IEnumerator inner)
                    Drive(inner);
                Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "coroutine timed out");
            }
        }

        private static void ServeOnce(TcpListener listener, string status, Action onRequest)
        {
            using TcpClient client = listener.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            // Read the request headers, then the JSON body
            var request = new StringBuilder();
            var buffer = new byte[4096];
            int headerEnd;
            while ((headerEnd = request.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal)) < 0)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0) return;
                request.Append(Encoding.ASCII.GetString(buffer, 0, read));
            }
            int contentLength = 0;
            foreach (string line in request.ToString(0, headerEnd).Split("\r\n"))
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    contentLength = int.Parse(line.Substring("Content-Length:".Length).Trim());
            }
            int remaining = contentLength - (request.Length - headerEnd - 4);
            while (remaining > 0)
            {
                int read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                if (read == 0) break;
                remaining -= read;
            }

            onRequest?.Invoke();

            byte[] payload = Encoding.UTF8.GetBytes("{}");
            byte[] head = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                $"Content-Length: {payload.Length}\r\n" +
                "Connection: close\r\n\r\n");
            stream.Write(head, 0, head.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }
    }
}
