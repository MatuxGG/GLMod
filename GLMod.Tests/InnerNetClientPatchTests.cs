using Xunit;

namespace GLMod.Tests
{
    public class InnerNetClientPatchTests
    {
        [Fact]
        public void OnDisconnectRpc_UsesOnDisconnectCase()
        {
            // Bughunt k0001: OnDisconnect sent RPC 3 (HandleDisconnect) instead of RPC 4 (OnDisconnect).
            Assert.Equal(4, InnerNetClientPatch.ON_DISCONNECT_RPC_ID);
        }

        [Fact]
        public void OnDisconnectRpc_PayloadIsReasonThenPlayerName()
        {
            // Bughunt k0001: the payload was [playerName] only, so the receiver's values[1]
            // threw ArgumentOutOfRangeException and the disconnect was never recorded.
            var values = InnerNetClientPatch.BuildOnDisconnectRpcValues("Bob");

            Assert.Equal(new[] { "On Disconnect", "Bob" }, values);
        }

        [Fact]
        public void OnDisconnectRpc_NullPlayerNameBecomesEmpty()
        {
            var values = InnerNetClientPatch.BuildOnDisconnectRpcValues(null);

            Assert.Equal(new[] { "On Disconnect", "" }, values);
        }
    }
}
