using System.Collections.Generic;
using System.Linq;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Pure checks of the answers of GLPlugin (plugin scenario). The texts are translated in the language
    /// of the game, so only what never changes is compared: command names and game codes.
    /// </summary>
    public static class PluginChecks
    {
        public const string COMMAND_HELP = "#help";
        public const string COMMAND_CODE = "#code";
        public const string COMMAND_ROOM = "#room";
        public const string COMMAND_KICK = "#kick";
        public const string COMMAND_BAN = "#ban";

        /// <summary>
        /// What is wrong with the answer of #help, null when it is right: every player sees #help and
        /// #code, only the host sees #kick and #ban, and #room does not exist anymore.
        /// </summary>
        public static string VerifyHelp(string answer, bool isHost)
        {
            if (string.IsNullOrEmpty(answer)) return "no answer to " + COMMAND_HELP;

            foreach (string command in new[] { COMMAND_HELP, COMMAND_CODE })
            {
                if (!answer.Contains(command + ":")) return $"{COMMAND_HELP} does not list {command}";
            }
            foreach (string command in new[] { COMMAND_KICK, COMMAND_BAN })
            {
                if (answer.Contains(command + ":") != isHost)
                {
                    return $"{COMMAND_HELP} {(isHost ? "does not list" : "shows")} {command} {(isHost ? "for the host" : "to a player")}";
                }
            }
            if (answer.Contains(COMMAND_ROOM + ":")) return $"{COMMAND_HELP} still lists {COMMAND_ROOM}";
            return null;
        }

        /// <summary>
        /// The answer of #help among the chat lines: its first line lists the commands.
        /// </summary>
        public static string FindHelpAnswer(IEnumerable<string> chatLines)
        {
            return chatLines.LastOrDefault(line => line != null && line.Contains(COMMAND_CODE + ":"));
        }

        /// <summary>
        /// True when a line other than the sent command itself mentions the text.
        /// </summary>
        public static bool IsAnswered(IEnumerable<string> chatLines, string command, string expected)
        {
            return chatLines.Any(line => line != null && line != command && line.Contains(expected));
        }
    }
}
