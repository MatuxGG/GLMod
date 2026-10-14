using System;

namespace GLMod.Menu
{
    /// <summary>
    /// Menu tab built from delegates, for mods that do not need their own IMenuTab class.
    /// </summary>
    public sealed class MenuTab : IMenuTab
    {
        private readonly Action _draw;
        private readonly Action _onShown;

        public string Id { get; }

        public string Title { get; }

        public MenuTab(string id, string title, Action draw, Action onShown = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A menu tab needs an id", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A menu tab needs a title", nameof(title));
            }

            Id = id;
            Title = title;
            _draw = draw ?? throw new ArgumentNullException(nameof(draw));
            _onShown = onShown;
        }

        public void OnShown() => _onShown?.Invoke();

        public void Draw() => _draw();
    }
}
