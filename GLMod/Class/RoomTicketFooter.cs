using GLMod.Constants;
using System;
using System.Text;

namespace GLMod.Class
{
    /// <summary>
    /// Appends the room ticket to the connection data of the game: [ticket, ASCII][length, 2 bytes
    /// little endian][marker]. GLPlugin reads it from the end (Class/HandshakeTicket.cs). Pure.
    /// </summary>
    public static class RoomTicketFooter
    {
        private const int LengthSize = 2;

        public static byte[] Append(byte[] connectionData, string ticket)
        {
            if (connectionData == null) throw new ArgumentNullException(nameof(connectionData));
            if (string.IsNullOrEmpty(ticket)) return connectionData;

            byte[] raw = Encoding.ASCII.GetBytes(ticket);
            byte[] marker = Encoding.ASCII.GetBytes(GameConstants.ROOM_TICKET_MARKER);
            var result = new byte[connectionData.Length + raw.Length + LengthSize + marker.Length];

            connectionData.CopyTo(result, 0);
            raw.CopyTo(result, connectionData.Length);
            int offset = connectionData.Length + raw.Length;
            result[offset] = (byte)(raw.Length & 0xFF);
            result[offset + 1] = (byte)(raw.Length >> 8);
            marker.CopyTo(result, offset + LengthSize);
            return result;
        }
    }
}
