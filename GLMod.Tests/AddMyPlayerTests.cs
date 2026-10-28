using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GLMod.Services.Implementations;
using Xunit;

namespace GLMod.Tests
{
    /// <summary>
    /// Bughunt k0019: AddMyPlayer called onComplete(true) even when POST /game/addMyPlayer
    /// failed, so a third-party mod could not detect the failure and retry.
    /// </summary>
    public class AddMyPlayerTests
    {
        private static Dictionary<string, string> Form()
            => new Dictionary<string, string> { { "gameId", "42" }, { "playerName", "Bob" } };

        [Fact]
        public void PostMyPlayer_HttpError_ReportsFalse_k0019()
        {
            bool? ok = null;
            var logs = new List<string>();

            RunWithStub("500 Internal Server Error", url =>
                GameStateManager.PostMyPlayer(url, Form(), "", logs.Add, result => ok = result));

            Assert.False(ok);
            Assert.Single(logs);
        }

        [Fact]
        public void PostMyPlayer_NetworkError_ReportsFalse_k0019()
        {
            // A port that was free a moment ago: the connection is refused
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            bool? ok = null;
            Drive(GameStateManager.PostMyPlayer($"http://127.0.0.1:{port}/", Form(), "", null, result => ok = result));

            Assert.False(ok);
        }

        [Fact]
        public void PostMyPlayer_Success_ReportsTrue_k0019()
        {
            bool? ok = null;

            RunWithStub("200 OK", url =>
                GameStateManager.PostMyPlayer(url, Form(), "", null, result => ok = result));

            Assert.True(ok);
        }

        /// <summary>
        /// Serves one HTTP response with the given status and drives the coroutine to its end.
        /// </summary>
        private static void RunWithStub(string status, Func<string, IEnumerator> coroutine)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Task server = Task.Run(() => ServeOnce(listener, status));

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
                // Nested coroutines (yield return ApiService.PostFormAsync) are driven here, like Unity does
                if (it.Current is IEnumerator inner)
                    Drive(inner);
                Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "coroutine timed out");
            }
        }

        private static void ServeOnce(TcpListener listener, string status)
        {
            using TcpClient client = listener.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            // Read the request headers, then the form body
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
