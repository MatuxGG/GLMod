using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GLMod.Class;
using Xunit;

namespace GLMod.Tests
{
    /// <summary>
    /// ApiService against a local TCP stub server (HttpHelper.Client is shared and not injectable).
    /// </summary>
    public class ApiServiceTests
    {
        // Bughunt k0010: Regex.Unescape turned \" into a raw quote and \\ into a lone backslash (invalid JSON)
        private const string EscapedJson = "{\"name\":\"a\\\"b\\\\c\"}";

        [Fact]
        public void PostFormAsync_KeepsJsonEscapes_k0010()
        {
            string got = null;
            string error = null;

            RunWithStub(EscapedJson, url => ApiService.PostFormAsync(url, new Dictionary<string, string>(),
                result => got = result,
                err => error = err));

            Assert.Null(error);
            Assert.Equal(EscapedJson, got);
            Assert.Equal("a\"b\\c", JsonDocument.Parse(got).RootElement.GetProperty("name").GetString());
        }

        [Fact]
        public void PostFormWithErrorHandlingAsync_KeepsJsonEscapes_k0010()
        {
            ApiResponse got = null;

            RunWithStub(EscapedJson, url => ApiService.PostFormWithErrorHandlingAsync(url, new Dictionary<string, string>(),
                result => got = result));

            Assert.NotNull(got);
            Assert.True(got.IsSuccess);
            Assert.Equal(200, got.StatusCode);
            Assert.Equal(EscapedJson, got.Content);
            Assert.Equal("a\"b\\c", JsonDocument.Parse(got.Content).RootElement.GetProperty("name").GetString());
        }

        /// <summary>
        /// Serves one HTTP 200 response with the given JSON body and drives the coroutine to its end.
        /// </summary>
        private static void RunWithStub(string body, Func<string, IEnumerator> coroutine)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Task server = Task.Run(() => ServeOnce(listener, body));

                IEnumerator it = coroutine($"http://127.0.0.1:{port}/");
                var watch = Stopwatch.StartNew();
                while (it.MoveNext())
                {
                    Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "coroutine timed out");
                }
                server.Wait(TimeSpan.FromSeconds(10));
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void ServeOnce(TcpListener listener, string body)
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

            byte[] payload = Encoding.UTF8.GetBytes(body);
            byte[] head = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                $"Content-Length: {payload.Length}\r\n" +
                "Connection: close\r\n\r\n");
            stream.Write(head, 0, head.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }
    }
}
