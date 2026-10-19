using System;
using System.Text;
using GLMod.Constants;
using GLMod.GLEntities;

namespace GLMod.Class
{
    /// <summary>
    /// Pure helpers of the Good Loss login (/user/login): Steam ticket encoding,
    /// reading the answers of the Steam ticket and legacy logins. No IO, no logging.
    /// </summary>
    public static class LoginResponse
    {
        private const string BANNED_PREFIX = "Banned: ";

        /// <summary>
        /// Upper-case hex of the first length bytes of a Steam ticket buffer, or null when length is out of range.
        /// </summary>
        public static string ToHex(byte[] buffer, uint length)
        {
            if (buffer == null || length == 0 || length > buffer.Length)
            {
                return null;
            }

            var hex = new StringBuilder((int)length * 2);
            for (int i = 0; i < length; i++)
            {
                hex.Append(buffer[i].ToString("X2"));
            }
            return hex.ToString();
        }

        /// <summary>
        /// Reads the JSON answer of a Steam ticket login; false unless it holds a game token and an account name.
        /// </summary>
        public static bool TryParseTicketLogin(string body, out GLLoginResponse login)
        {
            if (!PlayerApiResponse.TryParse(body, out login)
                || string.IsNullOrEmpty(login.token)
                || !login.token.StartsWith(GameConstants.GAME_TOKEN_PREFIX, StringComparison.Ordinal)
                || string.IsNullOrEmpty(login.username))
            {
                login = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Ban reason of a 403 "Banned: reason" answer, or null when the body is not a ban.
        /// </summary>
        public static string ReadBanReason(string body)
        {
            string trimmed = body?.Trim('"').Trim();
            if (string.IsNullOrEmpty(trimmed) || !trimmed.StartsWith(BANNED_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return trimmed.Substring(BANNED_PREFIX.Length);
        }

        /// <summary>
        /// Account name of a legacy token ("username#keycloakId"), or "" when it has none.
        /// </summary>
        public static string AccountNameOfLegacyToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "";
            }
            int hashIndex = token.IndexOf('#');
            return hashIndex > 0 ? token.Substring(0, hashIndex) : "";
        }

        /// <summary>
        /// Whether a game token expiring at expiresAt (Unix s) can still be reused at nowSeconds.
        /// </summary>
        public static bool CanReuseToken(long? expiresAt, long nowSeconds)
        {
            return expiresAt.HasValue && expiresAt.Value - nowSeconds >= GameConstants.GAME_TOKEN_REUSE_MARGIN_SECONDS;
        }
    }
}
