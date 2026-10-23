# deploy.ps1 - copy the built plugin into the game and verify by MD5.
# Pure ASCII on purpose: PowerShell 5.1 reads UTF-8 without BOM as ANSI and chokes on CJK.
#
# Why this file exists: the game path contains a long digit run (SteamLibrary).
# Typing it by hand is a coin flip. Never hand-type paths - let the script hold them.

param(
    [string]$GameDir = "d:\SteamLibrary\steamapps\common\Silver Pines"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src  = Join-Path $root "bin\Release\SilverPinesCombat.dll"
$dstDir = Join-Path $GameDir "BepInEx\plugins\SilverPinesCombat"
$dst  = Join-Path $dstDir "SilverPinesCombat.dll"

Write-Host "src = $src"
Write-Host "dst = $dst"
Write-Host ""

if (-not (Test-Path $src)) { Write-Host "[FAIL] built dll not found - run: dotnet build -c Release"; exit 1 }
if (-not (Test-Path $GameDir)) { Write-Host "[FAIL] game dir not found: $GameDir"; exit 1 }

$proc = Get-Process -Name Carpenter -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "[BLOCKED] Carpenter.exe is running (pid $($proc.Id)). Close the game first."
    exit 2
}

if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Path $dstDir -Force | Out-Null }

Copy-Item $src $dst -Force

$a = (Get-FileHash $src -Algorithm MD5).Hash
$b = (Get-FileHash $dst -Algorithm MD5).Hash

Write-Host ""
Write-Host ("src md5 = " + $a)
Write-Host ("dst md5 = " + $b)
if ($a -eq $b) {
    Write-Host ("[OK] deployed, " + (Get-Item $dst).Length + " bytes")
    exit 0
} else {
    Write-Host "[FAIL] md5 mismatch"
    exit 3
}
