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

        public string Title { get; }

        public MenuTab(string title, Action draw, Action onShown = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("A menu tab needs a title", nameof(title));
            }

            Title = title;
            _draw = draw ?? throw new ArgumentNullException(nameof(draw));
            _onShown = onShown;
        }

        public void OnShown() => _onShown?.Invoke();

        public void Draw() => _draw();
    }
}
