using GLMod.Constants;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Steamworks;

namespace GLMod.Class
{
    /// <summary>
    /// Steam session ticket proving the local SteamID to the Good Loss API (/user/login).
    /// Among Us ships a Steam SDK without GetAuthTicketForWebApi; the site validates this
    /// GetAuthSessionTicket ticket for the Among Us app id. Main thread only (Steam API).
    /// </summary>
    public static class SteamSessionTicket
    {
        /// <summary>
        /// Asks Steam for a ticket; on success the caller must Cancel the handle once the login answered.
        /// </summary>
        public static bool TryCreate(out string ticketHex, out HAuthTicket handle)
        {
            ticketHex = null;
            var buffer = new Il2CppStructArray<byte>(GameConstants.STEAM_TICKET_BUFFER_SIZE);
            handle = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out uint length);
            if (handle.m_HAuthTicket == 0)
            {
                return false;
            }

            var bytes = new byte[buffer.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = buffer[i];
            }
            ticketHex = LoginResponse.ToHex(bytes, length);
            if (ticketHex == null)
            {
                Cancel(handle);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Invalidates a ticket: it can no longer be validated (nor replayed) by anyone.
        /// </summary>
        public static void Cancel(HAuthTicket handle)
        {
            if (handle.m_HAuthTicket != 0)
            {
                SteamUser.CancelAuthTicket(handle);
            }
        }
    }
}
