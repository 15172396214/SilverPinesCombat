# ============================================================
#  Silver Pines - modding recon script
#  Game    : Silver Pines (Steam AppID 2333000, Unity 6000.3.15f1)
#  Purpose : detect scripting backend (Mono vs IL2CPP), Unity build,
#            existing mod loader, data dirs. Run BEFORE installing anything.
#  Usage   : powershell -ExecutionPolicy Bypass -File recon.ps1
# ============================================================

$ErrorActionPreference = 'SilentlyContinue'
$AppId = 2333000
$Out   = @()

function Log($s) { Write-Host $s; $script:Out += $s }
function Head($s) { Log ""; Log ("--- " + $s + " ---") }

Log "==============================================="
Log " Silver Pines modding recon"
Log " $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Log "==============================================="

# ---------- 1. locate the game ----------
Head "1. locating game install"

$steamRoots = @()
$reg = Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name SteamPath
if ($reg -and $reg.SteamPath) { $steamRoots += ($reg.SteamPath -replace '/', '\') }
$steamRoots += "C:\Program Files (x86)\Steam"
$steamRoots += "C:\Program Files\Steam"
$steamRoots = $steamRoots | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique

$libs = @()
$seen = @{}
foreach ($sr in $steamRoots) {
    $lf = Join-Path $sr 'steamapps\libraryfolders.vdf'
    if (Test-Path $lf) {
        $key = $sr.ToLower()
        if (-not $seen[$key]) { $seen[$key] = 1; $libs += $sr }
        $txt = Get-Content $lf -Raw
        foreach ($m in [regex]::Matches($txt, '"path"\s+"([^"]+)"')) {
            $p = ($m.Groups[1].Value -replace '\\\\', '\')
            $key = $p.ToLower()
            if (-not $seen[$key]) { $seen[$key] = 1; $libs += $p }
        }
    }
}
Log ("steam libraries found: " + $libs.Count)
foreach ($l in $libs) { Log ("  " + $l) }

$gameDir = $null
foreach ($l in $libs) {
    $acf = Join-Path $l "steamapps\appmanifest_$AppId.acf"
    if (Test-Path $acf) {
        $m = [regex]::Match((Get-Content $acf -Raw), '"installdir"\s+"([^"]+)"')
        if ($m.Success) {
            $cand = Join-Path $l ("steamapps\common\" + $m.Groups[1].Value)
            if (Test-Path $cand) { $gameDir = $cand }
        }
    }
}

if (-not $gameDir) {
    Log ""
    Log "!! NOT FOUND via appmanifest. Searching steamapps/common for UnityPlayer.dll ..."
    foreach ($l in $libs) {
        $common = Join-Path $l 'steamapps\common'
        if (Test-Path $common) {
            Get-ChildItem $common -Directory | ForEach-Object {
                $hit = Get-ChildItem $_.FullName -Recurse -Depth 2 -Filter 'UnityPlayer.dll' | Select-Object -First 1
                if ($hit) {
                    $nm = $_.Name.ToLower()
                    if ($nm -like '*silver*' -or $nm -like '*pine*') {
                        $gameDir = $_.FullName
                        Log ("  candidate: " + $gameDir)
                    }
                }
            }
        }
    }
}

if (-not $gameDir) {
    Log ""
    Log "!! GAME NOT INSTALLED (or not in a Steam library)."
    Log "   Buy/install first: steam://install/$AppId  (CNY 81 launch price)"
    Log "   This script cannot continue. Re-run after install."
    $Out | Out-File -FilePath (Join-Path $PSScriptRoot 'recon-result.txt') -Encoding utf8
    exit 1
}

Log ("GAME DIR : " + $gameDir)
Set-Location $gameDir

# ---------- 2. scripting backend: Mono or IL2CPP ----------
Head "2. scripting backend (THIS DECIDES YOUR ENTIRE TOOLCHAIN)"

$isIl2cpp = Test-Path (Join-Path $gameDir 'GameAssembly.dll')
$dataDir = Get-ChildItem $gameDir -Directory -Filter '*_Data' | Select-Object -First 1
$hasMonoManaged = $false
if ($dataDir) {
    $hasMonoManaged = Test-Path (Join-Path $dataDir.FullName 'Managed\Assembly-CSharp.dll')
}

if ($isIl2cpp) {
    Log "BACKEND  : IL2CPP"
    Log "  marker : GameAssembly.dll present"
    Log "  -> needs BepInEx 6 (bleeding edge) + Il2CppInterop + Il2CppDumper"
    Log "  -> you edit NOTHING in C# directly; you patch il2cpp methods"
} elseif ($hasMonoManaged) {
    Log "BACKEND  : Mono"
    Log "  marker : *_Data\Managed\Assembly-CSharp.dll present"
    Log "  -> BepInEx 5/6 works out of the box, ILSpy reads game logic directly"
} else {
    Log "BACKEND  : UNKNOWN - neither marker found"
}

if ($dataDir) {
    Log ("DATA DIR : " + $dataDir.FullName)
    $managed = Join-Path $dataDir.FullName 'Managed'
    if (Test-Path $managed) {
        Log ("MANAGED  : " + $managed)
        Get-ChildItem $managed -Filter '*.dll' |
            Sort-Object Length -Descending | Select-Object -First 15 |
            ForEach-Object { Log ("   " + $_.Name.PadRight(42) + [math]::Round($_.Length/1KB) + " KB") }
    }
    $il2 = Join-Path $dataDir.FullName 'il2cpp_data'
    if (Test-Path $il2) { Log ("IL2CPP DATA dir present: " + $il2) }
}

# ---------- 3. unity version ----------
Head "3. Unity build"
$up = Join-Path $gameDir 'UnityPlayer.dll'
if (Test-Path $up) {
    $vi = (Get-Item $up).VersionInfo
    Log ("UnityPlayer.dll FileVersion : " + $vi.FileVersion)
    Log ("UnityPlayer.dll ProductVer  : " + $vi.ProductVersion)
}
if ($dataDir) {
    $ggm = Join-Path $dataDir.FullName 'globalgamemanagers'
    if (Test-Path $ggm) {
        $bytes = [System.IO.File]::ReadAllBytes($ggm)
        $txt = [System.Text.Encoding]::ASCII.GetString($bytes)
        $vm = [regex]::Match($txt, '\b(\d{4}\.\d+\.\d+[abfp]\d+)\b')
        if ($vm.Success) { Log ("globalgamemanagers version  : " + $vm.Groups[1].Value) }
    }
}
Log "  (web says 6000.3.15f1 -> you need a matching Unity Editor to rebuild AssetBundles)"

# ---------- 4. middleware / interesting dlls ----------
Head "4. middleware"
Get-ChildItem $gameDir -Recurse -Depth 2 -Filter '*.dll' |
    Where-Object { $_.Name -match 'fmod|resonance|playmaker|powersprite|steam_api|EOSSDK' } |
    ForEach-Object { Log ("  " + $_.FullName.Replace($gameDir, '.')) }

# ---------- 5. executables ----------
Head "5. executables"
Get-ChildItem $gameDir -Filter '*.exe' | ForEach-Object {
    Log ("  " + $_.Name + "   " + [math]::Round($_.Length/1MB, 1) + " MB")
}

# ---------- 6. existing mod loader ----------
Head "6. existing mod loader"
$bep = Join-Path $gameDir 'BepInEx'
if (Test-Path $bep) {
    Log "BepInEx : INSTALLED"
    Log ("  version file : " + (Get-ChildItem $bep -Filter '*version*' -Recurse -Depth 1 | Select-Object -First 1).FullName)
} else {
    Log "BepInEx : not installed"
}
foreach ($f in @('doorstop_config.ini', 'winhttp.dll', 'version.dll', 'changelog.txt')) {
    if (Test-Path (Join-Path $gameDir $f)) { Log ("  found: " + $f) }
}
$plugins = Join-Path $bep 'plugins'
if (Test-Path $plugins) {
    Log "  plugins:"
    Get-ChildItem $plugins -Recurse | ForEach-Object { Log ("    " + $_.FullName.Replace($gameDir, '.')) }
}
$logf = Join-Path $bep 'LogOutput.log'
if (Test-Path $logf) {
    Log "  --- last BepInEx log tail ---"
    Get-Content $logf -Tail 12 | ForEach-Object { Log ("    " + $_) }
}

# ---------- 7. save / config dirs ----------
Head "7. save + config locations"
$low = Join-Path $env:USERPROFILE 'AppData\LocalLow'
if (Test-Path $low) {
    Get-ChildItem $low -Directory | ForEach-Object {
        $company = $_
        if ($company.Name -match 'silver|pine|carpenter|wych') {
            Log ("  LocalLow match: " + $company.FullName)
            foreach ($f in (Get-ChildItem $company.FullName -Recurse -Depth 2 | Select-Object -First 25)) {
                Log ("     " + $f.FullName.Substring($company.FullName.Length))
            }
        }
        foreach ($s in (Get-ChildItem $company.FullName -Directory)) {
            if ($s.Name -match 'silver|pine|carpenter|wych') {
                Log ("  LocalLow match: " + $s.FullName)
                foreach ($f in (Get-ChildItem $s.FullName -Recurse -Depth 2 | Select-Object -First 25)) {
                    Log ("     " + $f.FullName.Substring($s.FullName.Length))
                }
            }
        }
    }
}
$gameData = Join-Path $env:LOCALAPPDATA $AppId
if (Test-Path $gameData) { Log ("  LocalAppData appid dir: " + $gameData) }

Head "8. registry (PlayerPrefs + company/product names)"
foreach ($rk in @("HKCU:\Software")) {
    Get-ChildItem $rk | Where-Object { $_.PSChildName -match 'silver|pine|carpenter|wych|team17' } | ForEach-Object {
        Log ("  " + $_.Name)
        Get-ItemProperty $_.PSPath | Format-List | Out-String | ForEach-Object { $_.Split("`n") } |
            Where-Object { $_ -match ':' } | Select-Object -First 20 | ForEach-Object { Log ("     " + $_.Trim()) }
    }
}

# ---------- 9. summary ----------
Head "9. SUMMARY / NEXT STEP"
if ($isIl2cpp) {
    Log "-> IL2CPP path. Grab BepInEx 6 bleeding-edge IL2CPP build (win-x64),"
    Log "   it generates Il2CppInterop assemblies on first launch (takes 1-3 min, needs net)."
    Log "   Then follow 银松镇Mod开发路线.md"
} elseif ($hasMonoManaged) {
    Log "-> MONO path. Easiest case. BepInEx 5.4.x x64 works immediately."
    Log "   Open *_Data\Managed\Assembly-CSharp.dll in ILSpy and read the combat code."
} else {
    Log "-> undetermined, paste this whole report back to the assistant."
}

$reportPath = Join-Path $PSScriptRoot 'recon-result.txt'
$Out | Out-File -FilePath $reportPath -Encoding utf8
Write-Host ""
Write-Host ("report written to: " + $reportPath)
Write-Host "paste that file back into the chat and we pick the exact toolchain."
