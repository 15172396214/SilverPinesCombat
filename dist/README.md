# Silver Pines Combat v1.1.0

[English](README.en.md) | [中文](README.md)

给《银松镇 / Silver Pines》做的战斗手感改造 mod。

界面支持中英双语：点面板页签行最右边的 **EN / 中文** 按钮即时切换，也可以在配置里写死 `Language = zh | en | auto`。

装上之后最直观的几件事：

- **背包能扩容**：面板里调额外行数（0–10），格子会跟着长出来，已买的扩容道具照常叠加；缩容前会检查额外行里有没有物品，有就拒绝（避免物品卡在容量外把游戏搞死）
- **瞄准时移动不再带偏方向**：手柄左摇杆、键鼠 A/D 都不会再改变瞄准角度，右摇杆/鼠标仍能正常转身
- **闪避无敌帧看得见**（默认关）：开启后无敌期间角色整体变亮青色，能直观看出无敌窗口有多长
- **瞄准多一条枪口细线**：从枪管画一条灰色细线，延到固定长度就断，只在真正瞄准时出现
- **攻击中途能闪避**：原版只有起手瞬间和判定结束后能取消，现在整段攻击都能取消
- **踩踏先冲一步**：按下踩踏时自动向前踏一小段，贴到刚好踩得到的位置，不再原地踩空

---

## 一、安装

### 情况 A：你什么都没有（推荐）

下载 **`SilverPinesCombat-1.0.0-full.zip`**，整个解压到游戏根目录。

游戏根目录在哪：Steam → 库 → 右键《Silver Pines》→ 管理 → 浏览本地文件。

⚠️ **别用「解压到 SilverPinesCombat-1.0.0-full\」**。Windows 资源管理器的「全部解压缩」默认会多套一层子文件夹
（路径变成 `...\Silver Pines\SilverPinesCombat-1.0.0-full\`），这样 `winhttp.dll` 就深了一层，
**游戏能正常启动、但 mod 完全不加载**，而且没有任何报错——最难查的那种。解压时把目标路径末尾那段
文件夹名删掉，让它正好等于 `Silver Pines`。

解压完应该长这样：

```
Silver Pines\
├── Carpenter.exe
├── winhttp.dll              ← 框架注入入口（新增）
├── doorstop_config.ini      ← 新增
├── .doorstop_version        ← 新增
├── dotnet\                  ← 新增
└── BepInEx\
    ├── core\
    ├── interop\             ← 首次启动后才会生成
    └── plugins\
        └── SilverPinesCombat\
            ├── SilverPinesCombat.dll   ← 本 mod
            ├── README.md / README.en.md
            └── THIRD-PARTY-NOTICES.txt
```

文档都放在插件自己的文件夹里，不会往游戏根目录乱扔文件——所以卸载就是删这一个文件夹。

### 情况 B：你已经装过 BepInEx

把压缩包里的 `BepInEx` 文件夹**合并**进游戏根目录（覆盖同名文件夹即可，不会覆盖你的其他 mod）。

### 首次启动会卡 1–3 分钟

第一次进游戏时，BepInEx 要把游戏的 IL2CPP 程序集翻译成托管代理（生成 `BepInEx\interop\`，约 180 个 DLL）。**这段时间看起来像卡死，但没死，别关。** 第二次启动就正常了。

> 顺带：这是 BepInEx 本身的行为，不是本 mod 干的。

---

## 二、前置要求（情况 B 必看）

| 项 | 要求 |
|---|---|
| 游戏 | 《银松镇 / Silver Pines》（Steam，AppID 2333000） |
| 框架 | **BepInEx 6.0.0-bleeding-edge（be.789 或更新），IL2CPP，win-x64** |
| ❌ 不要用 | `BepInEx 6.0.0-pre.X`、BepInEx 5.x |

**版本不对的典型报错**：

```
Unsupported metadata version found! We support 23-29, got 39
```

游戏用的是 Unity 6，metadata 版本 **39**。老的 BepInEx 只认到 29。看到这行就是版本错了，换 Bleeding Edge 构建。

---

## 三、热键

| 键 | 功能 |
|---|---|
| **F7** | 显示 / 隐藏面板 |
| F1 / F2 / F3 / F4 / F5 | 无敌 / 无限弹药 / 全程弱点命中 / 无限体力 / AI 失明 |
| F6 | 慢动作开关 |

面板有三个页签：**战斗 / 瞄准 / 作弊**。

> 说明：这一版把训练场、刷怪、强制换弹、调试控制台这些**没验证过 / 有风险**的东西全删了，
> 只留实测确认有效的功能。F8–F12、H、数字键 9 已不再绑定任何行为。

---

## 四、配置

配置文件在：

```
BepInEx\config\silverpines.combat.cfg
```

改完保存，重启游戏生效；也可以在面板里直接调（大部分滑块即时生效）。

开箱默认是**"手感改造模式"**——三个主功能都开着，作弊全关：

```ini
[Combat.Dodge]  CancelAttack = true     ; 闪避取消攻击
[Combat.Stomp]  LungeEnabled = true     ; 踩踏前踏吸附
[AimLine]       Enabled      = true     ; 瞄准辅助线
[Cheats]        Invincible   = false    ; 作弊默认全关
[Debug]         DrawDamageColliders = false  ; 判定框默认关
[General]       Language     = auto     ; 界面语言 auto(跟随系统) | zh | en
```

面板页签行最右边的按钮也能切语言，点了会写回这个配置。

只想用其中一两个，就在面板里把其余的关掉。

**踩踏吸附的几个参数**（不想动手感就保持默认）：

| 键 | 默认 | 说明 |
|---|---|---|
| `LungeRange` | 1.5 | 吸附搜索半径（米）。敌人在这个距离内才触发前踏 |
| `StopDistance` | 0.10 | 停留距离**上限**，实际会按敌人碰撞体宽度自适应缩小 |
| `LungeDuration` | 0.15 | 位移时长（秒）。0 = 瞬移（很怪），0.12~0.20 是冲刺手感 |
| `FrontOnly` | — | 已移除（拿不到可靠的朝向数据） |

**瞄准线只在瞄准时出现**（写死，不是开关）——不瞄准时枪口姿态不更新，线会僵在原地变成一条挡视线的白线。

| 键 | 默认 | 说明 |
|---|---|---|
| `Length` | 2.0 | 线长（米） |
| `Width` | 0.007 | 线宽（米），越小越细 |
| `Gray` | 0.75 | 灰度，0 黑 1 白 |
| `ShowDelay` | 0.15 | 抬枪后**最多**等多久就画线（秒）。实际是「枪口连续静止 0.08s」先满足就先画，这个值只是兜底上限 |

---

## 五、卸载

删掉这个文件夹就干净了：

```
BepInEx\plugins\SilverPinesCombat\
```

如果你装的是 `-full` 包，并且不想保留 BepInEx，把这 5 项一起删掉即可（**没有任何原游戏文件被修改过**）：

```
winhttp.dll
doorstop_config.ini
.doorstop_version
BepInEx\
dotnet\
```

---

## 六、接下来要做的功能

下面这些**当前版本一个都没有**，别指望装上就有。列出来是说明方向，不是待办承诺：

| 功能 | 卡在哪 / 打算怎么做 |
|---|---|
| **辅助瞄准（准星吸附）** | 下一个就做它。现在只画了一条指示线，没做任何弹道修正。计划是开火瞬间在角度阈值内把射向掰到最近目标上，吸附强度做成滑块 |
| **霰弹枪打完自动上膛** | pump 参数在 ScriptableObject 里，运行时改不动。得换路子：开火后直接推进武器的动画状态机，让上膛动作并入后摇 |
| **换弹不用先瞄准** | 硬调 `StartReload` 会把主线程卡死。得先找到游戏自己那道「必须先瞄准」的输入闸门，放行而不是绕开 |
| **移动中也能踩踏** | 限制在攻击状态机里，不是输入层放不放行的问题。要把移动状态接进踩踏的前置条件 |

v1.0.0 只包含开头那三件：闪避取消、踩踏前踏、瞄准指示线。

---

## 七、兼容性

游戏本体更新后，这个 mod **有可能失效**。原因不是懒，是 IL2CPP 的性质：每次游戏打补丁，`global-metadata.dat` 的版本号和方法地址都可能变。

失效的表现：

- 面板还在但功能没反应 → 方法被改名/重排
- 启动就报错 → metadata 版本变了，需要重新适配

看日志确认：

```
BepInEx\LogOutput.log
```

搜 `[SPC]`，正常应该有：

```
[SPC] loaded v1.0.0 (be.789 / IL2CPP) lang=zh
[SPC] diagnose: 7/7 fields resolved
```

不是 7/7 就说明字段解析失败，功能会部分失效。

---

## 八、许可与致谢

- 本 mod 代码：MIT
- **BepInEx**（`-full` 包内含）：LGPL-2.1，源码见 https://github.com/BepInEx/BepInEx
- 本 mod 不含任何游戏原文件，也不修改游戏本体。所有改动都在运行时内存中完成，删掉插件即恢复原版

致谢：游戏的调试系统（`GameDebugCommands`）写得比不少商业项目还全，这个 mod 的一半功能是站在它的肩膀上做的。

---

## 附：这 mod 是怎么做出来的（给好奇的人）

游戏是 Unity 6 + IL2CPP，`global-metadata.dat` 版本 39。官方 Il2CppDumper 不支持这个版本，是从上游未合并的 PR 分支自己编译出来才拿到类型信息的。

所有功能都通过 BepInEx + Harmony 在运行时完成：

- 静态作弊开关：原生 `il2cpp_field_static_set_value`
- 方法拦截：Harmony `Prefix` / `Postfix`（IL2CPP 下没有 IL，`Transpiler` 不可用）
- 反射读游戏类成员：必须用指针把代理**重包装成真实类型**，否则 `TargetException` 被吞掉，表现为"字段不存在"
