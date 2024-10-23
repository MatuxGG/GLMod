<p align="center">
  <a href="https://github.com/MatuxGG/GLMod">
    <img src="https://img.shields.io/badge/GLMod-v4.0.2-blue" alt="GLMod Version">
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

# Downloads

| Among Us - Version | Link |
|--------------------|-----------------|
| 2024.10.29s        | [Release](https://github.com/MatuxGG/GLMod/releases/latest)
| 2024.09.04s        | [Release](https://github.com/MatuxGG/GLMod/releases/latest)
| 2024.06.18s        | [Release](https://github.com/MatuxGG/GLMod/releases/latest)

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

# Integrate GLMod to you own mod

See [dev](docs/dev.md).

# Contributing

If you want to contribute to the project, you need to install this repository on your computer. Here is how to do it:
1. Clone the repository.
2. In Among Us folder, make a copy of the Vanilla Among Us folder content.
3. Install BepInEx in the copied folder by downloading the zip file from the releases for your game version (see above).
4. Open the project in Visual Studio.
5. You can start contributing and testing your changes.

# License

This software is distributed under the GNU GPLv3 License.
