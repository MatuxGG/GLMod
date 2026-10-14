<#
.SYNOPSIS
    Runs the GLMod end-to-end session with 4 Among Us instances in an online lobby.

.DESCRIPTION
    1. Builds GLMod (copied to Among Us/BepInEx/plugins by its own build) and the test harness.
    2. Installs GLMod.TestHarness.dll in the plugins folder for the duration of the run.
    3. Launches 4 instances with GLMOD_API_ENDPOINT pointing to the preprod API:
       instance 0 creates an online game on the currently selected (custom) region, 1-3 join it.
    4. Plays game 1 (crewmates win by tasks), goes back to the lobby, plays game 2 (impostor wins by vote).
    5. Waits for every instance to write result-N.json, then prints a summary.
    6. Stops the instances it launched and uninstalls the harness.

    The harness refuses to run on official Innersloth servers.

.EXAMPLE
    ./GLMod.TestHarness/run-e2e.ps1
#>
param(
    [int]$TimeoutSeconds = 960,
    [string]$ApiEndpoint = 'https://preprod.goodloss.fr/api',
    # Mod name the games are recorded under (GLMod.ConfigService.SetModName); empty keeps GLMod's own (Vanilla)
    [string]$ModName = 'Among Us Ranked',
    # Map selected by the host. The site only draws the map visualization for maps it has an image for (Polus).
    [ValidateSet('Keep', 'Skeld', 'Mira', 'Polus', 'Airship', 'Fungle')]
    [string]$Map = 'Polus',
    # Leave the game windows where Unity puts them instead of tiling them 2x2 on the primary screen
    [switch]$NoTile,
    [switch]$NoBuild,
    [switch]$KeepPlugin,
    [switch]$KeepOtherPlugins
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$gameDir = Join-Path $repo 'Among Us'
$gameExe = Join-Path $gameDir 'Among Us.exe'
$pluginsDir = Join-Path $gameDir 'BepInEx\plugins'
$bepInExDir = Join-Path $gameDir 'BepInEx'
$harnessDll = Join-Path $PSScriptRoot 'bin\Debug\net6.0\GLMod.TestHarness.dll'
$installedHarness = Join-Path $pluginsDir 'GLMod.TestHarness.dll'
$playerCount = 4
# Plugins kept during a run: GLMod, the harness, and the region installer (custom server region)
$keptPlugins = @('GLMod.dll', 'GLMod.TestHarness.dll', 'Mini.RegionInstall.dll')
$mapIds = @{ Skeld = 0; Mira = 1; Polus = 2; Airship = 4; Fungle = 5 }
# Unity stores the window size/position here when the game closes: saved before the run, restored after
$unityPrefsKey = 'HKCU:\Software\Innersloth\Among Us'

Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class E2eWindow {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
}
'@

function Save-UnityScreenPrefs {
    $props = Get-ItemProperty $unityPrefsKey -ErrorAction SilentlyContinue
    if (-not $props) { return @{} }
    $saved = @{}
    foreach ($p in $props.PSObject.Properties | Where-Object { $_.Name -like 'Screenmanager*' }) { $saved[$p.Name] = $p.Value }
    return $saved
}

function Restore-UnityScreenPrefs($saved) {
    foreach ($name in $saved.Keys) {
        Set-ItemProperty -Path $unityPrefsKey -Name $name -Value $saved[$name] -ErrorAction SilentlyContinue
    }
}

# Places instance i in cell i of a 2x2 grid on the primary screen (16:9, centered in its cell).
# Returns $true once the window is where it should be.
function Set-InstanceWindow($process, [int]$index) {
    $process.Refresh()
    $handle = $process.MainWindowHandle
    if ($handle -eq [IntPtr]::Zero) { return $false }

    $area = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $cellWidth = [int][math]::Floor($area.Width / 2)
    $cellHeight = [int][math]::Floor($area.Height / 2)
    $width = [int][math]::Min($cellWidth, [math]::Floor($cellHeight * 16 / 9))
    $height = [int][math]::Floor($width * 9 / 16)
    $x = $area.X + ($index % 2) * $cellWidth + [int](($cellWidth - $width) / 2)
    $y = $area.Y + [math]::Floor($index / 2) * $cellHeight + [int](($cellHeight - $height) / 2)

    $rect = New-Object E2eWindow+Rect
    [void][E2eWindow]::GetWindowRect($handle, [ref]$rect)
    if ($rect.Left -eq $x -and $rect.Top -eq $y -and ($rect.Right - $rect.Left) -eq $width -and ($rect.Bottom - $rect.Top) -eq $height) { return $true }

    # SWP_NOZORDER | SWP_NOACTIVATE
    [void][E2eWindow]::SetWindowPos($handle, [IntPtr]::Zero, $x, $y, $width, $height, 0x0014)
    return $false
}

if (-not (Test-Path $gameExe)) { throw "Among Us not found at $gameExe" }
if (Get-Process -Name 'Among Us' -ErrorAction SilentlyContinue) {
    throw 'Among Us is already running. Close it before starting the end-to-end test.'
}

if (-not $NoBuild) {
    Write-Host '==> Building GLMod and the test harness'
    dotnet build (Join-Path $PSScriptRoot 'GLMod.TestHarness.csproj') --configuration Debug --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
}

$runId = [guid]::NewGuid().ToString('N').Substring(0, 8)
$outputDir = Join-Path $PSScriptRoot ("output\{0}-{1}" -f (Get-Date -Format 'yyyyMMdd-HHmmss'), $runId)
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Copy-Item $harnessDll $installedHarness -Force
Write-Host "==> Run $runId, API $ApiEndpoint, mod '$ModName', map $Map, output: $outputDir"

# Other mods change GLMod's mod name (e.g. ChallengerLevel sets "Challenger Maps"): set them aside
# so the session runs as Vanilla. Restored in the final block below.
$isolatedDir = Join-Path $bepInExDir 'plugins-disabled-by-e2e'
$isolated = @()
if (-not $KeepOtherPlugins) {
    New-Item -ItemType Directory -Path $isolatedDir -Force | Out-Null
    foreach ($dll in Get-ChildItem $pluginsDir -Filter '*.dll') {
        if ($keptPlugins -contains $dll.Name) { continue }
        Move-Item $dll.FullName (Join-Path $isolatedDir $dll.Name) -Force
        $isolated += $dll.Name
    }
    if ($isolated.Count -gt 0) { Write-Host "    plugins set aside for the run: $($isolated -join ', ')" }
}

$savedScreenPrefs = Save-UnityScreenPrefs
$mapId = if ($Map -eq 'Keep') { '' } else { "$($mapIds[$Map])" }

$processes = @()
try {
    for ($i = 0; $i -lt $playerCount; $i++) {
        $env:GLMOD_TEST_INSTANCE = "$i"
        $env:GLMOD_TEST_RUN_ID = $runId
        $env:GLMOD_TEST_OUTPUT = $outputDir
        $env:GLMOD_API_ENDPOINT = $ApiEndpoint
        $env:GLMOD_TEST_MOD_NAME = $ModName
        $env:GLMOD_TEST_MAP_ID = $mapId
        # Unity only writes the shared Player.log for one process: give each instance its own log
        $unityLog = Join-Path $outputDir "player-$i.log"
        $processes += Start-Process -FilePath $gameExe -WorkingDirectory $gameDir -ArgumentList '-logFile', "`"$unityLog`"" -PassThru
        Write-Host "    started instance $i (pid $($processes[-1].Id))"
        # Stagger startups: BepInEx caches are shared between instances
        Start-Sleep -Seconds $(if ($i -eq 0) { 8 } else { 4 })
    }
}
catch {
    foreach ($p in $processes) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    Remove-Item $installedHarness -Force -ErrorAction SilentlyContinue
    foreach ($name in $isolated) { Move-Item (Join-Path $isolatedDir $name) (Join-Path $pluginsDir $name) -Force -ErrorAction SilentlyContinue }
    Restore-UnityScreenPrefs $savedScreenPrefs
    throw
}
finally {
    Remove-Item Env:GLMOD_TEST_INSTANCE, Env:GLMOD_TEST_RUN_ID, Env:GLMOD_TEST_OUTPUT, Env:GLMOD_API_ENDPOINT, Env:GLMOD_TEST_MOD_NAME, Env:GLMOD_TEST_MAP_ID -ErrorAction SilentlyContinue
}

# Window tiling: a window is re-placed until it sits in its cell (Unity may resize it while loading),
# at most a few times so a window moved by hand is left alone afterwards
$tileAttempts = @{}
function Update-WindowTiling {
    if ($NoTile) { return }
    for ($i = 0; $i -lt $processes.Count; $i++) {
        $p = $processes[$i]
        if ($p.HasExited -or $tileAttempts[$i] -ge 5) { continue }
        try {
            if (Set-InstanceWindow $p $i) { $tileAttempts[$i] = 5 } else { $tileAttempts[$i] = 1 + [int]$tileAttempts[$i] }
        }
        catch { $tileAttempts[$i] = 5 }
    }
}

Write-Host "==> Waiting for results (timeout ${TimeoutSeconds}s)"
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$resultFiles = 0..($playerCount - 1) | ForEach-Object { Join-Path $outputDir "result-$_.json" }
try {
    while ((Get-Date) -lt $deadline) {
        $done = @($resultFiles | Where-Object { Test-Path $_ }).Count
        $alive = @($processes | Where-Object { -not $_.HasExited }).Count
        if ($done -eq $playerCount -or $alive -eq 0) { break }
        Update-WindowTiling
        Start-Sleep -Seconds 2
    }
    # Leave time for the host upload and clean quit
    Start-Sleep -Seconds 5
}
finally {
    foreach ($p in $processes) {
        if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    }
    Start-Sleep -Seconds 2
    if (-not $KeepPlugin) { Remove-Item $installedHarness -Force -ErrorAction SilentlyContinue }
    foreach ($name in $isolated) {
        Move-Item (Join-Path $isolatedDir $name) (Join-Path $pluginsDir $name) -Force -ErrorAction SilentlyContinue
    }
    if ((Test-Path $isolatedDir) -and -not (Get-ChildItem $isolatedDir)) { Remove-Item $isolatedDir -Force }
    Restore-UnityScreenPrefs $savedScreenPrefs
    Get-ChildItem $bepInExDir -Filter 'LogOutput*.log' | Copy-Item -Destination $outputDir -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host '==> Results'
$allPassed = $true
for ($i = 0; $i -lt $playerCount; $i++) {
    $file = $resultFiles[$i]
    if (-not (Test-Path $file)) {
        Write-Host ("  [{0}] NO RESULT (see instance-{0}.log)" -f $i) -ForegroundColor Red
        $allPassed = $false
        continue
    }
    $result = Get-Content $file -Raw | ConvertFrom-Json
    $role = if ($result.IsHost) { 'host' } else { 'client' }
    $status = if ($result.Passed) { 'PASSED' } else { 'FAILED' }
    $color = if ($result.Passed) { 'Green' } else { 'Red' }
    Write-Host ("  [{0}] {1,-6} {2} (phase: {3})" -f $i, $role, $status, $result.Phase) -ForegroundColor $color
    foreach ($game in $result.Games) {
        Write-Host ("        game {0}: {1}, GLMod id {2}, {3} failure(s)" -f $game.Number, $game.EndReason, $game.GameId, @($game.Failures).Count)
    }
    foreach ($f in $result.Failures) { Write-Host "        - $f" -ForegroundColor Red }
    foreach ($w in $result.Warnings) { Write-Host "        ! $w" -ForegroundColor Yellow }
    if (-not $result.Passed) { $allPassed = $false }
}

foreach ($n in 1, 2) {
    $plan = Join-Path $outputDir "plan-$n.json"
    if (Test-Path $plan) {
        $p = Get-Content $plan -Raw | ConvertFrom-Json
        Write-Host ("  Plan {0}: impostor={1}, A={2}, B={3}, C={4}" -f $n, $p.Impostor.Name, $p.CrewA.Name, $p.CrewB.Name, $p.CrewC.Name)
    }
}

Write-Host ''
if ($allPassed) {
    Write-Host 'END-TO-END TEST PASSED' -ForegroundColor Green
    exit 0
}
Write-Host "END-TO-END TEST FAILED (logs in $outputDir)" -ForegroundColor Red
exit 1
