using GLMod.Class;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GLModPlugin = global::GLMod.GLMod;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Plugin scenario: checks GLPlugin on the beta server, in a lobby, without playing.
    /// Instance 0 (host): its game gets the room code of the account, welcome message, host commands.
    /// Instance 1: creates a game while the code is taken (it gets another one), then joins the host and
    /// tries the player commands. Instance 2: joins and stays. Instance 3: gets kicked, comes back, gets banned.
    /// Every instance shares the Steam account, hence the room code, and the IP (a ban applies to the IP:
    /// it comes last, and only to players joining afterwards).
    /// </summary>
    public static partial class ScenarioRunner
    {
        private const int InstanceSecondCreator = 1;
        private const int InstanceWatcher = 2;
        private const int InstanceVictim = 3;

        private const string SignalJoinedFormat = "joined-{0}";
        private const string SignalFull = "full";
        private const string SignalPlayerHelpDone = "player-help-done";
        private const string SignalHostHelpDone = "host-help-done";
        private const string SignalDeniedKickSent = "denied-kick-sent";
        private const string SignalDeniedKickChecked = "denied-kick-checked";
        private const string SignalKicked = "kicked";
        private const string SignalRejoined = "rejoined";
        private const string SignalBanned = "banned";
        private const string SignalBanChecked = "ban-checked";
        private const string SignalFinished = "finished";

        private static readonly List<string> _chat = new List<string>();

        /// <summary>
        /// Called for every chat line shown by the game.
        /// </summary>
        public static void OnChat(string sender, string text)
        {
            if (_context?.Scenario != HarnessConstants.SCENARIO_PLUGIN) return;
            _chat.Add(text);
            HarnessLog.Info($"Chat [{sender}]: {text?.Replace('\n', '|')}");
        }

        private static IEnumerator PlayPluginScenario()
        {
            SetPhase("plugin:ticket");
            yield return WaitFor("a room ticket from the API", () => GLModPlugin.RoomTicketService?.Ticket != null, HarnessConstants.PLUGIN_STEP_TIMEOUT);
            if (_finished) yield break;

            string roomCode = GLModPlugin.RoomTicketService.RoomCode;
            HarnessLog.Info("Room code of the account: " + (roomCode ?? "none"));
            if (roomCode == null)
            {
                Fail("The account has no room code: set one on /account of the site of this API");
                yield break;
            }

            switch (_context.Instance)
            {
                case HarnessConstants.HOST_INSTANCE: yield return PluginHost(roomCode); break;
                case InstanceSecondCreator: yield return PluginSecondCreator(roomCode); break;
                case InstanceWatcher: yield return PluginWatcher(); break;
                case InstanceVictim: yield return PluginVictim(); break;
            }
        }

        private static IEnumerator PluginHost(string roomCode)
        {
            SetPhase("plugin:create");
            if (!Try("create the online game", HostOnlineGame)) yield break;
            yield return WaitFor("host in lobby", IsInLobby, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;

            string code = PlayModeHelper.CurrentOnlineGameCode();
            HarnessLog.Info("Game code: " + code);
            Check(code == roomCode, $"the game of the host has the code {code}, not the room code {roomCode} of the account");
            if (!Try("publish the lobby code", () => WriteLobbyFile(1))) yield break;

            SetPhase("plugin:welcome");
            yield return WaitFor("the welcome message", () => _chat.Any(line => line.Contains(_context.Region)), HarnessConstants.PLUGIN_STEP_TIMEOUT);
            if (_finished) yield break;

            yield return CapturePanel("panel-host.png");

            SetPhase("plugin:lobby");
            yield return WaitFor($"{HarnessConstants.PLAYER_COUNT} players in lobby", IsLobbyFull, HarnessConstants.LOBBY_FULL_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
            Signal(SignalFull);

            SetPhase("plugin:help");
            yield return WaitSignal(SignalPlayerHelpDone);
            if (_finished) yield break;
            _chat.Clear();
            if (!Try("send #help", () => SendChat(PluginChecks.COMMAND_HELP))) yield break;
            yield return WaitFor("the answer to #help", () => PluginChecks.FindHelpAnswer(_chat) != null, HarnessConstants.PLUGIN_STEP_TIMEOUT, required: false);
            CheckNoFailure(PluginChecks.VerifyHelp(PluginChecks.FindHelpAnswer(_chat), isHost: true));
            Signal(SignalHostHelpDone);

            SetPhase("plugin:denied-kick");
            yield return WaitSignal(SignalDeniedKickSent);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_SILENCE_DELAY);
            Check(PlayerControl.AllPlayerControls.Count == HarnessConstants.PLAYER_COUNT, "#kick worked for a player who is not the host");
            Signal(SignalDeniedKickChecked);

            SetPhase("plugin:kick");
            string victim = null;
            yield return WaitFor("the name of the victim", () => (victim = ReadSignal(string.Format(SignalJoinedFormat, InstanceVictim))) != null, HarnessConstants.PLUGIN_STEP_TIMEOUT);
            if (_finished) yield break;
            if (!Try("send #kick", () => SendChat($"{PluginChecks.COMMAND_KICK} {victim}"))) yield break;
            yield return WaitFor("the victim kicked", () => PlayerControl.AllPlayerControls.Count == HarnessConstants.PLAYER_COUNT - 1, HarnessConstants.PLUGIN_STEP_TIMEOUT);
            if (_finished) yield break;
            Signal(SignalKicked);

            SetPhase("plugin:kick-unknown");
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_CHAT_DELAY);
            _chat.Clear();
            string unknown = $"{PluginChecks.COMMAND_KICK} {HarnessConstants.PLUGIN_UNKNOWN_PLAYER}";
            if (!Try("send #kick to an unknown player", () => SendChat(unknown))) yield break;
            yield return WaitForQuiet(() => _chat.Any(line => line != unknown), HarnessConstants.PLUGIN_STEP_TIMEOUT);
            Check(_chat.Any(line => line != unknown), "no answer to #kick for an unknown player");

            SetPhase("plugin:ban");
            yield return WaitSignal(SignalRejoined);
            if (_finished) yield break;
            yield return WaitFor("the victim back", () => PlayerControl.AllPlayerControls.Count == HarnessConstants.PLAYER_COUNT, HarnessConstants.PLUGIN_STEP_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
            victim = ReadSignal(SignalRejoined);
            if (!Try("send #ban", () => SendChat($"{PluginChecks.COMMAND_BAN} {victim}"))) yield break;
            yield return WaitFor("the victim banned", () => PlayerControl.AllPlayerControls.Count == HarnessConstants.PLAYER_COUNT - 1, HarnessConstants.PLUGIN_STEP_TIMEOUT);
            if (_finished) yield break;
            Signal(SignalBanned);

            yield return WaitSignal(SignalBanChecked);
            Signal(SignalFinished);
        }

        private static IEnumerator PluginSecondCreator(string roomCode)
        {
            LobbyInfo lobby = null;
            SetPhase("plugin:second-creator");
            yield return WaitFor("lobby code published by host", () => (lobby = TryLoadLobby(1)) != null, HarnessConstants.LOBBY_FILE_TIMEOUT);
            if (_finished) yield break;

            // Same account, so same room code: it is taken by the game of the host.
            if (!Try("create a second online game", HostOnlineGame)) yield break;
            yield return WaitFor("second game created", IsInLobby, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;

            string code = PlayModeHelper.CurrentOnlineGameCode();
            HarnessLog.Info("Code of the second game: " + code);
            Check(code != null && code.Length == HarnessConstants.PLUGIN_ROOM_CODE_LENGTH && code != roomCode,
                $"the second creator got {code}, not another code than the taken room code {roomCode}");

            yield return CapturePanel("panel-second-creator.png");

            // Like the Leave button of the game: the scene change is not part of ExitGame.
            if (!Try("leave the second game", () =>
            {
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                SceneChanger.ChangeScene(HarnessConstants.MAIN_MENU_SCENE);
            })) yield break;
            yield return WaitFor("back to the main menu", IsAtMainMenu, HarnessConstants.CONNECT_TIMEOUT);
            if (_finished) yield break;

            yield return PluginJoin(lobby, "plugin:join");
            if (_finished) yield break;
            Signal(string.Format(SignalJoinedFormat, _context.Instance), LocalName());

            yield return WaitSignal(SignalFull);
            if (_finished) yield break;

            SetPhase("plugin:help");
            _chat.Clear();
            if (!Try("send #help", () => SendChat(PluginChecks.COMMAND_HELP))) yield break;
            yield return WaitFor("the answer to #help", () => PluginChecks.FindHelpAnswer(_chat) != null, HarnessConstants.PLUGIN_STEP_TIMEOUT, required: false);
            CheckNoFailure(PluginChecks.VerifyHelp(PluginChecks.FindHelpAnswer(_chat), isHost: false));
            Signal(SignalPlayerHelpDone);

            yield return WaitSignal(SignalHostHelpDone);
            if (_finished) yield break;

            SetPhase("plugin:code");
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_CHAT_DELAY);
            _chat.Clear();
            if (!Try("send #code", () => SendChat(PluginChecks.COMMAND_CODE))) yield break;
            yield return WaitForQuiet(() => PluginChecks.IsAnswered(_chat, PluginChecks.COMMAND_CODE, roomCode), HarnessConstants.PLUGIN_STEP_TIMEOUT);
            Check(PluginChecks.IsAnswered(_chat, PluginChecks.COMMAND_CODE, roomCode), $"#code did not answer {roomCode}");

            SetPhase("plugin:room-removed");
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_CHAT_DELAY);
            string room = $"{PluginChecks.COMMAND_ROOM} {HarnessConstants.PLUGIN_UNKNOWN_ROOM}";
            if (!Try("send #room", () => SendChat(room))) yield break;
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_SILENCE_DELAY);
            Check(!PluginChecks.IsAnswered(_chat, room, HarnessConstants.PLUGIN_UNKNOWN_ROOM), "#room still answers: the command should be gone");

            SetPhase("plugin:denied-kick");
            string watcher = ReadSignal(string.Format(SignalJoinedFormat, InstanceWatcher));
            if (!Try("send #kick as a player", () => SendChat($"{PluginChecks.COMMAND_KICK} {watcher}"))) yield break;
            Signal(SignalDeniedKickSent);

            yield return WaitSignal(SignalFinished);
        }

        private static IEnumerator PluginWatcher()
        {
            LobbyInfo lobby = null;
            yield return WaitFor("lobby code published by host", () => (lobby = TryLoadLobby(1)) != null, HarnessConstants.LOBBY_FILE_TIMEOUT);
            if (_finished) yield break;
            yield return WaitSignal(string.Format(SignalJoinedFormat, InstanceSecondCreator));
            if (_finished) yield break;

            yield return PluginJoin(lobby, "plugin:join");
            if (_finished) yield break;
            Signal(string.Format(SignalJoinedFormat, _context.Instance), LocalName());

            SetPhase("plugin:watch");
            yield return WaitSignal(SignalFinished);
            Check(IsInLobby(), "the watcher was disconnected: a #kick or #ban hit the wrong player");
        }

        private static IEnumerator PluginVictim()
        {
            LobbyInfo lobby = null;
            yield return WaitFor("lobby code published by host", () => (lobby = TryLoadLobby(1)) != null, HarnessConstants.LOBBY_FILE_TIMEOUT);
            if (_finished) yield break;
            yield return WaitSignal(string.Format(SignalJoinedFormat, InstanceWatcher));
            if (_finished) yield break;

            yield return PluginJoin(lobby, "plugin:join");
            if (_finished) yield break;
            Signal(string.Format(SignalJoinedFormat, _context.Instance), LocalName());

            SetPhase("plugin:kicked");
            yield return WaitFor("being kicked", IsAtMainMenu, HarnessConstants.PLUGIN_KICK_TIMEOUT);
            if (_finished) yield break;
            DisconnectReasons kickReason = AmongUsClient.Instance.LastDisconnectReason;
            Check(kickReason == DisconnectReasons.Kicked, $"kicked with the reason {kickReason}");

            // A kick is not a ban: the player can come back.
            yield return WaitSignal(SignalKicked);
            if (_finished) yield break;
            yield return PluginJoin(lobby, "plugin:rejoin");
            if (_finished) yield break;
            Signal(SignalRejoined, LocalName());

            SetPhase("plugin:banned");
            yield return WaitFor("being banned", IsAtMainMenu, HarnessConstants.PLUGIN_KICK_TIMEOUT);
            if (_finished) yield break;
            DisconnectReasons banReason = AmongUsClient.Instance.LastDisconnectReason;
            Check(banReason == DisconnectReasons.Banned, $"banned with the reason {banReason}");

            yield return WaitSignal(SignalBanned);
            if (_finished) yield break;

            SetPhase("plugin:ban-rejoin");
            if (!Try("try to join after the ban", () => JoinOnlineGame(lobby.GameId))) yield break;
            yield return WaitForQuiet(IsInLobby, HarnessConstants.PLUGIN_SILENCE_DELAY * 3);
            Check(!IsInLobby(), "joined again after #ban");
            Signal(SignalBanChecked);
        }

        private static IEnumerator PluginJoin(LobbyInfo lobby, string phase)
        {
            SetPhase(phase);
            yield return new WaitForSeconds(HarnessConstants.CLIENT_JOIN_STAGGER);
            for (int attempt = 1; attempt <= HarnessConstants.JOIN_MAX_ATTEMPTS; attempt++)
            {
                HarnessLog.Info($"Joining {lobby.Code} (attempt {attempt}/{HarnessConstants.JOIN_MAX_ATTEMPTS})");
                if (!Try("join the online game", () => JoinOnlineGame(lobby.GameId))) yield break;
                yield return WaitFor("joined lobby", IsInLobby, HarnessConstants.CONNECT_TIMEOUT, required: false);
                if (IsInLobby())
                {
                    yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
                    yield break;
                }
                Warn($"Join attempt {attempt} failed (last disconnect reason: {AmongUsClient.Instance.LastDisconnectReason})");
            }
            Fail($"Could not join {lobby.Code} after {HarnessConstants.JOIN_MAX_ATTEMPTS} attempts");
        }

        /// <summary>
        /// Opens the Good Loss menu and saves a screenshot in the output folder: the header shows the room
        /// code and the code of the account. Checked by eye, the IMGUI text cannot be read back.
        /// </summary>
        private static IEnumerator CapturePanel(string fileName)
        {
            if (!Try("open the Good Loss menu", () => GLModPlugin.MenuService.Open())) yield break;
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_PANEL_DELAY);
            Try("take the screenshot", () => ScreenCapture.CaptureScreenshot(Path.Combine(_context.OutputDir, fileName)));
            yield return new WaitForSeconds(HarnessConstants.PLUGIN_PANEL_DELAY);
            Try("close the Good Loss menu", () => GLModPlugin.MenuService.Close());
        }

        private static void SendChat(string text)
        {
            HarnessLog.Info("Sending: " + text);
            PlayerControl.LocalPlayer.RpcSendChat(text);
        }

        private static bool IsAtMainMenu()
        {
            return AmongUsClient.Instance != null
                && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.NotJoined
                && SceneManager.GetActiveScene().name == HarnessConstants.MAIN_MENU_SCENE;
        }

        private static string LocalName()
        {
            return PlayerControl.LocalPlayer?.Data?.PlayerName;
        }

        private static void Check(bool condition, string failure)
        {
            if (condition) return;
            _failures.Add($"[{_phase}] {failure}");
            HarnessLog.Error($"[{_phase}] {failure}");
        }

        private static void CheckNoFailure(string failure)
        {
            Check(failure == null, failure);
        }

        // Coordination between the instances: one file per signal, in the output folder of the run.

        private static string SignalPath(string name)
        {
            return Path.Combine(_context.OutputDir, string.Format(HarnessConstants.SIGNAL_FILE_FORMAT, name));
        }

        private static void Signal(string name, string content = "")
        {
            string path = SignalPath(name);
            File.WriteAllText(path + ".tmp", content ?? "");
            File.Move(path + ".tmp", path, true);
            HarnessLog.Info("Signal: " + name);
        }

        private static string ReadSignal(string name)
        {
            string path = SignalPath(name);
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static IEnumerator WaitSignal(string name)
        {
            yield return WaitFor("signal " + name, () => ReadSignal(name) != null, HarnessConstants.PLUGIN_SIGNAL_TIMEOUT);
        }
    }
}
