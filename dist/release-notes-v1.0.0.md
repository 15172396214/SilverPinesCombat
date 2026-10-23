# Silver Pines Combat v1.0.0

给《银松镇 / Silver Pines》（Steam AppID 2333000）做的战斗手感改造 mod。
Unity 6 + IL2CPP，基于 BepInEx 6 Bleeding Edge + Harmony，**全部改动在运行时内存里完成，不碰任何游戏文件**。

Combat feel overhaul for *Silver Pines* (Steam AppID 2333000).
Unity 6 + IL2CPP, built on BepInEx 6 Bleeding Edge + Harmony. **Everything happens in memory at runtime — no game file is ever modified.**

---

## 下载 / Download

| 文件 / File | 体积 | 内容 |
|---|---|---|
| `SilverPinesCombat-1.0.0-full.zip` | **32.8 MB** | 插件 + BepInEx 6 be.789 全套。什么都不装，解压到游戏根目录就能用 |
| `SilverPinesCombat-1.0.0.zip` | **45 KB** | 只有插件 DLL，适合已经装过 BepInEx 6 IL2CPP（be.789 或更新）的人 |

安装步骤见包内 `BepInEx\plugins\SilverPinesCombat\README.md`（中文）/ `README.en.md`（English）。

⚠️ 解压时别让资源管理器多套一层目录（默认会解到 `Silver Pines\SilverPinesCombat-1.0.0-full\`）。
`winhttp.dll` 必须在 `Silver Pines\` 这一层，否则游戏能正常启动但 mod 完全不加载、且没有任何报错。

> `-full` 包里不含 `BepInEx\unity-libs\` —— 那是首次启动时 BepInEx 自己生成的缓存（约 11 MB）。
> 打进压缩包会被 Nexus 的自动扫描判定成「嵌套压缩包」而隔离整个文件。

> ⚠️ 整合包里的 BepInEx 是 **6.0.0-bleeding-edge (be.789)**。如果你装的是 BepInEx 5 或更老的 6，会报
> `Unsupported metadata version found! We support 23-29, got 39` —— 本游戏用的是 metadata v39，老版本读不了。

---

## 功能 / Features

### 1. 闪避取消攻击 / Dodge cancels attacks

原版只有起手瞬间和判定结束后能取消 —— 中间那段硬直里按闪避没反应。
Patch `InputDodge.m_canBeAnimationBlocked` 之后，整段攻击动画随时能被闪避打断。

对照实验：原版在攻击中段的硬直里按闪避无响应；patch 后同一时间点按闪避立刻翻滚。

### 2. 踩踏前踏吸附 / Stomp snaps forward

原版原地踩踏，怪物站在边缘就踩空。现在按下踩踏会先向前匀速踏一小段（0.15 s），
停在与目标碰撞体宽度自适应的距离上，确保判定命中。

三个参数可调：吸附半径（默认 1.5 m）、停留距离（默认 0.10 m）、位移时长（默认 0.15 s）。

### 3. 瞄准指示线 / Aim indicator line

从枪口 `ProjectileWeapon.m_muzzle` 画一条灰色细线（默认长 2.0 m / 宽 0.007 m / 灰度 0.75）。

- **只在真正瞄准时出现** —— `AimingActive && !AimBlocked`，翻滚中不会挂着一条线
- 抬枪后等枪口姿态稳定才显示（默认 0.15 s），避免抬起过程闪一下旧位置的线
- 线长 / 线宽 / 灰度 / 延迟都能在面板里拖

### 其他 / Also included

- F1–F5 单项作弊（无敌 / 无限弹药 / 全程弱点命中 / 无限体力 / AI 失明），默认全关
- F6 慢动作、判定框绘制（默认关）
- **F7** 显隐面板；面板支持中英双语，右上角 `EN / 中文` 一键切换

---

## 热键 / Hotkeys

| 键 | 功能 |
|---|---|
| **F7** | 显示 / 隐藏面板 |
| F1 / F2 / F3 / F4 / F5 | 无敌 / 无限弹药 / 全程弱点命中 / 无限体力 / AI 失明 |
| F6 | 慢动作开关 |

---

## 已知情况 / Known behaviour

- 踩踏吸附选的是**最近的目标**，不分前后。原计划只吸正前方，但游戏的朝向字段拿不到可靠值，方案已放弃。
- 首次启动 BepInEx 会生成 `BepInEx\interop\`（约 110 MB），耗时 1–3 分钟，期间游戏窗口无响应属正常。

---

## 下一步 / What's next

**当前版本没有**下面任何一项，列出来是说明方向：

- **辅助瞄准（准星吸附）** —— 下一个做。现在只是画线，没有任何弹道修正
- 霰弹枪打完自动上膛 —— pump 参数在 ScriptableObject 里，运行时改不动
- 换弹不用先瞄准 —— 硬调 `StartReload` 会死锁主线程
- 移动中也能踩踏 —— 限制在攻击状态机里，不是输入层放不放行的问题

---

## 卸载 / Uninstall

删掉 `BepInEx\plugins\SilverPinesCombat\` 即可。没有动过任何其他文件。

## 许可 / License

Mod 代码 MIT。整合包里捆绑的 BepInEx 是 LGPL-2.1，见 `THIRD-PARTY-NOTICES.txt`。
仓库不含任何游戏原文件。
