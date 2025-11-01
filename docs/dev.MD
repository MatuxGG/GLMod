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

### Translation Service (ITranslationService)
Handles multi-language support with translations from Good Loss API.

```csharp
// Translate a key
string translated = GLMod.TranslationService.Translate("key");

// Set language
GLMod.TranslationService.SetLanguage("fr");

// Load translations
yield return GLMod.TranslationService.LoadTranslations();
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

## Service Architecture

GLMod follows a clean architecture pattern with dependency injection and interface-based services. All services are accessible through static properties in the main `GLMod` class:

```csharp
GLMod.AuthService      // IAuthenticationService
GLMod.TranslationService  // ITranslationService
GLMod.ConfigService    // IConfigurationService
GLMod.IntegrityService // IIntegrityService
GLMod.ItemService      // IItemService
GLMod.RankService      // IRankService
GLMod.MapService       // IMapService
GLMod.ServiceManager   // IServiceManager
GLMod.GameStateManager // IGameStateManager
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
