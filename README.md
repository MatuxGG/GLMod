# GLMod

GLMod is a mod to collect data for the website [Good Loss](https://goodloss.fr) inside the popular game Among Us.
It's mainly provided through [Mod Manager](https://goodloss.fr/github)

## Downloads



## How to customize GLMod for your mod ?

### Install GLMod

Download GLMod.dll for your game version and add GLMod.dll to BepInEx/plugins.
In files where you use GLMod, use:
```
using GLMod;
```

### Disable services

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

### Overwrite StartGame

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

### Overwrite EndGame

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

## What does MatuxMod collect

MatuxMod does collect the following data:

### Room data

- State : Started / Finished
- Code (disabled)
- Map
- Start date
- Duration
- Mod used
- Amount of players

### Player data

- Name
- Goodloss account (if connected)
- Role
- Team : Crewmate / Impostor / Neutral / Others
- Tasks completed alive
- Tasks completed dead
- Total tasks to complete

### Actions data

- Source
- Target
- Action

Default available actions : kills, exiles, reports, emergencies, votes
