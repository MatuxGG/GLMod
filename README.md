# GLMod

GLMod is a mod to collect data for the website [Good Loss](https://goodloss.fr) inside the popular game Among Us.

# Downloads

| Among Us - Version | Link |
|--------------------|-----------------|
| 2024.06.18s        | [Download](https://github.com/MatuxGG/GLMod/releases/download/4.0.0/GLMod.zip)

# Installation

## Combining GLMod with another mod
GLMod is a mod that can be combined with any other mod. It is designed to be as simple as possible to use and to be as flexible as possible. It is also designed to be as lightweight as possible to avoid any performance issues.
If you want to combine GLMod with another mod, you can do it by following the instructions below.

1. Download and install the mod you want following its instructions.
2. Download the dll file from the releases for your game version (see above).
3. In the folder of the mod, go to BepInEx/plugins and add the GLMod.dll file.
4. In the folder of the mod again, go to BepInEx/config and add a file called MODNAME.mm where MODNAME is the name of the other mod used.

Note that only vanilla actions and roles will be recorded. If you want to record custom actions and roles, you will have to modify the other mod to use GLMod functions.
If you're a mod developper, see the "Integration with other mods" section below.

## Installation for Vanilla on Windows & Steam
1. Download the zip file from the releases for your game version (see above).
2. Find the folder of your game. In Steam, you can right click on the game in your library, a menu will appear. Then, click on Properties > local data > browse.
3. Go to the parent folder named common and make a copy of your Among Us game folder. Then, rename it as you want (for example, "Among Us - GLMod") and move it wherever you want on same drive.
4. Now unzip the files from the .zip into the folder you just copied.
5. Run the game by starting Among Us.exe from this folder (the first launch might take a while).

Not working? You might want to install the dependency [vc_redist](https://aka.ms/vs/16/release/vc_redist.x86.exe)

## Installation for Vanilla on Windows & Epic
1. Download the zip file from the releases for your game version (see above).
2. Find the folder of your game. Should be stored in "Epic/AmongUs" (wherever you installed Epic on your PC)
3. Now unzip the files from the .zip into the original Epic Among Us game folder.
4. Run the game by starting the game in your Epic Games launcher (the first launch might take a while).

Not working? You might want to install the dependency [vc_redist](https://aka.ms/vs/16/release/vc_redist.x86.exe)

## Installation for Vanilla on Linux
1. Download the zip file from the releases for your game version (see above).
2. Install Among Us via Steam
3. Extract the zip file into "~/.steam/steam/steamapps/common/Among Us".
3. Enable winhttp.dll via the proton winecfg (https://docs.bepinex.dev/articles/advanced/proton_wine.html)
4. Launch the game via Steam

# Contributing

If you want to contribute to the project, you need to install this repository on your computer. Here is how to do it:
1. Clone the repository.
2. In Among Us folder, make a copy of the Vanilla Among Us folder content.
3. Install BepInEx in the copied folder by downloading the zip file from the releases for your game version (see above).
4. Open the project in Visual Studio.
5. You can start contributing and testing your changes.

# Integration with other mods

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

## What does MatuxMod collect

MatuxMod does collect the following data:

### <ins>Room data</ins>

- State : Started / Finished
- Code (disabled)
- Map
- Start date
- Duration
- Mod used
- Amount of players

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

Default available actions : kills, exiles, reports, emergencies, votes

# License

This software is distributed under the GNU GPLv3 License.
