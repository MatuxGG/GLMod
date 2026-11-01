using System.Collections.Generic;

namespace GLMod.Class
{
    /// <summary>
    /// Friend notifications waiting to be shown, one at a time. Nothing appears during a game: requests
    /// wait for the lobby (the badge of the menu counts them meanwhile), connection alerts are dropped.
    /// Pure.
    /// </summary>
    public sealed class FriendNotificationQueue
    {
        private readonly Queue<(string key, string name, bool isAlert)> _waiting = new Queue<(string key, string name, bool isAlert)>();
        private (string key, string name)? _shown;
        private bool _shownIsAlert;
        private float _shownUntil;

        /// <summary>
        /// Adds a notification: a translation key and the name it mentions.
        /// </summary>
        public void Enqueue(string key, string name)
        {
            _waiting.Enqueue((key, name, false));
        }

        /// <summary>
        /// Adds a connection alert: lost rather than delayed, it would no longer be news.
        /// </summary>
        public void EnqueueAlert(string key, string name)
        {
            _waiting.Enqueue((key, name, true));
        }

        /// <summary>
        /// Forgets the connection alerts still waiting. The other notifications keep their order.
        /// </summary>
        public void DropAlerts()
        {
            for (int remaining = _waiting.Count; remaining > 0; remaining--)
            {
                (string key, string name, bool isAlert) item = _waiting.Dequeue();
                if (!item.isAlert)
                {
                    _waiting.Enqueue(item);
                }
            }
        }

        public int Count => _waiting.Count + (_shown.HasValue ? 1 : 0);

        /// <summary>
        /// The notification to display now, null when there is none or during a game.
        /// </summary>
        public (string key, string name)? Current(float now, float durationSeconds, bool inGame)
        {
            if (_shown.HasValue && now >= _shownUntil)
            {
                _shown = null;
            }

            if (inGame)
            {
                DropAlerts();
                // Interrupted on screen: an alert is lost like the others, a request starts over in the lobby
                if (_shownIsAlert)
                {
                    _shown = null;
                }
                _shownUntil = float.PositiveInfinity;
                return null;
            }

            if (_shown.HasValue && float.IsPositiveInfinity(_shownUntil))
            {
                _shownUntil = now + durationSeconds;
            }
            if (!_shown.HasValue && _waiting.Count > 0)
            {
                (string key, string name, bool isAlert) next = _waiting.Dequeue();
                _shown = (next.key, next.name);
                _shownIsAlert = next.isAlert;
                _shownUntil = now + durationSeconds;
            }
            return _shown;
        }
    }
}
