<p align="center">
  <a href="https://github.com/MatuxGG/GLMod">
    <img src="https://img.shields.io/badge/GLMod-v5.9.0-blue" alt="GLMod Version">
  </a>
  <a href="https://goodloss.fr/discord">
    <img src="https://img.shields.io/badge/Good%20Loss%20Discord%20Server-Join-7289DA?logo=discord&logoColor=white" alt="Good Loss Discord Server">
  </a>
  <a href="https://dotnet.microsoft.com/download/dotnet/6.0">
    <img src="https://img.shields.io/badge/.NET-6.0-blueviolet" alt=".NET Version">
  </a>
  <a href="https://github.com/MatuxGG/MedBot/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/MatuxGG/GLMod" alt="GLMod License">
  </a>
</p>

# GLMod

Among us GLMod is an open source mod that collects data inside the game to provide a complete match history and various stats on [Good Loss](https://goodloss.fr).

## Features

GLMod provides a comprehensive set of services for tracking game data and enhancing the Among Us experience:

### Core Services

- **Authentication Service**: Manages Good Loss authentication with Steam (a Steam session ticket proves your account to Good Loss, which then gives GLMod a secret session token), login/logout functionality, and ban status handling
- **Integrity Service**: Verifies file integrity and checksums to ensure mod authenticity
- **Item Service**: Manages in-game items (successes) and DLC ownership tracking
- **Rank Service**: Fetches and displays player ranks from Good Loss
- **Service Manager**: Centralized service management with enable/disable capabilities
- **Game State Manager**: Tracks game state, player actions, and match progression

### Good Loss menu

At rest, GLMod only shows a small "GOOD LOSS" tab at the bottom edge of the screen, near the left corner (with Among Us Ranked, the menu takes the Among Us Ranked look: "AMONG US RANKED", its logo and the amber palette of its site). Press **F2** (or click the tab) to slide the menu in, and F2 or **×** to close it.
- **Left / right arrows** switch tabs, **up / down arrows** (or the mouse wheel) scroll the current tab.
- **Header**: the code of the room you are in and, below it, your own room code on the Among Us Ranked servers. You choose it on your Good Loss account page: your games get it when it is free (whoever creates a game first gets a shared code).
- **Account**: Good Loss connection status (connected account, not connected, banned with the reason) and your ranks: one badge per leaderboard with its points, your position and a link to the leaderboard.
- **Friends**: your friends and where they are (in your room, in a lobby with a **Join** button, in a game, offline), the requests you received, and the players of your room and of your last games, to add them. They must accept. A small count on the tab at the bottom of the screen shows the requests waiting; nothing pops up during a game. Block a player, refuse every request, appear offline or choose to be told when a friend comes online. You can also manage your friends on your Good Loss account page.
- **Stats**: your win rates, in total and as crewmate, impostor and neutral, like on your profile page.
- **Last game**: your last finished game on Good Loss (victory or defeat, map, role) with a button to open its recap, and every stat Good Loss computed for you in it (kills, tasks, votes, reports, survival...). After a game, it updates by itself once Good Loss has processed the game.
- **Sessions**: your last 10 sessions (date, number of games, duration, live session) with a link to each session page.
- Mods built on GLMod can hide the menu, remove its tabs or add their own (see [docs/dev.MD](docs/dev.MD)).
- The menu follows the language selected in the game (English and French for now; English when the language is not translated).

### Data Collection

GLMod automatically collects and synchronizes (free play games are never collected nor sent):
- Match history with detailed player statistics
- Role assignments and team compositions
- In-game actions (kills, reports, votes, sabotages, etc.)
- Player positions on the map (sampled every 0.1 s, only the points needed to redraw the path are sent), shown on the game recap
- Task completion progress
- Custom role actions and events
- Player ranks and achievements

When you are logged in, GLMod also tells Good Loss where you are, about every 15 seconds, for the friends list: in the menu, in a lobby or in a game, the server and its region, the mod, your in-game name and your client identifier. The code of your game is only sent on the servers run by Good Loss. Your friends see your status, and the players of your room who use GLMod see your Good Loss account name. See the [privacy policy](https://goodloss.fr/privacy).

# Downloads

| Among Us - Version | Link |
|--------------------|-----------------|
| 2026.3.31          | [Release](https://github.com/MatuxGG/GLMod/releases/latest)

# Installation

Currently, GLMod uses your Steam account to collect your data: Steam proves your Steam ID to Good Loss with a session ticket, so nobody else can log in as you.
Therefore, it's only available with Steam version of Among Us.
There is no support for any other platform.

## Installation for Vanilla on Windows & Steam
1. Download the zip file from the releases for your game version (see above).
2. Find the folder of your game. In Steam, you can right click on the game in your library, a menu will appear. Then, click on Properties > local data > browse.
3. Go to the parent folder named common and make a copy of your Among Us game folder. Then, rename it as you want (for example, "Among Us - GLMod") and move it wherever you want on same drive.
4. Now unzip the files from the .zip into the folder you just copied.
5. Run the game by starting Among Us.exe from this folder (the first launch might take a while).

Not working? You might want to install the dependency [vc_redist](https://aka.ms/vs/16/release/vc_redist.x86.exe)

## Installation for Vanilla on Linux & Steam
1. Download the zip file from the releases for your game version (see above).
2. Install Among Us via Steam
3. Extract the zip file into "~/.steam/steam/steamapps/common/Among Us".
3. Enable winhttp.dll via the proton winecfg (https://docs.bepinex.dev/articles/advanced/proton_wine.html)
4. Launch the game via Steam

## Combining GLMod with another mod
GLMod is a mod that can be combined with any other mod. It is designed to be as simple as possible to use and to be as flexible as possible. It is also designed to be as lightweight as possible to avoid any performance issues.
If you want to combine GLMod with another mod, you can do it by following the instructions below.

1. Download and install the mod you want following its instructions.
2. Download the dll file from the releases for your game version (see above).
3. In the folder of the mod, go to BepInEx/plugins and add the GLMod.dll file.
4. In the folder of the mod again, go to BepInEx/config and add a file called MODNAME.mm where MODNAME is the name of the other mod used.

Note that only vanilla actions and roles will be recorded. If you want to record custom actions and roles, you will have to modify the other mod to use GLMod functions.
If you're a mod developper, see the "Integration with other mods" section below.

# Integrate GLMod to your own mod

See [dev](docs/dev.MD).

# Contributing

If you want to contribute to the project, you need to install this repository on your computer. Here is how to do it:
1. Clone the repository.
2. In Among Us folder, make a copy of the Vanilla Among Us folder content.
3. Install BepInEx in the copied folder by downloading the zip file from the releases for your game version (see above).
4. Open the project in Visual Studio.
5. You can start contributing and testing your changes.

# License

This software is distributed under the GNU GPLv3 License.
