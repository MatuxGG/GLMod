using GLMod.GLEntities;
using System.Collections;

namespace GLMod.Services.Interfaces
{
    /// <summary>
    /// The next planned lobby of the site, for the banner of the main menu. Needs no account: the
    /// player signs in on the page of the lobby. Main thread only.
    /// </summary>
    public interface IPlannedLobbyService
    {
        /// <summary>
        /// Last answer of the site, null until one is received
        /// </summary>
        GLNextLobby Next { get; }

        /// <summary>
        /// True when the banner has something to show: a lobby to come, not dismissed
        /// </summary>
        bool HasBanner { get; }

        /// <summary>
        /// Hides the banner until another lobby comes up, also after a restart
        /// </summary>
        void Dismiss();

        /// <summary>
        /// Reads the next lobby. A failure keeps the last answer and only logs.
        /// </summary>
        IEnumerator Reload();

        /// <summary>
        /// Reads it again on the main menu every LOBBY_REFRESH_SECONDS, unless the menu is hidden. Never ends.
        /// </summary>
        IEnumerator KeepFresh();
    }
}
