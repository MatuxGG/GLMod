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

        [Theory]
        [InlineData(InnerNetClientPatch.DISCONNECT_INTERNAL_RPC_ID, "ExitGame", "DC_INTERNAL_ExitGame")]
        [InlineData(InnerNetClientPatch.HANDLE_DISCONNECT_RPC_ID, "ExitGame", "DC_HANDLE_ExitGame")]
        [InlineData(InnerNetClientPatch.ON_DISCONNECT_RPC_ID, InnerNetClientPatch.ON_DISCONNECT_REASON, "DC_ON_On Disconnect")]
        public void LocalDisconnect_UsesSamePrefixAsRemote(int rpcId, string reason, string expected)
        {
            // Bughunt k0008: the local patches recorded the raw reason ("ExitGame") while the
            // other clients recorded the same disconnect with its DC_* prefix ("DC_INTERNAL_ExitGame").
            Assert.Equal(expected, InnerNetClientPatch.BuildDisconnectAction(rpcId, reason));
        }

        [Fact]
        public void DisconnectAction_NullReasonBecomesUnknown()
        {
            Assert.Equal("DC_HANDLE_unknown",
                InnerNetClientPatch.BuildDisconnectAction(InnerNetClientPatch.HANDLE_DISCONNECT_RPC_ID, null));
        }
    }
}
