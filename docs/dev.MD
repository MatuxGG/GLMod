# Integrate GLMod to your own mod

Download GLMod.dll for your game version and add GLMod.dll to BepInEx/plugins.
In files where you use GLMod, use:

```csharp
using GLMod;
using GLMod.Services;
```

You also need to define the name of your mod in your Load() function:

```csharp
GLMod.setModName("YOUR_MOD_NAME");
```

Everything is set up and everything will be recorded. The game will be available on players' match history on Good Loss.

Note that GLMod uses RPC 240.

## Available Services

GLMod provides several services that you can use in your mod:

### Authentication Service (IAuthenticationService)
Manages Steam authentication and user login state.

```csharp
// Check if user is logged in
if (GLMod.AuthService.IsLoggedIn)
{
    string accountName = GLMod.AuthService.GetAccountName();
    string token = GLMod.AuthService.Token;
}

// Perform login
yield return GLMod.AuthService.Login(success =>
{
    if (success)
    {
        // Login successful
    }
});
```

### Configuration Service (IConfigurationService)
Manages mod configuration and custom mod names.

```csharp
// Get mod name
string modName = GLMod.ConfigService.ModName;

// Set custom mod name
GLMod.ConfigService.SetModName("MyCustomMod");
```

### Integrity Service (IIntegrityService)
Verifies file integrity and checksums.

```csharp
// Verify GLMod integrity
yield return GLMod.IntegrityService.VerifyGLMod(isValid =>
{
    if (isValid)
    {
        // GLMod is authentic
    }
});

// Verify any DLL
yield return GLMod.IntegrityService.VerifyDll("checksumId", "path/to/file.dll", isValid =>
{
    // Handle verification result
});
```

### Item Service (IItemService)
Manages in-game items and DLC ownership.

```csharp
// Get items
var items = GLMod.ItemService.Items;

// Get DLC ownerships
var dlcOwned = GLMod.ItemService.SteamOwnerships;

// Reload items
yield return GLMod.ItemService.ReloadItems();
```

### Rank Service (IRankService)
Fetches player ranks from Good Loss.

```csharp
// Get player rank
yield return GLMod.RankService.GetRank(steamId, rank =>
{
    if (rank != null && string.IsNullOrEmpty(rank.error))
    {
        // rank.id, rank.name, rank.percent
    }
});
```

### Map Service (IMapService)
Provides map detection and information.

```csharp
// Get current map name
string mapName = GLMod.MapService.GetMapName();
```

### Service Manager (IServiceManager)
Centralized management of game event services.

```csharp
// Enable/disable services
GLMod.ServiceManager.EnableService(ServiceType.StartGame);
GLMod.ServiceManager.DisableService(ServiceType.EndGame);

// Check if service exists
bool exists = GLMod.ServiceManager.ExistsService(ServiceType.Kills);
```

### Menu Service (IMenuService)
State of the Good Loss menu (F2 or a click on the "GOOD LOSS" tab in the bottom-left corner). From your plugin's `Load()` (GLMod is loaded first thanks to `[BepInDependency("glmod")]`) or later, your mod can:
- **hide the menu completely**,
- **remove** GLMod's built-in tabs,
- **add** its own tabs, at the end or at a given position.

Every tab has a unique, stable **id**. Built-in tabs, in this order:

| Tab | Id (`GLMod.Constants.GameConstants`) |
|-----|--------------------------------------|
| Account | `MENU_TAB_ACCOUNT` = `"glmod.account"` |
| Stats | `MENU_TAB_STATS` = `"glmod.stats"` |
| Last game | `MENU_TAB_LAST_GAME` = `"glmod.lastgame"` |
| Sessions | `MENU_TAB_SESSIONS` = `"glmod.sessions"` |

#### Hide the menu

```csharp
GLMod.MenuService.Enabled = false; // no "GOOD LOSS" tab, no panel; F2, arrows and clicks are left to the game
GLMod.MenuService.Enabled = true;  // back (closed)
```

The menu also hides itself when no tab is left. `IsVisible` tells whether it is shown (enabled and at least one tab), `IsOpen` whether the panel is open; `Open()` has no effect while it is not visible.

#### Remove built-in tabs

```csharp
using GLMod.Constants;

GLMod.MenuService.UnregisterTab(GameConstants.MENU_TAB_SESSIONS);
GLMod.MenuService.UnregisterTab(GameConstants.MENU_TAB_LAST_GAME);
```

#### Add tabs

Draw the content with the `GLMod.Menu.MenuUI` widgets: they follow the menu style and are laid out from top to bottom, and a tab that is too long scrolls automatically. Prefix your ids with your mod name to avoid collisions: a second tab with an existing id is refused.

```csharp
using GLMod.Menu;

// At the end of the tab bar
IMenuTab rules = GLMod.MenuService.RegisterTab("mymod.rules", "Rules",
    draw: () =>
    {
        MenuUI.Heading("Season 3");
        MenuUI.Tags(("Ranked", MenuUI.Accent), ("10 players", MenuUI.Success));
        MenuUI.StatGrid(3, ("Kill cooldown", "25 s"), ("Tasks", "4"), ("Meetings", "1"));
        if (MenuUI.Button("Open the rules"))
        {
            UnityEngine.Application.OpenURL("https://example.org/rules");
        }
    },
    onShown: () => { /* refresh your data here (called when the tab becomes visible) */ });

// At a given position (0 = first tab): here, right after Account
GLMod.MenuService.InsertTab(1, new MenuTab("mymod.news", "News", () => MenuUI.Label("Patch 2.3 is out!")));

// Later
GLMod.MenuService.Select("mymod.rules");   // shown on the next opening
GLMod.MenuService.Open();
GLMod.MenuService.UnregisterTab("mymod.news");
IMenuTab found = GLMod.MenuService.FindTab("mymod.rules");
```

For more control, implement `IMenuTab` (`Id`, `Title`, `OnShown()`, `Draw()`) and register it with `RegisterTab(tab)` or `InsertTab(index, tab)`.

Widgets: `Heading` (section title), `Highlight` (large display text), `Label` (optionally colored), `Muted`, `Field(name, value)`, `Button` (gradient call to action, returns true when clicked), `Row(text, buttonText, color)` (card with a colored edge and an optional small button), `ProgressBar(label, fraction, valueText)`, `Tiles(("Label", "value"), ...)` (row of stat tiles), `StatGrid(columns, ("Label", value), ...)` (compact grid of stat tiles, null values skipped), `Tags(("Text", color), ...)` (colored pills that wrap), `WinRate(label, counts, percent, color)` (win rate card with a colored bar), `Space`, `Separator`, and the colors `MenuUI.Accent`, `AccentStrong`, `Text`, `MutedText`, `Success`, `Danger`. The colors follow the current theme (`MenuUI.Theme`: Good Loss, or Among Us Ranked when the mod is Among Us Ranked), so read them while drawing instead of caching them. Sizes passed to the widgets (e.g. `Space(10f)`) are 1080p reference units; `MenuUI.Width` is in screen pixels.

Rules:
- Everything runs on the Unity main thread. `Draw()` is called several times per frame (IMGUI): keep it cheap and never start requests from it, start them from `OnShown()` (e.g. with a coroutine) and draw the cached result.
- An exception in your tab is logged once and replaced by an error message, it does not break the menu.
- `GUI.DrawTexture` is stripped from the Among Us build: drawing it from a tab throws. Use the `MenuUI` widgets (or `GUI.Label` / `GUI.Button` / `GUI.Box`).

### Translation Service (ITranslationService)
The menu follows the language selected in the game (`Language`: "en", "fr", "pt-BR"...), with English when a text is not translated. Translate your own tabs the same way: add your texts per language, then read them with `Get` (also in `Title`, so the tab bar follows language changes).

```csharp
GLMod.TranslationService.AddTranslations("en", new Dictionary<string, string> { { "mymod.rules", "Rules" }, { "mymod.players", "{0} players" } });
GLMod.TranslationService.AddTranslations("fr", new Dictionary<string, string> { { "mymod.rules", "Règles" }, { "mymod.players", "{0} joueurs" } });

GLMod.MenuService.RegisterTab(new MyRulesTab()); // Title => GLMod.TranslationService.Get("mymod.rules")
string players = GLMod.TranslationService.Get("mymod.players", 10);
```

Missing texts fall back to the base language ("pt-BR" -> "pt"), then English, then the key itself. `AddTranslations` can also add a new language to GLMod's own keys (see `GLMod/Resources/Translations/en.json`).

### Free play
Free play games are never recorded nor sent to Good Loss: in free play, `GLMod.StartGame` creates no game and `SendGame` / `AddMyPlayer` / `EndGame` send nothing, even when your mod calls them.

### Game History Service (IGameHistoryService)
Local history of the games played with GLMod, most recent first (stored on the player's computer only).

```csharp
GLGameHistoryEntry last = GLMod.GameHistoryService.Latest; // null when empty
// last.gameId, last.recapUrl, last.map, last.modName, last.role, last.team, last.won, last.endedAtMs
GLMod.GameHistoryService.EntryRecorded += entry => { /* a game just ended */ };
```

## Service Architecture

GLMod follows a clean architecture pattern with dependency injection and interface-based services. All services are accessible through static properties in the main `GLMod` class:

```csharp
GLMod.AuthService      // IAuthenticationService
GLMod.ConfigService    // IConfigurationService
GLMod.IntegrityService // IIntegrityService
GLMod.ItemService      // IItemService
GLMod.RankService      // IRankService
GLMod.MapService       // IMapService
GLMod.ServiceManager   // IServiceManager
GLMod.GameStateManager // IGameStateManager
GLMod.GameHistoryService // IGameHistoryService
GLMod.MenuService      // IMenuService
GLMod.PlayerService    // IPlayerService (GET /player/sessions, /player/last-game, /player/ranks, /player/stats)
GLMod.TranslationService // ITranslationService (menu texts in the game language)
```

### Service Initialization

All services are automatically initialized when GLMod loads. You can verify service availability at startup through the BepInEx console logs. Each service will display its initialization status.

### Thread Safety

Services that interact with the Good Loss API use coroutines for asynchronous operations. Always use `yield return` when calling service methods that return `IEnumerator`.

## Customize GLMod

### <ins>Disable services</ins>

GLMod comes with a lot of default services enabled. You have to disable each one that you want to overwrite by adding this call in your Load() function:

```csharp
GLMod.disableService("SERVICE_TO_DISABLE");
// Or using enum:
GLMod.ServiceManager.DisableService(ServiceType.StartGame);
```

If you have custom roles, you should disable these default services:

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

```csharp
GLMod.StartGame(GAME_CODE, GAME_MAP, false);
```

Replace `GAME_CODE` and `GAME_MAP` with the actual game code and map.

For example:

```csharp
GLMod.StartGame("ABCDEF", "Polus", false);
```

Then, for each role you give to players, you have to use this function on each client:

```csharp
GLMod.AddPlayer(PLAYER_NAME, PLAYER_ROLE, PLAYER_TEAM);
```

Note that for non crewmate/impostor roles (neutral, hybrid, ... roles), the team should be the same as the role.

For example:

```csharp
GLMod.AddPlayer("Matux1", "Sheriff", "Crewmate");
GLMod.AddPlayer("Sean", "Guesser", "Impostor");
GLMod.AddPlayer("Paul", "Jester", "Jester");
```

For each client, when all roles are set on itself, you should validate the start game process by calling these functions:

```csharp
GLMod.SendGame();
GLMod.AddMyPlayer();
```

Then, the start game is correctly overwritten!

### <ins>Add actions</ins>

Basically, in the game, there are a few actions recorded (kills, exiles, emergencies, ...).
But you can define custom ones for your roles with this function:

```csharp
GLMod.currentGame.addAction(SOURCE_PLAYER_NAME, TARGET_PLAYER_NAME, CUSTOM_ACTION);
```

For example, if Sean (Sheriff) kills Paul (Impostor), you can add:

```csharp
GLMod.currentGame.addAction("Sean", "Paul", "killed as Sheriff");
```

In this example, the next sentence will be shown in Good Loss history : "Send killed as Sheriff Paul".

You can also let the source or the target player empty.

For example, if a role can kill itself, you can have:

```csharp
GLMod.currentGame.addAction("Matux", "", "killed itself");
```

And in history: "Matux killed itself".

### <ins>Overwrite EndGame</ins>

In your mod, you should end a game by declaring an end game on each client.

You need to declare each team that won. Each player with this team will be added to winners.

```csharp
List<string> WinList = new List<string>();
WinList.Add(TEAM_1);
WinList.Add(TEAM_2);
// ...
GLMod.SetWinnerTeams(WinList);
```

If a team is called differently than the team role given to its members, you need to also add each player that won like this:

```csharp
GLMod.AddWinnerPlayer(PLAYER_NAME);
```

After all of these, you need to validate the end game with this function:

```csharp
GLMod.EndGame();
```

Here is an example of a complete endgame:

```csharp
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
