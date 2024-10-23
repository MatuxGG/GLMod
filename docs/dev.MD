# Integrate GLMod to you own mod

Download GLMod.dll for your game version and add GLMod.dll to BepInEx/plugins.
In files where you use GLMod, use:

```
using GLMod;
```

You also need to define the name of your mod in your Load() function:

```
GLMod.setModName("YOUR_MOD_NAME");
```

Everything is set up and everything will be recorded. The game will be available on players' match history on Good Loss.

Note that GLMod uses RPC 240.

## Customize GLMod

### <ins>Disable services</ins>

GLMod comes with a lot of default services enabled. You have to disable each one that you want to overwrite by add this call in your Load() function :

```
GLMod.disableService("SERVICE_TO_DISABLE");
```

If you have custom roles, you should disable this default services:

- StartGame: Manage Start Game.
- EndGame: Manage End Game.

For information, these services also exist and can be disabled. However, it's recommended to not disable them.

- Tasks: Manage tasks collect. Prefer changing manually tasks from a GLPlayer Object directly if you need to.
- TasksMax: Manage total tasks collect. Prefer changing manually tasks from a GLPlayer Object directly if you need to.
- Exiled: Manage exiles collect.
- Kills: Manage kills collect. Special kills are already handled without any action.
- BodyReported: Manage body reports collect.
- Emergencies: Manage emergencies collect.
- Turns: Manage turns collect (meeting & turns).
- Votes: Manage votes collect. The vote count is not stored, so mayor roles should already work without any action.
- Roles: Manage vanilla roles actions collect. See actions to see which roles.

### <ins>Overwrite StartGame</ins>

In your mod, you should start with the declaration of a new game on each client:

```
GLMod.StartGame(GAME_CODE, GAME_MAP, false);
```

Replace GAME_CODE and GAME map with the game code and map.

For example:

```
GLMod.StartGame("ABCDEF", "Polus", false);
```

Then, for each role you give to players, you have to use this function on each client:

```
GLMod.AddPlayer(PLAYER_NAME, PLAYER_ROLE, PLAYER_TEAM);
```

Note that for non crewmate/impostor roles (neutral, hybrid, ... roles), the team should be the same as the role.

For example:

```
GLMod.AddPlayer("Matux1", "Sheriff", "Crewmate");
GLMod.AddPlayer("Sean", "Guesser", "Impostor");
GLMod.AddPlayer("Paul", "Jester", "Jester");
```

For each client, when all roles are set on itself, you should validate the start game process by calling these functions:

```
GLMod.SendGame();
GLMod.AddMyPlayer();
```

Then, the start game is correctly overwritten ;)

### <ins>Add actions</ins>

Basically, in the game, there are a few actions recorded (kills, exiles, emergencies, ...).
But you can define custom ones for your roles with this function:
```
GLMod.currentGame.addAction(SOURCE_PLAYER_NAME, TARGET_PLAYER_NAME, CUSTOM_ACTION);
```

For example, if Sean (Sheriff) kills Paul (Impostor), you can add:

```
GLMod.currentGame.addAction("Sean", "Paul", "killed as Sheriff");
```

In this example, the next sentence will be shown in Good Loss history : "Send killed as Sheriff Paul".

You can also let the source or the target player empty.

For example, if a role can kill itself, you can have:

```
GLMod.currentGame.addAction("Matux", "", "killed itself");
```

And in history: "Matux killed itself".

### <ins>Overwrite EndGame</ins>

In your mod, you should end a game by declaring an end game on each client.

You need to declare each team that won. Each player with this team will be added to winners.

```
List<string> WinList = new List<string>();
WinList.Add(TEAM_1);
WinList.Add(TEAM_2);
...
GLMod.SetWinnerTeams(WinList);
```

If a team is called diffently that the team role given to its members, you need to also add each players that won like this:

```
GLMod.AddWinnerPlayer(PLAYER_NAME);
```

After all of these, you need to validate the end game with this function:

```
GLMod.EndGame();
```

Here is an example of a complete endgame:

```
List<string> WinList = new List<string>();
WinList.Add("Crewmate");
WinList.Add("Love");
GLMod.SetWinnerTeams(WinList);
// Love players have the role "Lover"
GLMod.AddWinnerPlayer("Sean"); // Lover 1
GLMod.AddWinnerPlayer("Paul"); // Lover 2
GLMod.EndGame();
```

Note that default teams are "Crewmate" and "Impostor". Also, prefer starting teams and roles with a capital letter.

### <ins>Check if it works</ins>

To verify that your implementation of GLMod is correct, you can complete a game with GLMod enabled.
Take a look at the file BepInEx/config/glmod.cfg.

After completing a game, all config entries in "Validation" section (aka "stepConf" and "stepRpc") should be equal to "YES".
If it does, all good :)
If not, please open an issue for this repository and explain how you configuration looks like !

## What does GLMod collect

GLMod does collect the following data:

### <ins>Room data</ins>

- State : Started / Finished
- Code (disabled)
- Map
- Start date
- Duration
- Mod used
- Amount of players
- Mod (if any)

### <ins>Player data</ins>

- Name
- Goodloss account (if connected)
- Role
- Team : Crewmate / Impostor / Neutral / Others
- Tasks completed alive
- Tasks completed dead
- Total tasks to complete

### <ins>Actions data</ins>

- Source
- Target
- Action

Default available actions : kills, exiles, reports, emergencies, votes, shapeshifts, unshapeshifts, tracks, untracks, shields.
