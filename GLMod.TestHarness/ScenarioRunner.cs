using GLMod.Class;
using GLMod.Enums;
using GLMod.GLEntities;
using Il2CppInterop.Runtime.InteropTypes;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using GLModPlugin = global::GLMod.GLMod;

namespace GLMod.TestHarness
{
    /// <summary>
    /// Drives one Among Us instance through the scripted scenario (see ScenarioPlan): an online lobby, then
    /// either the session (game 1: crewmates win by tasks, back to the lobby, game 2: impostor wins by vote)
    /// or the movement game (players walk around, the impostor kills twice).
    /// Every instance runs the same coroutine; each one only performs the actions it owns.
    /// Instances coordinate through files in the run output folder (lobby code, plans).
    /// </summary>
    public static class ScenarioRunner
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        private static HarnessContext _context;
        private static bool _started;
        private static bool _finished;
        private static string _phase = "boot";
        private static string _region;
        private static ScenarioPlan _plan;
        private static readonly List<string> _failures = new List<string>();
        private static readonly List<string> _warnings = new List<string>();
        private static readonly List<GameResult> _games = new List<GameResult>();

        // Current game, reset before each game starts
        private static bool _gameEnded;
        private static GLGame _endGame;
        private static bool _localIsImpostor;
        private static string _endGameJson;
        private static string _endReason;

        // Set by OpenConsole
        private static bool _consoleOpened;

        // Movement game, host: real positions per player name (RecordTruth)
        private static readonly Dictionary<string, List<TrackPoint>> _truth = new Dictionary<string, List<TrackPoint>>();

        // Host map setting before SelectMap changed it
        private static byte? _originalMapId;

        /// <summary>
        /// Content of lobby-N.json, written by the host once it is in the lobby of game N.
        /// </summary>
        public sealed class LobbyInfo
        {
            public string RunId { get; set; }
            public int GameId { get; set; }
            public string Code { get; set; }
        }

        public static void Init(HarnessContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Called once the main menu is loaded: starts the scenario (first call only).
        /// </summary>
        public static void OnMainMenu()
        {
            if (_context == null || _started) return;
            _started = true;
            CoroutineRunner.Run(Watchdog());
            CoroutineRunner.Run(RelaxNetworkTimeouts());
            CoroutineRunner.Run(Run());
        }

        /// <summary>
        /// Called after GLMod's own OnGameEnd postfix: snapshots the game GLMod is about to upload.
        /// </summary>
        public static void OnGameEnd(GameOverReason reason)
        {
            if (_context == null || _gameEnded) return;
            _gameEnded = true;
            _endReason = reason.ToString();
            _endGame = GLModPlugin.GameStateManager?.CurrentGame;
            _endGameJson = _endGame != null ? GLJson.Serialize(_endGame) : null;
            _localIsImpostor = _plan != null && PlayerControl.LocalPlayer != null && _plan.Impostor?.Id == PlayerControl.LocalPlayer.PlayerId;
            HarnessLog.Info($"Game ended: {reason} (GLMod step {GLModPlugin.GameStateManager?.Step})");
        }

        private static IEnumerator Run()
        {
            Application.runInBackground = true;
            HarnessLog.Info($"Scenario starting: run {_context.RunId}, {(_context.IsHost ? "host" : "client")}, API {GLModPlugin.api}");
            Try("cap the frame rate", LimitFrameRate);

            // Main menu
            SetPhase("menu");
            yield return new WaitForSeconds(HarnessConstants.MENU_SETTLE_DELAY);
            HarnessLog.Info($"GLMod login: {GLModPlugin.AuthService?.IsLoggedIn} ({GLModPlugin.AuthService?.GetAccountName()}), mod: {GLModPlugin.ConfigService?.ModName}");
            if (!Try($"select the server region '{_context.Region}'", SelectRegion)) yield break;
            if (!Try("check the server region", () => _region = CheckRegion())) yield break;
            HarnessLog.Info("Region: " + _region);

            SetPhase("connect");
            yield return ConnectToLobby();
            if (_finished) yield break;

            string previousGameId = null;
            int[] gameNumbers = _context.GameNumbers;
            for (int i = 0; i < gameNumbers.Length; i++)
            {
                yield return PlayGame(gameNumbers[i], previousGameId);
                if (_finished) yield break;

                previousGameId = _endGame?.id;

                if (i + 1 < gameNumbers.Length)
                {
                    yield return ReturnToLobby(gameNumbers[i + 1]);
                    if (_finished) yield break;
                }
            }

            SetPhase("done");
            Finish();
        }

        private static IEnumerator PlayGame(int gameNumber, string previousGameId)
        {
            string prefix = $"g{gameNumber}";
            _gameEnded = false;
            _endGame = null;
            _endGameJson = null;
            _endReason = null;
            _plan = null;

            // Lobby
            SetPhase(prefix + ":lobby");
            if (_context.IsHost)
            {
                yield return HostStartGame();
                if (_finished) yield break;
            }

            // Intro + GLMod registration of a new game
            SetPhase(prefix + ":intro");
            yield return WaitFor("game started with roles", () => IsGameStarted() && PlayerControl.LocalPlayer.Data.Role != null, HarnessConstants.GAME_START_TIMEOUT);
            if (_finished) yield break;
            HarnessLog.Info($"Game {gameNumber} started, my role: {PlayerControl.LocalPlayer.Data.Role.Role} ({PlayerControl.LocalPlayer.Data.Role.TeamType})");

            yield return WaitFor("GLMod registered a new game (step >= PlayersRecorded)", () => IsNewGLModGame(previousGameId), HarnessConstants.GLMOD_READY_TIMEOUT);
            if (_finished) yield break;
            HarnessLog.Info($"GLMod game id: {GLModPlugin.GameStateManager.CurrentGame.id}");

            // Plan
            SetPhase(prefix + ":plan");
            if (_context.IsHost)
            {
                if (!Try("build and publish the plan", () => BuildAndPublishPlan(gameNumber))) yield break;
            }
            else
            {
                yield return WaitFor("plan published by host", () => TryLoadPlan(gameNumber), HarnessConstants.PLAN_TIMEOUT);
                if (_finished) yield break;
            }
            HarnessLog.Info("Plan: " + _plan);

            yield return WaitFor("intro finished (CanMove)", () => PlayerControl.LocalPlayer.CanMove, HarnessConstants.CAN_MOVE_TIMEOUT, required: false);
            yield return new WaitForSeconds(HarnessConstants.POST_INTRO_DELAY);

            switch (gameNumber)
            {
                case ScenarioPlan.GAME_CREWMATES_WIN_BY_TASKS:
                    yield return PlayGameOne();
                    break;
                case ScenarioPlan.GAME_IMPOSTOR_WINS_BY_VOTE:
                    yield return PlayGameTwo();
                    break;
                default:
                    yield return PlayMovementGame();
                    break;
            }
            if (_finished) yield break;

            // End of game
            SetPhase(prefix + ":end");
            yield return WaitFor("game end", () => _gameEnded, HarnessConstants.GAME_END_TIMEOUT);
            if (_finished) yield break;

            if (_context.IsHost)
            {
                SetPhase(prefix + ":upload");
                yield return WaitFor("GLMod /game/end upload (step back to Initial)", () => GLModPlugin.GameStateManager.Step == GameStep.Initial, HarnessConstants.UPLOAD_TIMEOUT);
                if (_finished) yield break;
            }

            // The end screen records the game in GLMod's local history (menu end-of-game signal)
            SetPhase(prefix + ":history");
            yield return WaitForQuiet(IsGameInHistory, HarnessConstants.END_SCREEN_TIMEOUT);
            if (_finished) yield break;

            RecordGameResult(gameNumber, previousGameId);
        }

        private static bool IsGameInHistory()
        {
            return _endGame == null
                || string.IsNullOrEmpty(_endGame.id)
                || GLModPlugin.GameHistoryService?.Latest?.gameId == _endGame.id;
        }

        // ---------------------------------------------------------------- game 1: crewmates win by tasks

        private static IEnumerator PlayGameOne()
        {
            byte me = PlayerControl.LocalPlayer.PlayerId;

            // 1. Tasker plays one task
            SetPhase("g1:task");
            if (me == _plan.Tasker.Id)
            {
                yield return DoTasks(1, HarnessConstants.MINIGAME_VISIBLE_DELAY);
                if (_finished) yield break;
            }
            yield return WaitFor($"first task of {_plan.Tasker.Name}", () => CountCompletedTasks(_plan.Tasker.Id) >= 1, HarnessConstants.STEP_TIMEOUT);
            if (_finished) yield break;

            // 2. Impostor sabotages the lights, Caller fixes them
            SetPhase("g1:sabotage");
            if (me == _plan.Impostor.Id)
            {
                yield return WaitFor("sabotage cooldown over", () => GetSystem<SabotageSystemType>(SystemTypes.Sabotage).Timer <= 0f, HarnessConstants.SABOTAGE_COOLDOWN_TIMEOUT);
                if (_finished) yield break;
                HarnessLog.Info("Sabotaging the lights");
                if (!Try("sabotage the lights", () => ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, (byte)SystemTypes.Electrical))) yield break;
            }
            yield return WaitFor("lights sabotaged", () => GetSystem<SwitchSystem>(SystemTypes.Electrical).IsActive, HarnessConstants.STEP_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.SABOTAGE_SETTLE_DELAY);

            if (me == _plan.Caller.Id)
            {
                yield return FixLights();
                if (_finished) yield break;
            }
            yield return WaitFor("lights fixed", () => !GetSystem<SwitchSystem>(SystemTypes.Electrical).IsActive, HarnessConstants.STEP_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.SABOTAGE_SETTLE_DELAY);

            // 3. Caller presses the emergency button; tie between Tasker and skip
            SetPhase("g1:emergency");
            if (me == _plan.Caller.Id)
            {
                yield return CallEmergency();
                if (_finished) yield break;
            }
            yield return VoteInMeeting(waitForMeetingEnd: true);
            if (_finished) yield break;

            // 4. Impostor kills Victim
            SetPhase("g1:kill");
            yield return KillVictim();
            if (_finished) yield break;

            // 5. Every crewmate plays all its remaining tasks (Victim as a ghost)
            SetPhase("g1:tasks");
            if (me != _plan.Impostor.Id)
            {
                yield return DoTasks(HarnessConstants.MAX_TASKS_PER_PLAYER, HarnessConstants.MINIGAME_VISIBLE_DELAY_FAST);
            }
        }

        // ---------------------------------------------------------------- game 2: impostor wins by vote

        private static IEnumerator PlayGameTwo()
        {
            byte me = PlayerControl.LocalPlayer.PlayerId;

            SetPhase("g2:kill");
            yield return KillVictim();
            if (_finished) yield break;

            SetPhase("g2:report");
            if (me == _plan.Reporter.Id)
            {
                yield return ReportBody(_plan.Victim.Id);
                if (_finished) yield break;
            }

            yield return VoteInMeeting(waitForMeetingEnd: false);
        }

        // ---------------------------------------------------------------- game 3: movement

        /// <summary>
        /// Everyone walks from console to console; the impostor roams, hunts and kills the first
        /// victim, roams again for MOVEMENT_KILL_GAP seconds, then hunts and kills the second one.
        /// The host records where every player really is on each frame (truth) to measure the
        /// accuracy of the positions GLMod sends.
        /// </summary>
        private static IEnumerator PlayMovementGame()
        {
            byte me = PlayerControl.LocalPlayer.PlayerId;
            var random = new System.Random(Environment.TickCount + _context.Instance * 7919);

            if (_context.IsHost)
            {
                CoroutineRunner.Run(RecordTruth());
            }

            SetPhase("g3:grid");
            if (!Try("build the navigation grid", HarnessWalker.BuildGrid)) yield break;

            float start = Time.realtimeSinceStartup;
            SetPhase("g3:roam");
            if (me != _plan.Impostor.Id)
            {
                yield return HarnessWalker.Roam(random, () => _gameEnded || PlayerControl.LocalPlayer.Data.IsDead);
                yield break;
            }

            yield return HarnessWalker.Roam(random, () => _gameEnded || Time.realtimeSinceStartup - start > HarnessConstants.MOVEMENT_FIRST_KILL_DELAY);
            if (_finished || _gameEnded) yield break;

            SetPhase("g3:kill1");
            yield return Hunt(_plan.FirstVictim);
            if (_finished || _gameEnded) yield break;

            float firstKill = Time.realtimeSinceStartup;
            SetPhase("g3:wait");
            HarnessLog.Info($"Waiting {HarnessConstants.MOVEMENT_KILL_GAP}s before the second kill");
            yield return HarnessWalker.Roam(random, () => _gameEnded || Time.realtimeSinceStartup - firstKill > HarnessConstants.MOVEMENT_KILL_GAP);
            if (_finished || _gameEnded) yield break;

            SetPhase("g3:kill2");
            yield return Hunt(_plan.SecondVictim);
        }

        /// <summary>
        /// Walks after the victim and presses Kill once the kill button targets it and is ready.
        /// </summary>
        private static IEnumerator Hunt(ScenarioPlayer victim)
        {
            HarnessLog.Info($"Hunting {victim.Name}#{victim.Id}");
            float deadline = Time.realtimeSinceStartup + HarnessConstants.MOVEMENT_HUNT_TIMEOUT;
            while (!_finished && !_gameEnded && !IsDead(victim.Id))
            {
                float remaining = deadline - Time.realtimeSinceStartup;
                if (remaining <= 0f)
                {
                    Fail($"Could not kill {victim.Name} within {HarnessConstants.MOVEMENT_HUNT_TIMEOUT}s");
                    yield break;
                }

                if (SafeEvaluate(() => CanKill(victim.Id)))
                {
                    HarnessLog.Info($"Killing {victim.Name}");
                    if (!Try("press Kill", () => HudManager.Instance.KillButton.DoClick())) yield break;
                    yield return WaitForQuiet(() => _gameEnded || IsDead(victim.Id), HarnessConstants.MOVEMENT_KILL_CONFIRM_TIMEOUT);
                    continue;
                }

                yield return HarnessWalker.Walk(() => PositionOf(victim.Id), () => _gameEnded || IsDead(victim.Id) || CanKill(victim.Id),
                    remaining, HarnessConstants.MOVEMENT_HUNT_REPLAN_INTERVAL);
                yield return null;
            }

            if (IsDead(victim.Id))
            {
                HarnessLog.Info($"{victim.Name} killed");
            }
        }

        private static bool CanKill(byte victimId)
        {
            PlayerControl me = PlayerControl.LocalPlayer;
            KillButton button = HudManager.Instance?.KillButton;
            return me != null
                && me.CanMove
                && me.killTimer <= 0f
                && button != null
                && button.currentTarget != null
                && button.currentTarget.PlayerId == victimId;
        }

        private static System.Numerics.Vector2? PositionOf(byte playerId)
        {
            PlayerControl player = FindPlayer(playerId);
            return player != null ? HarnessWalker.ToNumerics(player.GetTruePosition()) : (System.Numerics.Vector2?)null;
        }

        private static bool IsDead(byte playerId)
        {
            return SafeEvaluate(() => FindPlayer(playerId)?.Data?.IsDead == true);
        }

        /// <summary>
        /// Host: position of every alive player on every frame, as GLMod reads it, until the game ends.
        /// </summary>
        private static IEnumerator RecordTruth()
        {
            _truth.Clear();
            while (!_finished && !_gameEnded)
            {
                try
                {
                    if (MeetingHud.Instance == null)
                    {
                        long now = ServerClock.NowMs();
                        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                        {
                            if (player?.Data == null || player.Data.IsDead) continue;

                            Vector3 position = player.MyPhysics.body.transform.position;
                            if (!_truth.TryGetValue(player.Data.PlayerName, out List<TrackPoint> track))
                            {
                                track = new List<TrackPoint>();
                                _truth[player.Data.PlayerName] = track;
                            }
                            track.Add(new TrackPoint(now, position.x, position.y));
                        }
                    }
                }
                catch (Exception e)
                {
                    HarnessLog.Warn("Could not record the real positions: " + e.Message);
                }
                yield return null;
            }
        }

        // ---------------------------------------------------------------- shared steps

        private static IEnumerator KillVictim()
        {
            if (_context.IsHost)
            {
                yield return new WaitForSeconds(HarnessConstants.KILL_DELAY);
                HarnessLog.Info($"{_plan.Impostor.Name} kills {_plan.Victim.Name}");
                if (!Try("kill", () => FindPlayer(_plan.Impostor.Id).RpcMurderPlayer(FindPlayer(_plan.Victim.Id), true))) yield break;
            }

            yield return WaitFor($"{_plan.Victim.Name} dead", () => FindPlayer(_plan.Victim.Id)?.Data?.IsDead == true, HarnessConstants.KILL_TIMEOUT);
        }

        private static IEnumerator VoteInMeeting(bool waitForMeetingEnd)
        {
            // Votes can all land between two polls: accept a meeting already showing its results,
            // or a game already over (dead players do not vote and may only see the end)
            yield return WaitFor("voting phase reached", () => _gameEnded || IsVotingOpen() || IsVotingOver(), HarnessConstants.VOTING_PHASE_TIMEOUT);
            if (_finished || _gameEnded) yield break;

            byte me = PlayerControl.LocalPlayer.PlayerId;
            byte? target = _plan.GetVoteTarget(me);
            if (target.HasValue && !PlayerControl.LocalPlayer.Data.IsDead && !IsVotingOpen())
            {
                Fail("Voting closed before this player could vote");
                yield break;
            }

            if (target.HasValue && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                byte suspect = target.Value == ScenarioPlan.SKIP_VOTE ? PlayerVoteArea.SkippedVote : target.Value;
                HarnessLog.Info(target.Value == ScenarioPlan.SKIP_VOTE ? "Voting skip" : $"Voting for player #{suspect}");
                if (!Try("vote", () => MeetingHud.Instance.CmdCastVote(me, suspect))) yield break;
            }

            // Meeting results wait for someone to press "Proceed"
            yield return WaitFor("Proceed button shown", () => _gameEnded || MeetingHud.Instance == null || IsProceedButtonShown(), HarnessConstants.PROCEED_TIMEOUT, required: false);
            if (IsProceedButtonShown())
            {
                yield return new WaitForSeconds(HarnessConstants.PROCEED_DELAY);
                HarnessLog.Info("Pressing Proceed");
                if (!Try("press Proceed", () => MeetingHud.Instance.HandleProceed())) yield break;
            }

            if (waitForMeetingEnd)
            {
                yield return WaitFor("meeting over", () => MeetingHud.Instance == null && ExileController.Instance == null && PlayerControl.LocalPlayer.CanMove,
                    HarnessConstants.MEETING_END_TIMEOUT);
                if (_finished) yield break;
                yield return new WaitForSeconds(HarnessConstants.POST_MEETING_DELAY);
            }
        }

        /// <summary>
        /// Plays up to maxTasks real tasks: for each step, go to the console, open the minigame,
        /// finish it through NormalPlayerTask.NextStep() (what minigames call on success).
        /// Stops quietly if the game ends meanwhile (the last task can end it).
        /// </summary>
        private static IEnumerator DoTasks(int maxTasks, float visibleDelay)
        {
            if (PlayerControl.LocalPlayer.Data.IsDead)
            {
                // Let the kill animation finish and the ghost spawn
                yield return new WaitForSeconds(HarnessConstants.GHOST_DELAY);
            }

            for (int played = 0; played < maxTasks && !_gameEnded; played++)
            {
                NormalPlayerTask task = PickEasiestTask();
                if (task == null) break;

                yield return PlayTask(task, visibleDelay);
                if (_finished || _gameEnded) yield break;
            }

            if (!_gameEnded)
            {
                HarnessLog.Info("No task left to play");
            }
        }

        private static IEnumerator PlayTask(NormalPlayerTask task, float visibleDelay)
        {
            HarnessLog.Info($"Playing task {task.TaskType} (id {task.Id}, {task.MaxStep} step(s)){(PlayerControl.LocalPlayer.Data.IsDead ? " as a ghost" : "")}");

            for (int attempt = 0; attempt < task.MaxStep + HarnessConstants.TASK_EXTRA_STEP_ATTEMPTS && !task.IsComplete && !_gameEnded; attempt++)
            {
                Console console = FindClosestConsole(task);
                if (console == null)
                {
                    Fail($"No console found for {task.TaskType} step {task.taskStep + 1}");
                    yield break;
                }

                Vector2 position = console.transform.position;
                HarnessLog.Info($"  step {task.taskStep + 1}/{task.MaxStep}: console #{console.ConsoleId} in {console.Room} at ({position.x:0.00}, {position.y:0.00})");

                yield return OpenConsole(position, () => console.Use(), () =>
                {
                    console.CanUse(PlayerControl.LocalPlayer.Data, out bool canUse, out bool _);
                    return canUse;
                });
                if (_finished || _gameEnded) yield break;
                if (!_consoleOpened)
                {
                    Fail($"Could not open the minigame of {task.TaskType} at console #{console.ConsoleId}");
                    yield break;
                }

                Minigame minigame = Minigame.Instance;
                yield return new WaitForSeconds(visibleDelay);
                if (_gameEnded) yield break;

                if (!Try("finish the minigame", () =>
                {
                    NormalPlayerTask minigameTask = minigame.MyNormTask ?? task;
                    minigameTask.NextStep();
                    minigame.Close();
                })) yield break;

                yield return WaitForQuiet(() => Minigame.Instance == null, HarnessConstants.MINIGAME_OPEN_TIMEOUT);
            }

            if (!task.IsComplete && !_gameEnded)
            {
                Fail($"Task {task.TaskType} not complete after playing its steps ({task.taskStep}/{task.MaxStep})");
            }
        }

        /// <summary>
        /// Goes to the lights panel, opens it and flips every switch that is in the wrong position
        /// (each flip is the RpcUpdateSystem call the switch minigame makes).
        /// </summary>
        private static IEnumerator FixLights()
        {
            PlayerTask lightsTask = null;
            yield return WaitFor("lights task assigned", () => (lightsTask = FindTask(TaskTypes.FixLights)) != null, HarnessConstants.STEP_TIMEOUT);
            if (_finished) yield break;

            Console console = FindClosestConsole(lightsTask);
            if (console == null)
            {
                Fail("No console found for FixLights");
                yield break;
            }

            Vector2 position = console.transform.position;
            HarnessLog.Info($"Fixing lights: console #{console.ConsoleId} in {console.Room} at ({position.x:0.00}, {position.y:0.00})");
            yield return OpenConsole(position, () => console.Use(), () =>
            {
                console.CanUse(PlayerControl.LocalPlayer.Data, out bool canUse, out bool _);
                return canUse;
            });
            if (_finished) yield break;
            if (!_consoleOpened)
            {
                Fail("Could not open the lights panel");
                yield break;
            }

            yield return new WaitForSeconds(HarnessConstants.MINIGAME_VISIBLE_DELAY);
            SwitchSystem switches = GetSystem<SwitchSystem>(SystemTypes.Electrical);
            int wrong = switches.ExpectedSwitches ^ switches.ActualSwitches;
            for (int i = 0; i < 8; i++)
            {
                if ((wrong & (1 << i)) == 0) continue;
                byte switchIndex = (byte)i;
                HarnessLog.Info($"  flipping switch {switchIndex}");
                if (!Try("flip a switch", () => ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, switchIndex))) yield break;
                yield return new WaitForSeconds(HarnessConstants.SWITCH_TOGGLE_DELAY);
            }

            Try("close the lights panel", () => Minigame.Instance?.Close());
        }

        private static IEnumerator CallEmergency()
        {
            yield return WaitFor("emergency cooldown over", () => ShipStatus.Instance.EmergencyCooldown <= 0f, HarnessConstants.EMERGENCY_COOLDOWN_TIMEOUT);
            if (_finished) yield break;

            SystemConsole button = ShipStatus.Instance.EmergencyButton;
            Vector2 position = button.transform.position;
            HarnessLog.Info($"Going to the emergency button at ({position.x:0.00}, {position.y:0.00})");
            yield return OpenConsole(position, () => button.Use(), null);
            if (_finished) yield break;

            EmergencyMinigame emergency = Minigame.Instance?.TryCast<EmergencyMinigame>();
            if (!_consoleOpened || emergency == null)
            {
                Fail("Could not open the emergency button");
                yield break;
            }

            yield return new WaitForSeconds(HarnessConstants.MINIGAME_VISIBLE_DELAY);
            HarnessLog.Info("Pressing the emergency button");
            if (!Try("call the meeting", () => emergency.CallMeeting())) yield break;
            yield return WaitFor("emergency meeting started", () => MeetingHud.Instance != null, HarnessConstants.MEETING_START_TIMEOUT);
        }

        /// <summary>
        /// Walks to the body and presses Report (PlayerControl.ReportClosest, the report button action).
        /// </summary>
        private static IEnumerator ReportBody(byte victimId)
        {
            DeadBody body = null;
            yield return WaitFor("dead body spawned", () => (body = FindBody(victimId)) != null, HarnessConstants.BODY_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.REPORT_DELAY);

            Vector2 position = body.transform.position;
            HarnessLog.Info($"Reporting body of #{victimId} at ({position.x:0.00}, {position.y:0.00})");
            foreach (Vector2 offset in HarnessConstants.CONSOLE_APPROACH_OFFSETS)
            {
                Vector2 target = position + offset;
                if (!Try("move to the body", () => PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target))) yield break;
                yield return new WaitForSeconds(HarnessConstants.MOVE_SETTLE_DELAY);
                if (!Try("press Report", () => PlayerControl.LocalPlayer.ReportClosest())) yield break;
                yield return WaitForQuiet(() => MeetingHud.Instance != null, HarnessConstants.MINIGAME_OPEN_TIMEOUT);
                if (MeetingHud.Instance != null) yield break;
            }

            Fail("Report did not start a meeting");
        }

        /// <summary>
        /// Moves next to a console (trying small offsets) and uses it until a minigame opens.
        /// Sets _consoleOpened.
        /// </summary>
        private static IEnumerator OpenConsole(Vector2 position, Action use, Func<bool> canUse)
        {
            _consoleOpened = false;
            foreach (Vector2 offset in HarnessConstants.CONSOLE_APPROACH_OFFSETS)
            {
                Vector2 target = position + offset;
                if (!Try("move to the console", () => PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target))) yield break;
                yield return new WaitForSeconds(HarnessConstants.MOVE_SETTLE_DELAY);
                if (_gameEnded) yield break;

                bool usable;
                try
                {
                    usable = canUse == null || canUse();
                }
                catch (Exception)
                {
                    usable = false;
                }
                if (!usable) continue;

                if (!Try("use the console", use)) yield break;
                yield return WaitForQuiet(() => Minigame.Instance != null, HarnessConstants.MINIGAME_OPEN_TIMEOUT);
                if (Minigame.Instance != null)
                {
                    HarnessLog.Info($"  {Minigame.Instance.GetIl2CppType().Name} opened");
                    _consoleOpened = true;
                    yield break;
                }
            }
        }

        // ---------------------------------------------------------------- network / lobby

        /// <summary>
        /// Selects the test region for this session only: the choice is not saved, so the instances never
        /// write the shared regionInfo.json at the same time and the player's own choice is kept.
        /// </summary>
        private static void SelectRegion()
        {
            ServerManager servers = DestroyableSingleton<ServerManager>.Instance;
            if (servers.CurrentRegion?.Name == _context.Region) return;

            var names = new List<string>();
            foreach (IRegionInfo region in servers.AvailableRegions)
            {
                if (region == null) continue;
                if (region.Name == _context.Region)
                {
                    servers.CurrentRegion = region;
                    servers.ReselectServer();
                    HarnessLog.Info($"Region {region.Name} selected");
                    return;
                }
                names.Add(region.Name);
            }
            throw new InvalidOperationException($"region not found (available: {string.Join(", ", names)})");
        }

        /// <summary>
        /// Refuses official Innersloth servers: the harness teleports players and runs 4 clients on one account.
        /// </summary>
        private static string CheckRegion()
        {
            IRegionInfo region = DestroyableSingleton<ServerManager>.Instance.CurrentRegion;
            string description = $"{region.Name} ({region.PingServer})";
            string haystack = (region.Name + " " + region.PingServer).ToLowerInvariant();
            foreach (string marker in HarnessConstants.OFFICIAL_SERVER_MARKERS)
            {
                if (haystack.Contains(marker))
                {
                    throw new InvalidOperationException($"refusing to run on official server {description}; select a custom region");
                }
            }
            return description;
        }

        private static IEnumerator ConnectToLobby()
        {
            if (_context.IsHost)
            {
                HarnessLog.Info("Creating online game");
                if (!Try("create the online game", HostOnlineGame)) yield break;
                yield return WaitFor("host in lobby", IsInLobby, HarnessConstants.CONNECT_TIMEOUT);
                if (_finished) yield break;
                if (!Try("publish the lobby code", () => WriteLobbyFile(1))) yield break;
                yield break;
            }

            LobbyInfo lobby = null;
            yield return WaitFor("lobby code published by host", () => (lobby = TryLoadLobby(1)) != null, HarnessConstants.LOBBY_FILE_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(_context.Instance * HarnessConstants.CLIENT_JOIN_STAGGER);

            for (int attempt = 1; attempt <= HarnessConstants.JOIN_MAX_ATTEMPTS; attempt++)
            {
                HarnessLog.Info($"Joining {lobby.Code} (attempt {attempt}/{HarnessConstants.JOIN_MAX_ATTEMPTS})");
                if (!Try("join the online game", () => JoinOnlineGame(lobby.GameId))) yield break;

                yield return WaitFor("joined lobby", IsInLobby, HarnessConstants.CONNECT_TIMEOUT, required: false);
                if (IsInLobby()) yield break;

                Warn($"Join attempt {attempt} failed (last disconnect reason: {AmongUsClient.Instance.LastDisconnectReason})");
            }

            Fail($"Could not join {lobby.Code} after {HarnessConstants.JOIN_MAX_ATTEMPTS} attempts");
        }

        private static void HostOnlineGame()
        {
            AmongUsClient client = AmongUsClient.Instance;
            client.NetworkMode = NetworkModes.OnlineGame;
            client.MainMenuScene = HarnessConstants.MAIN_MENU_SCENE;
            client.OnlineScene = HarnessConstants.ONLINE_SCENE;
            client.StartCoroutine(client.CoCreateOnlineGame());
        }

        private static void JoinOnlineGame(int gameId)
        {
            AmongUsClient client = AmongUsClient.Instance;
            client.NetworkMode = NetworkModes.OnlineGame;
            client.MainMenuScene = HarnessConstants.MAIN_MENU_SCENE;
            client.OnlineScene = HarnessConstants.ONLINE_SCENE;
            client.StartCoroutine(client.CoJoinOnlineGameFromCode(gameId, true));
        }

        /// <summary>
        /// From the end screen, everyone presses "Play Again"; the host goes first and publishes
        /// lobby-N.json once it is back in the lobby.
        /// </summary>
        private static IEnumerator ReturnToLobby(int nextGame)
        {
            SetPhase($"g{nextGame - 1}:rejoin");
            EndGameNavigation navigation = null;
            yield return WaitFor("end screen", () => (navigation = UnityEngine.Object.FindObjectOfType<EndGameNavigation>()) != null, HarnessConstants.END_SCREEN_TIMEOUT);
            if (_finished) yield break;
            yield return new WaitForSeconds(HarnessConstants.END_SCREEN_DELAY);

            if (!_context.IsHost)
            {
                yield return WaitFor("host back in lobby", () => TryLoadLobby(nextGame) != null, HarnessConstants.REJOIN_TIMEOUT);
                if (_finished) yield break;
                yield return new WaitForSeconds(_context.Instance * HarnessConstants.CLIENT_JOIN_STAGGER);
            }

            HarnessLog.Info("Pressing Play Again");
            if (!Try("press Play Again", () => navigation.NextGame())) yield break;
            yield return WaitFor("back in lobby", IsInLobby, HarnessConstants.REJOIN_TIMEOUT);
            if (_finished) yield break;

            if (_context.IsHost)
            {
                Try("publish the lobby code", () => WriteLobbyFile(nextGame));
            }
        }

        private static IEnumerator HostStartGame()
        {
            yield return WaitFor($"{HarnessConstants.PLAYER_COUNT} players in lobby", IsLobbyFull, HarnessConstants.LOBBY_FULL_TIMEOUT);
            if (_finished) yield break;

            yield return new WaitForSeconds(HarnessConstants.LOBBY_SETTLE_DELAY);
            HarnessLog.Info("Lobby full: " + string.Join(", ", PlayerNames()));

            if (!Try("select the map", SelectMap)) yield break;

            if (!Try("begin the game", () => GameStartManager.Instance.BeginGame())) yield break;

            yield return WaitFor("countdown started", () => IsGameStarted() || GameStartManager.Instance.startState != GameStartManager.StartingStates.NotStarting,
                HarnessConstants.START_COUNTDOWN_TIMEOUT, required: false);

            if (!IsGameStarted() && GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.NotStarting)
            {
                Warn("BeginGame did not start the countdown; forcing AmongUsClient.StartGame()");
                Try("force start", () => AmongUsClient.Instance.StartGame());
            }
        }

        /// <summary>
        /// Host only: switches the lobby to the requested map (the site only draws the map
        /// visualization for maps it has an image for, e.g. Polus) and syncs the options.
        /// The previous map is restored at the end of the session (RestoreMap).
        /// </summary>
        private static void SelectMap()
        {
            if (!_context.MapId.HasValue) return;

            AmongUs.GameOptions.IGameOptions options = GameOptionsManager.Instance.CurrentGameOptions;
            byte current = options.GetByte(AmongUs.GameOptions.ByteOptionNames.MapId);
            if (current == _context.MapId.Value) return;

            _originalMapId ??= current;
            options.SetByte(AmongUs.GameOptions.ByteOptionNames.MapId, _context.MapId.Value);
            GameManager.Instance.LogicOptions.SyncOptions();
            HarnessLog.Info($"Map {current} -> {_context.MapId.Value}");
        }

        private static void RestoreMap()
        {
            if (!_originalMapId.HasValue) return;

            GameOptionsManager.Instance.CurrentGameOptions.SetByte(AmongUs.GameOptions.ByteOptionNames.MapId, _originalMapId.Value);
            GameOptionsManager.Instance.SaveNormalHostOptions();
            HarnessLog.Info($"Host map setting restored to {_originalMapId.Value}");
        }

        /// <summary>
        /// Keeps raising the Hazel timeouts of the client connection.
        /// </summary>
        private static IEnumerator RelaxNetworkTimeouts()
        {
            while (!_finished)
            {
                try
                {
                    Hazel.Udp.UdpConnection connection = AmongUsClient.Instance?.connection;
                    if (connection != null)
                    {
                        connection.DisconnectTimeoutMs = HarnessConstants.NETWORK_DISCONNECT_TIMEOUT_MS;
                        connection._MissingPingsUntilDisconnect_k__BackingField = HarnessConstants.NETWORK_MISSING_PINGS_UNTIL_DISCONNECT;
                    }
                }
                catch (Exception e)
                {
                    HarnessLog.Warn("Could not relax network timeouts: " + e.Message);
                }

                yield return new WaitForSeconds(HarnessConstants.NETWORK_RELAX_INTERVAL);
            }
        }

        /// <summary>
        /// Clients must load the map within a hard-coded 10s (AmongUsClient.MAX_CLIENT_WAIT_SECONDS,
        /// a native constant: writing it crashes the game). Capping the frame rate of every instance
        /// frees enough CPU for 4 instances to load in time, especially with software rendering (RDP).
        /// </summary>
        private static void LimitFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = HarnessConstants.TARGET_FRAME_RATE;
            // Async loading is time-sliced per frame: give it more of each (capped) frame
            Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.High;
            HarnessLog.Info($"Frame rate capped to {HarnessConstants.TARGET_FRAME_RATE} fps, background loading priority High");
        }

        // ---------------------------------------------------------------- files

        private static void WriteLobbyFile(int gameNumber)
        {
            int gameId = AmongUsClient.Instance.GameId;
            var lobby = new LobbyInfo { RunId = _context.RunId, GameId = gameId, Code = GameCode.IntToGameName(gameId) };
            WriteJsonAtomically(_context.LobbyPath(gameNumber), lobby);
            HarnessLog.Info($"Lobby {lobby.Code} published for game {gameNumber}");
        }

        private static LobbyInfo TryLoadLobby(int gameNumber)
        {
            LobbyInfo lobby = TryReadJson<LobbyInfo>(_context.LobbyPath(gameNumber));
            return lobby?.RunId == _context.RunId ? lobby : null;
        }

        private static void BuildAndPublishPlan(int gameNumber)
        {
            var players = new List<ScenarioPlayer>();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                bool isImpostor = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
                players.Add(new ScenarioPlayer(player.PlayerId, player.Data.PlayerName, isImpostor));
            }

            _plan = ScenarioPlan.Build(_context.RunId, gameNumber, players);
            WriteJsonAtomically(_context.PlanPath(gameNumber), _plan);
        }

        private static bool TryLoadPlan(int gameNumber)
        {
            ScenarioPlan plan = TryReadJson<ScenarioPlan>(_context.PlanPath(gameNumber));
            if (plan?.RunId != _context.RunId || plan.GameNumber != gameNumber) return false;
            _plan = plan;
            return true;
        }

        private static void WriteJsonAtomically<T>(string path, T value)
        {
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(tempPath, path, true);
        }

        private static T TryReadJson<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
            }
            catch (IOException)
            {
                // Being written: retry on next poll
                return null;
            }
        }

        // ---------------------------------------------------------------- result

        private static void RecordGameResult(int gameNumber, string previousGameId)
        {
            List<string> checks;
            if (_context.IsHost)
            {
                checks = _plan != null ? ScenarioVerifier.VerifyHostGame(_endGame, _plan) : new List<string> { "No plan to verify against" };
            }
            else
            {
                checks = ScenarioVerifier.VerifyClientGame(_endGame);
            }
            checks.AddRange(ScenarioVerifier.VerifyDistinctGames(previousGameId, _endGame?.id));
            checks.AddRange(ScenarioVerifier.VerifyHistoryEntry(
                GLModPlugin.GameHistoryService?.Latest, _endGame, gameNumber, _localIsImpostor, GLModPlugin.api));

            List<PlayerMovementStats> movement = null;
            if (_context.IsHost && gameNumber == ScenarioPlan.GAME_MOVEMENT && _plan != null)
            {
                movement = new List<PlayerMovementStats>();
                checks.AddRange(ScenarioVerifier.VerifyPositions(_endGame, _plan, _truth, movement));
                foreach (PlayerMovementStats line in movement)
                {
                    HarnessLog.Info("Positions " + line);
                }
            }

            var result = new GameResult
            {
                Number = gameNumber,
                Plan = _plan,
                EndReason = _endReason,
                GameId = _endGame?.id,
                GameStep = GLModPlugin.GameStateManager?.Step.ToString(),
                Failures = checks,
                GameJson = _endGameJson,
                Movement = movement
            };
            _games.Add(result);

            foreach (string failure in checks)
            {
                _failures.Add($"[game {gameNumber}] {failure}");
                HarnessLog.Error($"[game {gameNumber}] {failure}");
            }
            HarnessLog.Info($"Game {gameNumber} ({_endReason}): {(checks.Count == 0 ? "OK" : checks.Count + " failure(s)")}");
        }

        private static void Finish()
        {
            if (_finished) return;
            _finished = true;

            if (_context.IsHost)
            {
                try
                {
                    RestoreMap();
                }
                catch (Exception e)
                {
                    HarnessLog.Warn("Could not restore the host map setting: " + e.Message);
                }
            }

            var result = new HarnessResult
            {
                RunId = _context.RunId,
                Instance = _context.Instance,
                IsHost = _context.IsHost,
                Passed = _failures.Count == 0 && _games.Count == _context.GameNumbers.Length,
                Phase = _phase,
                LocalPlayer = SafeLocalName(),
                Region = _region,
                ApiEndpoint = GLModPlugin.api,
                Failures = _failures,
                Warnings = _warnings,
                Games = _games
            };

            try
            {
                File.WriteAllText(_context.ResultPath, JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception e)
            {
                HarnessLog.Error("Could not write result file: " + e.Message);
            }

            HarnessLog.Info(result.Passed ? "RESULT: PASSED" : $"RESULT: FAILED ({_failures.Count} failure(s), {_games.Count}/{_context.GameNumbers.Length} game(s) played)");
            CoroutineRunner.Run(QuitLater());
        }

        private static IEnumerator QuitLater()
        {
            yield return new WaitForSeconds(HarnessConstants.QUIT_DELAY);
            Application.Quit();
        }

        private static IEnumerator Watchdog()
        {
            yield return new WaitForSeconds(HarnessConstants.GLOBAL_TIMEOUT);
            if (!_finished)
            {
                Fail($"Global timeout ({HarnessConstants.GLOBAL_TIMEOUT}s) in phase '{_phase}'");
            }
        }

        // ---------------------------------------------------------------- helpers

        private static void SetPhase(string phase)
        {
            if (_phase == phase) return;
            _phase = phase;
            HarnessLog.Info("Phase: " + phase);
        }

        private static void Warn(string message)
        {
            _warnings.Add(message);
            HarnessLog.Warn(message);
        }

        private static void Fail(string reason)
        {
            if (_finished) return;
            string failure = $"[{_phase}] {reason}";
            _failures.Add(failure);
            HarnessLog.Error(failure);
            Finish();
        }

        private static bool Try(string what, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Fail($"Could not {what}: {e.GetType().Name}: {e.Message}");
                return false;
            }
        }

        private static IEnumerator WaitFor(string what, Func<bool> condition, float timeoutSeconds, bool required = true)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!_finished)
            {
                if (SafeEvaluate(condition)) yield break;

                if (Time.realtimeSinceStartup > deadline)
                {
                    if (required)
                    {
                        Fail($"Timeout ({timeoutSeconds}s) waiting for: {what}");
                    }
                    else
                    {
                        HarnessLog.Warn($"Timeout ({timeoutSeconds}s) waiting for: {what} (continuing)");
                    }
                    yield break;
                }

                yield return new WaitForSeconds(HarnessConstants.POLL_INTERVAL);
            }
        }

        /// <summary>
        /// Like WaitFor, but never fails nor logs: callers check the condition themselves.
        /// </summary>
        private static IEnumerator WaitForQuiet(Func<bool> condition, float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!_finished && !SafeEvaluate(condition) && Time.realtimeSinceStartup <= deadline)
            {
                yield return new WaitForSeconds(HarnessConstants.POLL_INTERVAL);
            }
        }

        private static bool SafeEvaluate(Func<bool> condition)
        {
            try
            {
                return condition();
            }
            catch (Exception)
            {
                // Game objects may not exist yet: treat as "not yet"
                return false;
            }
        }

        private static bool IsGameStarted()
        {
            return AmongUsClient.Instance != null
                && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started
                && ShipStatus.Instance != null;
        }

        private static bool IsInLobby()
        {
            return AmongUsClient.Instance != null
                && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined
                && GameStartManager.InstanceExists
                && PlayerControl.LocalPlayer != null
                && PlayerControl.LocalPlayer.Data != null;
        }

        private static bool IsLobbyFull()
        {
            if (!IsInLobby()) return false;
            if (PlayerControl.AllPlayerControls.Count != HarnessConstants.PLAYER_COUNT) return false;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || string.IsNullOrEmpty(player.Data.PlayerName)) return false;
            }
            return true;
        }

        private static bool IsNewGLModGame(string previousGameId)
        {
            var manager = GLModPlugin.GameStateManager;
            return manager != null
                && (int)manager.Step >= (int)GameStep.PlayersRecorded
                && !string.IsNullOrEmpty(manager.CurrentGame?.id)
                && manager.CurrentGame.id != previousGameId;
        }

        private static bool IsVotingOpen()
        {
            MeetingHud meeting = MeetingHud.Instance;
            return meeting != null
                && (meeting.state == MeetingHud.VoteStates.NotVoted || meeting.state == MeetingHud.VoteStates.Voted);
        }

        private static bool IsVotingOver()
        {
            MeetingHud meeting = MeetingHud.Instance;
            return meeting != null
                && (meeting.state == MeetingHud.VoteStates.Results || meeting.state == MeetingHud.VoteStates.Proceeding);
        }

        private static bool IsProceedButtonShown()
        {
            MeetingHud meeting = MeetingHud.Instance;
            return meeting != null
                && meeting.ProceedButton != null
                && meeting.ProceedButton.gameObject.activeInHierarchy;
        }

        private static T GetSystem<T>(SystemTypes type) where T : Il2CppObjectBase
        {
            return ShipStatus.Instance.Systems[type].TryCast<T>();
        }

        private static int CountCompletedTasks(byte playerId)
        {
            PlayerControl player = FindPlayer(playerId);
            if (player?.Data?.Tasks == null) return 0;

            int completed = 0;
            foreach (NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
            {
                if (task.Complete) completed++;
            }
            return completed;
        }

        /// <summary>
        /// Prefers single-step tasks (one console, one minigame).
        /// </summary>
        private static NormalPlayerTask PickEasiestTask()
        {
            NormalPlayerTask best = null;
            foreach (PlayerTask playerTask in PlayerControl.LocalPlayer.myTasks)
            {
                if (playerTask == null || playerTask.IsComplete) continue;
                NormalPlayerTask task = playerTask.TryCast<NormalPlayerTask>();
                if (task == null) continue;
                if (best == null || task.MaxStep < best.MaxStep) best = task;
            }
            return best;
        }

        private static PlayerTask FindTask(TaskTypes type)
        {
            foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
            {
                if (task != null && task.TaskType == type) return task;
            }
            return null;
        }

        private static Console FindClosestConsole(PlayerTask task)
        {
            Vector2 me = PlayerControl.LocalPlayer.transform.position;
            Console closest = null;
            float closestDistance = float.MaxValue;
            foreach (Console console in task.FindConsoles())
            {
                if (console == null) continue;
                float distance = Vector2.Distance(me, console.transform.position);
                if (distance < closestDistance)
                {
                    closest = console;
                    closestDistance = distance;
                }
            }
            return closest;
        }

        private static DeadBody FindBody(byte playerId)
        {
            foreach (DeadBody body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
            {
                if (body != null && body.ParentId == playerId) return body;
            }
            return null;
        }

        private static PlayerControl FindPlayer(byte playerId)
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player != null && player.PlayerId == playerId) return player;
            }
            return null;
        }

        private static List<string> PlayerNames()
        {
            var names = new List<string>();
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                names.Add($"{player.Data?.PlayerName}#{player.PlayerId}");
            }
            return names;
        }

        private static string SafeLocalName()
        {
            try
            {
                return PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.Data?.PlayerName : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
