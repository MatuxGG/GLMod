<p align="center">
  <a href="https://github.com/MatuxGG/GLMod">
    <img src="https://img.shields.io/badge/GLMod-v5.6.0-blue" alt="GLMod Version">
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

- **Authentication Service**: Manages Good Loss authentication with Steam, login/logout functionality, and ban status handling
- **Integrity Service**: Verifies file integrity and checksums to ensure mod authenticity
- **Item Service**: Manages in-game items (successes) and DLC ownership tracking
- **Rank Service**: Fetches and displays player ranks from Good Loss
- **Service Manager**: Centralized service management with enable/disable capabilities
- **Game State Manager**: Tracks game state, player actions, and match progression

### Good Loss menu

At rest, GLMod only shows a small "GOOD LOSS" tab at the bottom edge of the screen, near the left corner (with Among Us Ranked, the menu takes the Among Us Ranked look: "AMONG US RANKED", its logo and the amber palette of its site). Press **F2** (or click the tab) to slide the menu in, and F2 or **×** to close it.
- **Left / right arrows** switch tabs, **up / down arrows** (or the mouse wheel) scroll the current tab.
- **Account**: Good Loss connection status (connected account, not connected, banned with the reason) and your ranks: one badge per leaderboard with its points, your position and a link to the leaderboard.
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
- Task completion progress
- Custom role actions and events
- Player ranks and achievements

# Downloads

| Among Us - Version | Link |
|--------------------|-----------------|
| 2026.3.31          | [Release](https://github.com/MatuxGG/GLMod/releases/latest)

# Installation

Currently, GLMod uses your Steam ID to collect your data.
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
