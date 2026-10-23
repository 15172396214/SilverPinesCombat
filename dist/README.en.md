# Silver Pines Combat v1.0.0

[English](README.en.md) | [中文](README.md)

Combat feel overhaul for *Silver Pines* (Steam AppID 2333000). Everything happens at runtime — no game file is modified.

Three things you'll notice right away:

- **A thin muzzle line while aiming** — grey, fixed length, only visible when you actually aim
- **Dodge cancels attacks** — vanilla only lets you cancel during startup and after the hitbox closes; now the whole attack is cancellable
- **Stomp steps forward first** — press stomp and the character closes the gap to exactly stomping range instead of whiffing in place

UI language: **中文 / English** — click the button at the right end of the tab bar, or set `[General] Language = zh | en | auto` in the config.

---

## 1. Install

### Case A — you have nothing (recommended)

Grab **`SilverPinesCombat-1.0.0-full.zip`** and extract the whole thing into the game root.

Game root: Steam → Library → right-click *Silver Pines* → Manage → Browse local files.

⚠️ **Do not use "Extract to SilverPinesCombat-1.0.0-full\"**. Windows Explorer's "Extract All" pre-fills the
destination with an extra subfolder (`...\Silver Pines\SilverPinesCombat-1.0.0-full\`). That puts `winhttp.dll`
one level too deep: **the game starts fine and the mod silently never loads**, with no error at all — the worst
kind of bug to trace. Trim the trailing folder name off the path so it ends exactly at `Silver Pines`.

After extraction:

```
Silver Pines\
├── Carpenter.exe
├── winhttp.dll              <- injection entry (new)
├── doorstop_config.ini      <- new
├── .doorstop_version        <- new
├── dotnet\                  <- new
└── BepInEx\
    ├── core\
    ├── interop\             <- generated on first launch
    └── plugins\
        └── SilverPinesCombat\
            ├── SilverPinesCombat.dll   <- this mod
            ├── README.md / README.en.md
            └── THIRD-PARTY-NOTICES.txt
```

Every document lives inside the plugin's own folder, so nothing is dropped loose into the game root —
uninstalling means deleting that one folder.

### Case B — you already have BepInEx

**Merge** the `BepInEx` folder from the archive into the game root (overwrite same-named folders; your other mods are untouched).

### First launch hangs for 1–3 minutes

BepInEx has to translate the game's IL2CPP assemblies into managed proxies (~180 DLLs under `BepInEx\interop\`). **It looks frozen but it isn't — don't kill it.** Later launches are normal.

---

## 2. Requirements (read this in case B)

| Item | Requirement |
|---|---|
| Game | *Silver Pines* (Steam, AppID 2333000) |
| Framework | **BepInEx 6.0.0-bleeding-edge (be.789 or newer), IL2CPP, win-x64** |
| ❌ Don't use | `BepInEx 6.0.0-pre.X`, BepInEx 5.x |

Typical error when the version is wrong:

```
Unsupported metadata version found! We support 23-29, got 39
```

The game runs Unity 6 with metadata version **39**; older BepInEx only understands up to 29. Use a Bleeding Edge build.

---

## 3. Hotkeys

| Key | Function |
|---|---|
| **F7** | Show / hide the panel |
| F1 / F2 / F3 / F4 / F5 | Invincible / infinite ammo / always weakpoint / infinite stamina / AI oblivious |
| F6 | Toggle slow motion |

The panel has three tabs: **Combat / Aim / Cheats**.

> The training room, enemy spawner, forced reload and debug console were all **removed** in this release — they were unverified or risky. F8–F12, H and digit 9 are no longer bound to anything.

---

## 4. Config

```
BepInEx\config\silverpines.combat.cfg
```

Save and restart, or just drag the sliders in the panel (most take effect immediately).

Out of the box it's in **"combat feel" mode** — the three main features on, all cheats off:

```ini
[Combat.Dodge]  CancelAttack = true     ; dodge cancels attacks
[Combat.Stomp]  LungeEnabled = true     ; stomp snap
[AimLine]       Enabled      = true     ; aim line
[Cheats]        Invincible   = false    ; all cheats off by default
[Debug]         DrawDamageColliders = false  ; hitbox drawing off
[General]       Language = auto         ; auto | zh | en
```

Want only one or two of them? Turn the rest off in the panel.

**Stomp snap parameters** (leave them alone if you don't want to tinker):

| Key | Default | Meaning |
|---|---|---|
| `LungeRange` | 1.5 | Search radius in metres. Enemies farther than this don't trigger the lunge |
| `StopDistance` | 0.10 | **Upper bound** on the gap left after the lunge; the real value shrinks to fit the enemy collider |
| `LungeDuration` | 0.15 | Lunge duration in seconds. 0 = teleport (looks wrong), 0.12–0.20 reads as a dash |

**The aim line is hardcoded to aiming only** (not a toggle) — outside of aiming the muzzle pose doesn't update, so the line would freeze in place and block your view.

| Key | Default | Meaning |
|---|---|---|
| `Length` | 2.0 | Line length in metres |
| `Width` | 0.007 | Line width; smaller is thinner |
| `Gray` | 0.75 | 0 = black, 1 = white |
| `ShowDelay` | 0.15 | **Maximum** wait after raising the gun, in seconds. In practice the line appears as soon as the muzzle has been still for 0.08 s; this is only the fallback ceiling |

---

## 5. Uninstall

Delete this folder and you're clean:

```
BepInEx\plugins\SilverPinesCombat\
```

If you installed the `-full` archive and don't want to keep BepInEx, remove these five as well — **no original game file was ever modified**:

```
winhttp.dll
doorstop_config.ini
.doorstop_version
BepInEx\
dotnet\
```

---

## 6. What's next

**None of the following is in this release** — don't install it expecting any of it. This is a direction, not a promise:

| Feature | Where it stands |
|---|---|
| **Aim assist (soft lock)** | Next up. Right now the mod only draws an indicator line; no trajectory correction at all. Plan: at the moment of firing, bend the shot toward the nearest target within an angle threshold, with a strength slider |
| **Auto pump-action for the shotgun** | The pump parameter lives in a ScriptableObject and can't be touched at runtime. Different route needed: advance the weapon's animation state machine after firing so the pump folds into the recovery frames |
| **Reload without aiming first** | Calling `StartReload` directly deadlocks the main thread. Need to find the game's own "must be aiming" input gate and let it through, not bypass it |
| **Stomp while moving** | The restriction sits in the attack state machine, so it isn't an input-layer allow/deny problem. The move state has to be wired into the stomp precondition |

v1.0.0 ships only the three features listed at the top: dodge cancel, stomp snap, aim line.

---

## 7. Compatibility

A game update **may break this mod**. That's the nature of IL2CPP — every patch can move method addresses and bump the metadata version.

How it breaks:

- Panel shows but features do nothing → a method was renamed or reordered
- Crash on startup → metadata version changed, needs re-adapting

Check the log:

```
BepInEx\LogOutput.log
```

Search for `[SPC]`. A healthy start looks like:

```
[SPC] loaded v1.0.0 (be.789 / IL2CPP) lang=zh
[SPC] diagnose: 7/7 fields resolved
```

Anything other than 7/7 means field resolution failed and parts of the mod won't work.

---

## 8. License & credits

- This mod's code: MIT
- **BepInEx** (bundled in `-full`): LGPL-2.1, source at https://github.com/BepInEx/BepInEx
- This mod ships no game files and modifies nothing on disk. Everything is patched in memory; delete the plugin and you're back to vanilla.

Credits: the game's own debug system (`GameDebugCommands`) is more complete than a lot of shipped tooling — half of this mod stands on it.

---

## Appendix: how this was built (for the curious)

The game is Unity 6 + IL2CPP with `global-metadata.dat` version 39. The official Il2CppDumper doesn't support that version, so the type information came from compiling an unmerged upstream PR branch by hand.

Everything runs through BepInEx + Harmony at runtime:

- Static cheat flags: native `il2cpp_field_static_set_value`
- Method interception: Harmony `Prefix` / `Postfix` (there is no IL under IL2CPP, so `Transpiler` is unavailable)
- Reading game class members by reflection: you must **re-wrap the proxy into its real type** via its pointer, otherwise `TargetException` gets swallowed and it just looks like "field doesn't exist"
