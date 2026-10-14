using GLMod.GLEntities;
using GLMod.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GLMod.Menu
{
    /// <summary>
    /// Last game with a link to its recap, then the local history.
    /// Selected automatically when a game ends, so the next opening shows the recap link.
    /// </summary>
    public class GamesMenuTab : IMenuTab
    {
        private const string VanillaRolePrefix = "Vanilla";

        private readonly IGameHistoryService _historyService;

        public string Title => "Games";

        public GamesMenuTab(IGameHistoryService historyService, IMenuService menuService)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
            if (menuService == null)
            {
                throw new ArgumentNullException(nameof(menuService));
            }

            _historyService.EntryRecorded += _ =>
            {
                if (!menuService.IsOpen)
                {
                    menuService.Select(this);
                }
            };
        }

        public void OnShown()
        {
        }

        public void Draw()
        {
            IReadOnlyList<GLGameHistoryEntry> entries = _historyService.Entries;
            MenuUI.Heading("Last game");
            if (entries.Count == 0)
            {
                MenuUI.Muted("No game played with GLMod yet. Your games will be listed here.");
                return;
            }

            GLGameHistoryEntry latest = entries[0];
            MenuUI.Label(latest.won ? "Victory" : "Defeat", latest.won ? MenuUI.Success : MenuUI.Danger);
            MenuUI.Muted(string.Join("  ·  ", new[] { latest.map, DisplayRole(latest), latest.modName }));
            MenuUI.Muted(FormatDate(latest.endedAtMs));
            if (!string.IsNullOrEmpty(latest.recapUrl))
            {
                if (MenuUI.Button("Open the recap"))
                {
                    Application.OpenURL(latest.recapUrl);
                }
            }
            else
            {
                MenuUI.Muted("This game was not recorded on Good Loss.");
            }

            if (entries.Count < 2)
            {
                return;
            }

            MenuUI.Space();
            MenuUI.Heading("History");
            for (int i = 1; i < entries.Count; i++)
            {
                GLGameHistoryEntry entry = entries[i];
                string text = FormatDate(entry.endedAtMs) + "  ·  " + entry.map + "  ·  " + (entry.won ? "Victory" : "Defeat");
                string button = string.IsNullOrEmpty(entry.recapUrl) ? null : "Recap";
                if (MenuUI.Row(text, button, entry.won ? MenuUI.Text : MenuUI.MutedText))
                {
                    Application.OpenURL(entry.recapUrl);
                }
            }
        }

        private static string DisplayRole(GLGameHistoryEntry entry)
        {
            // Vanilla roles are recorded as "Vanilla<RoleTypes>"
            string role = entry.role ?? "";
            if (role.StartsWith(VanillaRolePrefix, StringComparison.Ordinal))
            {
                role = role.Substring(VanillaRolePrefix.Length);
            }
            return string.IsNullOrEmpty(role) ? entry.team : role;
        }

        private static string FormatDate(long unixMs)
        {
            if (unixMs <= 0)
            {
                return "-";
            }

            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        }
    }
}
