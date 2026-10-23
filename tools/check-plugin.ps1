# Silver Pines Combat - post-launch verification (ASCII only on purpose;
# PowerShell 5.1 mis-parses UTF-8-without-BOM scripts containing CJK text)

$ErrorActionPreference = 'SilentlyContinue'
$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Silver Pines'
$Bep     = Join-Path $GameDir 'BepInEx'
$Log     = Join-Path $Bep 'LogOutput.log'
$Plugin  = Join-Path $Bep 'plugins\SilverPinesCombat\SilverPinesCombat.dll'
$Cfg     = Join-Path $Bep 'config\silverpines.combat.cfg'

Write-Host '================================================'
Write-Host ' Silver Pines Combat - verification'
Write-Host '================================================'

Write-Host ''
Write-Host '--- 1. files ---'
Write-Host ("  plugin dll      : " + $(if (Test-Path $Plugin) { 'OK  ' + (Get-Item $Plugin).Length + ' bytes' } else { 'MISSING' }))
Write-Host ("  config (auto)   : " + $(if (Test-Path $Cfg) { 'OK' } else { 'not yet (created on first load)' }))
Write-Host ("  interop dlls    : " + $(if (Test-Path (Join-Path $Bep 'interop')) { (Get-ChildItem (Join-Path $Bep 'interop') -Filter *.dll).Count } else { 'MISSING' }))

Write-Host ''
Write-Host '--- 2. process ---'
$p = Get-Process -Name Carpenter -ErrorAction SilentlyContinue
if ($p) { Write-Host ("  Carpenter.exe running, pid=" + $p.Id + " started=" + $p.StartTime.ToString('HH:mm:ss')) }
else { Write-Host '  not running' }
$logTime = (Get-Item $Log).LastWriteTime
$dllTime = (Get-Item $Plugin).LastWriteTime
if ($p -and $p.StartTime -lt $dllTime) {
    Write-Host '  !! game started BEFORE plugin was deployed -> RESTART needed'
} elseif ($p) {
    Write-Host '  game started after plugin deploy -> plugin should be loaded'
}

Write-Host ''
Write-Host '--- 3. plugin load lines ---'
if (Test-Path $Log) {
    $all = Get-Content $Log
    $hits = $all | Select-String -Pattern '\[SPT\]|SilverPinesCombat|silverpines'
    if ($hits) { $hits | ForEach-Object { Write-Host ('  ' + $_.Line.Trim()) } }
    else { Write-Host '  no [SPC] lines found -> plugin did not load' }
} else { Write-Host '  LogOutput.log missing' }

Write-Host ''
Write-Host '--- 4. errors ---'
if (Test-Path $Log) {
    $bad = Get-Content $Log | Select-String -Pattern '\[Error|\[Fatal|Exception|Failed to load|Skipping'
    if ($bad) { $bad | Select-Object -First 20 | ForEach-Object { Write-Host ('  !! ' + $_.Line.Trim()) } }
    else { Write-Host '  none' }
}

Write-Host ''
Write-Host '--- 5. tail ---'
if (Test-Path $Log) { Get-Content $Log -Tail 25 | ForEach-Object { Write-Host ('  ' + $_) } }

Write-Host ''
Write-Host '--- verdict ---'
$spt = $false
if (Test-Path $Log) { $spt = [bool](Get-Content $Log | Select-String -Pattern 'loaded \(be\.789') }
if ($spt) {
    Write-Host '  [OK] plugin loaded'
    if (Test-Path $Log) {
        $diag = (Get-Content $Log | Select-String -Pattern 'diagnose: (\d+)/(\d+)')
        if ($diag) { Write-Host ('  ' + $diag[-1].Line.Trim()) }
    }
} else {
    Write-Host '  [NOT LOADED] restart the game, then run this again'
}
Write-Host ''
