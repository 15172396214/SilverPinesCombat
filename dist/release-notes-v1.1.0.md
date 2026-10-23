## v1.1.0

### 新增 / New

- **背包扩容**：面板「背包」页调额外行数（0–10），格子会跟着长出来，已买的扩容道具照常叠加。
  缩容前会检查额外行里有没有物品，有就拒绝 —— 越界物品会让背包菜单卡死、所有按键（含 ESC）失效，只能大退。
  Expandable inventory: set extra rows (0–10) from the panel. Shrinking is refused while the extra rows still hold items, because an out-of-bounds item can hard-lock the inventory menu.
- **瞄准时移动不再带偏方向**：手柄左摇杆、键鼠 A/D 都不再改变瞄准角度，右摇杆/鼠标仍能正常转身。
  Movement input no longer changes your aim angle — with a controller or with WASD. The right stick or mouse still turns you normally.
- **显示闪避无敌帧**（默认关）：无敌期间角色整体变亮青色，能直观看出无敌窗口有多长。
  Visible dodge i-frames (off by default): the character tints bright cyan while invulnerable.

### 安装 / Install

- 已装 BepInEx 6（be.789+）→ 下载 `SilverPinesCombat-1.1.0.zip`（57 KB）
- 全新安装 → 下载 `SilverPinesCombat-1.1.0-full.zip`（34.4 MB，含运行时）

⚠️ 解压别多套一层：`winhttp.dll` 必须和 `Carpenter.exe` 同级。首次启动会卡 1–3 分钟（BepInEx 生成 interop 程序集），属正常现象。

### 移除 / Removed

- 未完成的辅助瞄准实现与其调试探针（hook 落点未打通，后续版本再上）。
  The unfinished aim-assist implementation and its debug probe — the hook point wasn't reliable yet.

### 说明

- 游戏本体对闪避无敌帧**没有任何视觉反馈**，这是新增的显示，不是修 bug
- 配置项：`[Bag] ExtraRows`、`[Aim] GamepadLockAimFromMovement`、`[Visual] IFrameBlueTint`
