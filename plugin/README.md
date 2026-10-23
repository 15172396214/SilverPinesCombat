# Silver Pines Combat — 构建与使用

给《银松镇》(Silver Pines) 做的训练/调试插件。**核心不是"加功能"，是把游戏内建的作弊开关接到热键上。**

---

## 一、为什么这么做

反编译 `global-metadata.dat` 后发现游戏自带这些（全部 `public static bool`）：

```csharp
GameDebugCommands.CHEAT_INVINCIBLE          // 无敌
GameDebugCommands.CHEAT_INFINITE_AMMO       // 无限弹药
GameDebugCommands.CHEAT_INFINITE_STAMINA    // 无限体力
GameDebugCommands.ALWAYS_WEAKPOINT_HIT      // 所有命中算弱点
GameDebugDrawer.DRAW_DAMAGE_COLLIDERS       // 绘制判定框
AISenses.s_aiOblivious                      // AI 失明
```

**不需要 Harmony，不需要造场景，赋值就生效。**

---

## 二、前置：装 BepInEx 6 Bleeding Edge

⚠️ **必须用 Bleeding Edge（`be.XXX`），不能用 `6.0.0-pre.X`。**
Unity 6 的 metadata 是 **v39**，pre 版只支持到 v29，会直接报错退出：

```
Unsupported metadata version found! We support 23-29, got 39
```

1. 去 BepisBuilds 下载**最新的** `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.XXX`
2. 解压到游戏根目录（和 `Carpenter.exe` 同级）
3. **启动游戏一次**，等 1–3 分钟生成 `BepInEx/interop/*.dll`（需要联网，别以为卡死）
4. 确认 `BepInEx/LogOutput.log` 有内容

兜底：如果 BepInEx 走不通，试 **MelonLoader Nightly**（2025 年底社区反馈它在 Unity 新版 IL2CPP 上支持一度更好）。

---

## 三、先试这个（0 代码）

**游戏编译进了 `DebugConsole.dll`。** 先花 10 分钟试：

- 游戏中按 `F1` / `` ` `` / `~` / `F12`
- 或加启动参数：Steam → 属性 → 启动选项填 `-debug`
- 或 `--console`

如果能开出来，你就有 80+ 条命令（`invincible` / `spawn` / `timescale` / `draw_damage_colliders` / `gameunlocks_all` …），**本插件都可以不写**。

控制台静态入口（如果要用代码开）：

```csharp
LevelManager.LoadDebugConsole();     // public static
LevelManager.LoadUtilityScenes();    // public static
// 闸门：DebugConsole.m_debugConsoleAllowed (private static bool)
```

---

## 四、构建插件

工程支持**两种构建模式**，自动切换：

| 模式 | 触发条件 | 用途 |
|---|---|---|
| **正式** | `BepInEx/interop/*.dll` 存在 | 产出可运行的插件，自动拷进 `BepInEx/plugins/` |
| **校验** | interop 未生成 | 用 Il2CppDumper 的 DummyDll 冒充 UnityEngine，只做**语法/API 编译校验**，`AddComponent` 被条件编译跳过 |

```powershell
cd plugin
dotnet build -c Release
```

**当前状态**：校验模式已通过（0 警告 0 错误）。
正式模式需要先启动一次游戏生成 interop。

### 已验证的 API 签名（踩坑记录）

| 调用 | 实际签名 | 说明 |
|---|---|---|
| `il2cpp_domain_assembly_open` | `(IntPtr domain, **IntPtr name**)` | 不是 `string`，要用 `Marshal.StringToHGlobalAnsi` |
| `il2cpp_field_static_set_value` | `(IntPtr field, **void\*** value)` | 是裸指针，需 `unsafe` |
| `il2cpp_class_from_name` | `(IntPtr image, string ns, string name)` | 这个确实是 string |
| `BasePlugin.AddComponent<T>` | 约束 `T : Il2CppObjectBase` | 无 `AddComponent(Type)` 重载 |

---

## 五、热键

| 键 | 功能 |
|---|---|
| `F1` | 无敌 |
| `F2` | 无限弹药 |
| `F3` | 全程弱点命中（不用瞄头） |
| `F4` | 无限体力 |
| `F5` | AI 失明 |
| `F6` | 慢动作开关 |
| `F7` | 显示/隐藏面板 |

配置在 `BepInEx\config\silverpines.combat.cfg`，改完即时生效。

---

## 六、⚠️ 已知风险 / 首次构建可能要改的地方

这段代码**没有编译验证过**（写的时候 BepInEx 还没装）。第一次 build 大概率先遇到下面某一项：

| 症状 | 原因 | 改法 |
|---|---|---|
| 找不到 `BepInEx.Unity.IL2CPP` | 命名空间不对 | 看 `BepInEx\core\` 里实际 DLL 名，调整 `using` |
| `BasePlugin` 不存在 | 版本差异 | BepInEx 6 IL2CPP 用 `BasePlugin`；若拿到的是 Mono 版则改 `BaseUnityPlugin` + `Awake()` |
| `AddComponent<T>` 不可用 | 需要 `MonoBehaviour` 派生自 interop 类型 | 改 `Il2CppInterop` 的写法，或用 `new GameObject().AddComponent<CombatBehaviour>()` |
| 反射找不到 `GameDebugCommands` | interop 程序集名不同 | 打开 `BepInEx\interop\` 看有没有 `Assembly-CSharp.dll`；日志会打印 `type not found` |
| 字段赋值无效 | IL2CPP 静态字段需特殊处理 | 改用直接引用（见 `Plugin.cs` 末尾注释） |

**先看 `BepInEx\LogOutput.log`**，插件每步都有日志。

---

## 七、下一步（真正的 mod 候选）

按价值排：

1. **鼠标辅助瞄准** —— `CharacterAiming.ApplyAimAssist` 只挂手柄通路，鼠标走 `MouseAimInputData`。Postfix `UpdateAim`，从 `m_aimTargetsSet` 取目标做吸附。
2. **后摇缩短** —— Postfix `Actions.PlaySpriteAnimation`，攻击类 clip 提速。
3. **输入缓冲** —— 看 `BaseCharacterInput` / `CharacterInputPlayer`，加输入队列。

**但要先测**：`战斗手感实测记录表.md` 第 2 条（挥砍中反复按攻击键，看收招后是否自动接刀）。
