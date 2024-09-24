# Services

GLMod.disableService("StartGame");
GLMod.disableService("EndGame");
GLMod.disableService("Tasks");
GLMod.disableService("TasksMax");
GLMod.disableService("Exiled");
GLMod.disableService("Kills");
GLMod.disableService("Killed");
GLMod.disableService("KilledFirst");
GLMod.disableService("BodyReported");
GLMod.disableService("Emergencies");
GLMod.disableService("Turns");

# Process

GLMod.SetModName("Challenger");
GLMod.StartGame("GVOVVF", "Polus", false);

GLMod.AddPlayer("Matux", "Sheriff", "Crewmate");
GLMod.AddPlayer("Matux", "Guesser", "Impostor");

GLMod.AddMyPlayer();

GLMod.SetWinnerTeams(new List<string>() { "Crewmate" });
GLMod.AddWinnerPlayer("Matux");

GLMod.EndGame();

