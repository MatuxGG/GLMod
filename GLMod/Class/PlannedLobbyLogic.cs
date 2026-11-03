using GLMod.GLEntities;
using System;
using System.Globalization;

namespace GLMod.Class
{
    /// <summary>
    /// What the main menu banner of the next planned lobby shows, and when. Pure.
    /// </summary>
    public static class PlannedLobbyLogic
    {
        /// <summary>
        /// "{api}/lobbies/next?language=...&amp;mod=...", without the values that are not known (values escaped)
        /// </summary>
        public static string BuildUrl(string apiEndpoint, string path, string language, string mod)
        {
            string url = apiEndpoint + path;
            string separator = "?";
            if (!string.IsNullOrWhiteSpace(language))
            {
                url += separator + "language=" + Uri.EscapeDataString(language.Trim());
                separator = "&";
            }
            if (!string.IsNullOrWhiteSpace(mod))
            {
                url += separator + "mod=" + Uri.EscapeDataString(mod.Trim());
            }
            return url;
        }

        /// <summary>
        /// A lobby to come, with texts and a web page to open, that the player has not dismissed
        /// </summary>
        public static bool ShouldShow(GLNextLobby next, string dismissedId, DateTimeOffset now)
        {
            GLPlannedLobby lobby = next?.lobby;
            if (lobby == null || next.texts == null || string.IsNullOrEmpty(next.texts.action) || string.IsNullOrEmpty(next.texts.title))
            {
                return false;
            }
            if (!IsWebPage(lobby.url) || lobby.id.ToString(CultureInfo.InvariantCulture) == dismissedId)
            {
                return false;
            }
            return TryParseStart(lobby.startsAt, out DateTimeOffset start) && start > now;
        }

        /// <summary>
        /// The server decides what is opened in the browser of the player: nothing but a web page
        /// </summary>
        public static bool IsWebPage(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }

        /// <summary>
        /// "Next lobby · Sun 8 Nov 21:00 · 3/15 players", in the time zone of the player
        /// </summary>
        public static string Text(GLNextLobby next, TimeZoneInfo zone, CultureInfo culture)
        {
            GLPlannedLobby lobby = next.lobby;
            TryParseStart(lobby.startsAt, out DateTimeOffset start);
            string when = TimeZoneInfo.ConvertTime(start, zone).ToString("ddd d MMM HH:mm", culture);
            return next.texts.title + " · " + when + " · " + lobby.players + "/" + lobby.places + " " + next.texts.players;
        }

        /// <summary>
        /// Culture of a language code of the game, the invariant one when the runtime does not know it
        /// </summary>
        public static CultureInfo CultureOf(string language)
        {
            try
            {
                return string.IsNullOrWhiteSpace(language) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private static bool TryParseStart(string startsAt, out DateTimeOffset start)
        {
            return DateTimeOffset.TryParse(startsAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
        }
    }
}
