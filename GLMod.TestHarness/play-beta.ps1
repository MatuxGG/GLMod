<#
.SYNOPSIS
    Launches Among Us by hand on the beta server of GLPlugin: region Beta Ranked EU, preprod API.

.DESCRIPTION
    Builds GLMod (copied to Among Us/BepInEx/plugins by its own build) and the test harness, installs the
    harness for the time of the game, and launches the game with the other plugins (Among Us Ranked...).
    The region is only set in memory: the regionInfo.json of the player does not change. A game launched
    from Steam stays on the prod API and on the region of the mod.

.EXAMPLE
    ./GLMod.TestHarness/play-beta.ps1
#>
param(
    [string]$ApiEndpoint = 'https://preprod.goodloss.fr/api',
    [switch]$NoBuild,
    # Closes the game after this many seconds (0: when you close it). For a quick check.
    [int]$StopAfterSeconds = 0
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$gameDir = Join-Path $repo 'Among Us'
$gameExe = Join-Path $gameDir 'Among Us.exe'
$harnessDll = Join-Path $PSScriptRoot 'bin\Debug\net6.0\GLMod.TestHarness.dll'
$installedHarness = Join-Path $gameDir 'BepInEx\plugins\GLMod.TestHarness.dll'

if (-not (Test-Path $gameExe)) { throw "Among Us not found at $gameExe" }
if (Get-Process -Name 'Among Us' -ErrorAction SilentlyContinue) {
    throw 'Among Us is already running. Close it first.'
}

if (-not $NoBuild) {
    Write-Host '==> Building GLMod and the test harness'
    dotnet build (Join-Path $PSScriptRoot 'GLMod.TestHarness.csproj') --configuration Debug --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
}

Copy-Item $harnessDll $installedHarness -Force
try {
    $env:GLMOD_PLAY_BETA = '1'
    $env:GLMOD_API_ENDPOINT = $ApiEndpoint
    Write-Host "==> Among Us on Beta Ranked EU, API $ApiEndpoint"
    $process = Start-Process -FilePath $gameExe -WorkingDirectory $gameDir -PassThru
    if ($StopAfterSeconds -gt 0) {
        if (-not $process.WaitForExit($StopAfterSeconds * 1000)) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    $process.WaitForExit()
}
finally {
    Remove-Item Env:GLMOD_PLAY_BETA, Env:GLMOD_API_ENDPOINT -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Remove-Item $installedHarness -Force -ErrorAction SilentlyContinue
}
