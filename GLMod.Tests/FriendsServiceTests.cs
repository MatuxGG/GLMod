using GLMod.Class;
using GLMod.Constants;
using GLMod.Services.Implementations;
using GLMod.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GLMod.Tests
{
    public class FriendsServiceTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public class Fake<T> : DispatchProxy
        {
            public Func<MethodInfo, object> Handler;

            protected override object Invoke(MethodInfo method, object[] args) => Handler(method);
        }

        // The constructor takes a BepInEx logger, which this project does not reference: the logger is only used on failures
        private static IFriendsService CreateService(string apiEndpoint)
        {
            IAuthenticationService auth = DispatchProxy.Create<IAuthenticationService, Fake<IAuthenticationService>>();
            ((Fake<IAuthenticationService>)(object)auth).Handler = method =>
                method.Name == "get_BearerToken" ? "test" : (object)(method.Name == "get_IsLoggedIn");

            object service = RuntimeHelpers.GetUninitializedObject(typeof(FriendsService));
            SetField(service, "_authService", auth);
            SetField(service, "_apiEndpoint", apiEndpoint);
            SetField(service, "<Notifications>k__BackingField", new FriendNotificationQueue());
            return (IFriendsService)service;
        }

        private static void SetField(object target, string name, object value)
        {
            typeof(FriendsService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        // k0034: the sync of the action was skipped while another one was running, and that one wrote the list
        // it had read before the action: the accepted request stayed on screen until the next sync
        [Fact]
        public void Answer_WithASyncRunning_ReadsTheListAgainAfterIt_k0034()
        {
            using var firstSyncReceived = new ManualResetEventSlim();
            using var releaseFirstSync = new ManualResetEventSlim();
            using var answerReceived = new ManualResetEventSlim();
            int syncs = 0;
            using var api = new ApiStub(path =>
            {
                if (path == GameConstants.API_PATH_FRIENDS_ANSWER)
                {
                    answerReceived.Set();
                    return "{}";
                }
                if (Interlocked.Increment(ref syncs) > 1)
                {
                    return "{\"incoming\":[]}";
                }
                firstSyncReceived.Set();
                releaseFirstSync.Wait(Timeout);
                return "{\"incoming\":[{\"requestId\":7}]}";
            });
            IFriendsService service = CreateService(api.Url);
            int? pendingWhenDone = null;

            var sync = new Pump(service.Sync());
            sync.Step();
            Assert.True(firstSyncReceived.Wait(Timeout));

            var answer = new Pump(service.Answer(7, true, error =>
            {
                Assert.Null(error);
                pendingWhenDone = service.PendingRequestCount;
            }));
            var watch = Stopwatch.StartNew();
            while (!answerReceived.IsSet && watch.Elapsed < Timeout)
            {
                answer.Step();
            }
            Assert.True(answerReceived.IsSet);
            // Leaves the answer of the action the time to come back while the first sync is still running
            watch.Restart();
            while (!answer.Done && watch.ElapsedMilliseconds < 500)
            {
                answer.Step();
            }

            releaseFirstSync.Set();
            watch.Restart();
            while (!(sync.Done && answer.Done) && watch.Elapsed < Timeout)
            {
                sync.Step();
                answer.Step();
            }

            Assert.True(sync.Done && answer.Done, "coroutines timed out");
            Assert.Equal(2, Volatile.Read(ref syncs));
            Assert.Equal(0, service.PendingRequestCount);
            Assert.Equal(0, pendingWhenDone);
        }

        /// <summary>
        /// Runs nested IEnumerators like a Unity coroutine: Step is one frame.
        /// </summary>
        private sealed class Pump
        {
            private readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();

            public Pump(IEnumerator routine) => _stack.Push(routine);

            public bool Done => _stack.Count == 0;

            public void Step()
            {
                while (_stack.Count > 0)
                {
                    if (!_stack.Peek().MoveNext())
                    {
                        _stack.Pop();
                        continue;
                    }
                    if (_stack.Peek().Current is IEnumerator nested)
                    {
                        _stack.Push(nested);
                        continue;
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Local HTTP server answering each POST, at the same time, with the body given for its path.
        /// </summary>
        private sealed class ApiStub : IDisposable
        {
            private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly Func<string, string> _bodyOf;

            public string Url { get; }

            public ApiStub(Func<string, string> bodyOf)
            {
                _bodyOf = bodyOf;
                _listener.Start();
                Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
                _ = Task.Run(AcceptAll);
            }

            private void AcceptAll()
            {
                try
                {
                    while (true)
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        _ = Task.Run(() => Serve(client));
                    }
                }
                catch (SocketException)
                {
                    // Listener stopped
                }
            }

            private void Serve(TcpClient connection)
            {
                using TcpClient client = connection;
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
                string[] lines = request.ToString(0, headerEnd).Split("\r\n");
                int contentLength = 0;
                foreach (string line in lines)
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

                byte[] payload = Encoding.UTF8.GetBytes(_bodyOf(lines[0].Split(' ')[1]));
                byte[] head = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: application/json; charset=utf-8\r\n" +
                    $"Content-Length: {payload.Length}\r\n" +
                    "Connection: close\r\n\r\n");
                stream.Write(head, 0, head.Length);
                stream.Write(payload, 0, payload.Length);
                stream.Flush();
            }

            public void Dispose() => _listener.Stop();
        }
    }
}
