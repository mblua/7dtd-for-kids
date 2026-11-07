<# :
@echo off
rem ======================================================================
rem  7 Days For Kids - installer
rem  Double-click this file. It finds your 7 Days to Die install folder
rem  (or asks you for it) and copies the mod into the game's Mods folder.
rem  Optional: pass the game folder as the first argument.
rem
rem  This file is a batch + PowerShell hybrid: the batch part below runs
rem  the PowerShell code that follows the closing marker.
rem ======================================================================
setlocal
set "SFK_REPO_DIR=%~dp0"
set "SFK_GAME_ARG=%~1"
set "SFK_SELF=%~f0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-Expression ([System.IO.File]::ReadAllText($env:SFK_SELF))"
set "SFK_EXIT=%ERRORLEVEL%"
echo.
pause
exit /b %SFK_EXIT%
#>

$ErrorActionPreference = 'Stop'
$AppId    = '251570'               # Steam app id of 7 Days to Die
$GameDirName = '7 Days To Die'
$ModName  = '7DaysForKids'

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "WARNING: $msg" -ForegroundColor Yellow }
function Write-Fail($msg) { Write-Host "ERROR: $msg" -ForegroundColor Red }

function Test-GameDir($path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return $false }
    $path = $path.Trim().Trim('"')
    return (Test-Path -LiteralPath (Join-Path $path '7DaysToDie_Data')) -and
           (Test-Path -LiteralPath (Join-Path $path 'Mods'))
}

function Get-SteamRoots {
    $roots = @()
    foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam') {
        try {
            $p = Get-ItemProperty -Path $key -ErrorAction Stop
            foreach ($name in 'SteamPath', 'InstallPath') {
                if ($p.$name) { $roots += ($p.$name -replace '/', '\') }
            }
        } catch { }
    }
    return $roots | Select-Object -Unique
}

function Get-SteamLibraries {
    $libs = @()
    foreach ($root in Get-SteamRoots) {
        $libs += $root
        $vdf = Join-Path $root 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            $text = Get-Content -LiteralPath $vdf -Raw
            foreach ($m in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $libs += ($m.Groups[1].Value -replace '\\\\', '\')
            }
        }
    }
    return $libs | Select-Object -Unique
}

function Find-GameDir {
    $candidates = @()

    # 1. Steam libraries (registry + libraryfolders.vdf + app manifest)
    foreach ($lib in Get-SteamLibraries) {
        $manifest = Join-Path $lib "steamapps\appmanifest_$AppId.acf"
        if (Test-Path -LiteralPath $manifest) {
            $m = [regex]::Match((Get-Content -LiteralPath $manifest -Raw), '"installdir"\s+"([^"]+)"')
            if ($m.Success) { $candidates += (Join-Path $lib ('steamapps\common\' + $m.Groups[1].Value)) }
        }
        $candidates += (Join-Path $lib "steamapps\common\$GameDirName")
    }

    # 2. Common locations on every drive
    $candidates += "${env:ProgramFiles(x86)}\Steam\steamapps\common\$GameDirName"
    $candidates += "$env:ProgramFiles\Steam\steamapps\common\$GameDirName"
    foreach ($d in Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue) {
        $candidates += "$($d.Root)SteamLibrary\steamapps\common\$GameDirName"
        $candidates += "$($d.Root)Steam\steamapps\common\$GameDirName"
        $candidates += "$($d.Root)Games\Steam\steamapps\common\$GameDirName"
    }

    foreach ($c in ($candidates | Select-Object -Unique)) {
        if (Test-GameDir $c) { return (Resolve-Path -LiteralPath $c).Path }
    }
    return $null
}

# ---------------------------------------------------------------------
Write-Host ''
Write-Host '7 Days For Kids - installer' -ForegroundColor Green
Write-Host ''

# Mod source (next to this .bat)
$repoDir = $env:SFK_REPO_DIR
$modSrc  = Join-Path $repoDir "Mods\$ModName"
if (-not (Test-Path -LiteralPath (Join-Path $modSrc 'ModInfo.xml'))) {
    Write-Fail "Mod files not found at: $modSrc"
    Write-Host 'Keep install.bat in the same folder as the "Mods" folder (extract the whole download first).'
    exit 1
}
if (-not (Test-Path -LiteralPath (Join-Path $modSrc "$ModName.dll"))) {
    Write-Fail "$ModName.dll is missing in $modSrc"
    exit 1
}

# Game folder
$gameDir = $null
if (Test-GameDir $env:SFK_GAME_ARG) {
    $gameDir = $env:SFK_GAME_ARG.Trim().Trim('"')
} else {
    Write-Step 'Looking for 7 Days to Die...'
    $found = Find-GameDir
    if ($found) {
        Write-Host "Found: $found"
        $ans = Read-Host 'Install the mod here? [Y/n]'
        if ($ans -eq '' -or $ans -match '^[YySs]') { $gameDir = $found }
    } else {
        Write-Warn 'Could not find the game automatically.'
    }
}

while (-not $gameDir) {
    Write-Host ''
    Write-Host 'Please type (or paste) your 7 Days to Die folder.'
    Write-Host "It is usually:  C:\Program Files (x86)\Steam\steamapps\common\$GameDirName"
    Write-Host "            or: <Drive>:\SteamLibrary\steamapps\common\$GameDirName"
    Write-Host 'Tip: in Steam, right-click the game > Manage > Browse local files.'
    $typed = Read-Host 'Game folder (empty to cancel)'
    if ([string]::IsNullOrWhiteSpace($typed)) { Write-Host 'Cancelled.'; exit 1 }
    $typed = $typed.Trim().Trim('"')
    if (Test-GameDir $typed) { $gameDir = $typed } else { Write-Fail "That folder does not look like a 7 Days to Die install: $typed" }
}

# Game must be closed (files are locked while it runs)
if (Get-Process -Name '7DaysToDie' -ErrorAction SilentlyContinue) {
    Write-Fail 'The game is running. Close it and run install.bat again.'
    exit 1
}

if (-not (Test-Path -LiteralPath (Join-Path $gameDir 'Mods\0_TFP_Harmony'))) {
    Write-Warn 'Mods\0_TFP_Harmony is missing. The mod needs it. In Steam, use "Verify integrity of game files" to restore it.'
}

# Copy: make Mods\7DaysForKids identical to the download (only that folder is touched)
$dest = Join-Path $gameDir "Mods\$ModName"
Write-Step "Copying mod to: $dest"
& robocopy $modSrc $dest /MIR /XD Scripts bin obj /XF *.pdb .gitignore /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) {
    Write-Fail "Copy failed (robocopy exit code $LASTEXITCODE)."
    exit 1
}

Write-Host ''
Write-Host 'Done! The mod is installed.' -ForegroundColor Green
Write-Host 'IMPORTANT: start the game with EasyAntiCheat (EAC) turned OFF, or the mod will not load.'
Write-Host "To uninstall, delete: $dest"
exit 0
