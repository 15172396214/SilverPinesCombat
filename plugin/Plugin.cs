// Silver Pines 战斗性能优化 — BepInEx 6 (IL2CPP be.789) 插件
// Target: Silver Pines / Unity 6000.3.15f1 / IL2CPP metadata v39
// 字段名来自 tools/dump/dump.cs（Il2CppDumper 反编译产物），非猜测。

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace SilverPinesCombat
{
    [BepInPlugin(GUID, "Silver Pines Combat", "1.1.0")]
    public class CombatPlugin : BasePlugin
    {
        public const string GUID = "silverpines.combat";

        public static CombatPlugin Inst;

        // ---------- 配置 ----------
        public static ConfigEntry<bool> CfgInvincible;
        public static ConfigEntry<bool> CfgAmmo;
        public static ConfigEntry<bool> CfgStamina;
        public static ConfigEntry<bool> CfgWeakpoint;
        public static ConfigEntry<bool> CfgDrawBoxes;
        public static ConfigEntry<bool> CfgAiBlind;
        public static ConfigEntry<float> CfgSlowMo;
        public static ConfigEntry<bool> CfgGpDiag;

        // ---------- 战斗改造 ----------
        public static ConfigEntry<bool> CfgDodgeCancel;
        public static ConfigEntry<bool> CfgStompLunge;
        public static ConfigEntry<float> CfgStompLungeRange;
        public static ConfigEntry<float> CfgStompStopDistance;
        public static ConfigEntry<float> CfgStompLungeDuration;

        // ---------- 瞄准辅助线 ----------
        public static ConfigEntry<bool> CfgAimLineEnabled;
        public static ConfigEntry<float> CfgAimLineLength;
        public static ConfigEntry<float> CfgAimLineWidth;
        public static ConfigEntry<float> CfgAimLineGray;
        public static ConfigEntry<float> CfgAimLineShowDelay;
        public static ConfigEntry<string> CfgLanguage;

        // ---------- 手柄：左摇杆不影响瞄准 ----------
        public static ConfigEntry<bool> CfgAimLockLeftStick;
        public static ConfigEntry<float> CfgAimLockDeadzone;

        // ---------- 背包 ----------
        public static ConfigEntry<int> CfgBagExtraRows;

        // ---------- 视觉 ----------
        public static ConfigEntry<bool> CfgIFrameTint;

        // ---------- 辅助瞄准 ----------

        public override void Load()
        {
            Inst = this;

            CfgInvincible = Config.Bind("Cheats", "Invincible", false, "玩家无敌 CHEAT_INVINCIBLE");
            CfgAmmo = Config.Bind("Cheats", "InfiniteAmmo", false, "无限弹药 CHEAT_INFINITE_AMMO");
            CfgStamina = Config.Bind("Cheats", "InfiniteStamina", false, "无限体力 CHEAT_INFINITE_STAMINA");
            CfgWeakpoint = Config.Bind("Cheats", "AlwaysWeakpoint", false, "所有命中算弱点 ALWAYS_WEAKPOINT_HIT");
            CfgDrawBoxes = Config.Bind("Debug", "DrawDamageColliders", false, "绘制判定框 DRAW_DAMAGE_COLLIDERS");
            CfgAiBlind = Config.Bind("Debug", "AiOblivious", false, "AI 失明 s_aiOblivious");
            CfgSlowMo = Config.Bind("Debug", "SlowMoScale", 0.35f, "慢动作倍率 (F6 切换)");
            CfgGpDiag = Config.Bind("Debug", "GamepadInputLog", false,
                "手柄输入诊断日志。每 0.2s 采样一次 CharacterInputPlayer 的移动/瞄准输入，" +
                "用来查清【左摇杆为什么会改变瞄准角度】。值没变化时不重复打印。排障用，平时请关掉。");
            // ===== 瞄准时移动不干扰方向（手柄 + 键鼠通用）=====
            CfgAimLockLeftStick = Config.Bind("Aim", "GamepadLockAimFromMovement", true,
                "瞄准时移动输入（左摇杆 / WASD）不再改变瞄准角度，也不再抢角色朝向。" +
                "★ 根因（实测）：这是 2D 精灵角色，朝向只有左/右两种、靠镜像翻转实现，而朝向由移动输入的左右符号决定。" +
                "所以 A/D 一翻面，瞄准方向就被水平镜像；手柄左右摇杆不同侧时，镜像后角度超出手臂可达范围，就卡在最高/最低位。" +
                "开启后：瞄准时把移动输入对齐到瞄准方向，朝向只跟右摇杆/鼠标走。");
            CfgAimLockDeadzone = Config.Bind("Aim", "GamepadAimDeadzone", 0.15f,
                "摇杆死区。摇杆偏移量小于这个值算没动。游戏自身也有死区，这里偏大一点更稳。");

            CfgLanguage = Config.Bind("General", "Language", "auto", "界面语言 / UI language: auto(跟随系统) | zh | en");

            // ===== 背包 =====
            CfgBagExtraRows = Config.Bind("Bag", "ExtraRows", 1,
                "背包额外行数（0-10）。运行时改 PlayerMainInventory.m_baseInventorySize，" +
                "游戏内 InventorySize = base + 已购扩容道具，所以不会覆盖你已经买的扩容。" +
                "UI 的格子是动态生成的，会自动跟着长。" +
                "⚠️ 缩容前先清空多出来的行，否则里面的物品可能丢失。");

            // ===== 视觉 =====
            CfgIFrameTint = Config.Bind("Visual", "IFrameBlueTint", false,
                "闪避无敌帧期间给角色叠一层浅蓝滤镜。默认开启。" +
                "★ 状态源：DamageBlock.m_damageBlockActive —— 闪避动画的帧事件调 EnableInvincibilityFrames() 开关它。" +
                "2D 精灵直接改 SpriteRenderer.color 做 tint，不换材质、不加 shader。");

            // ===== 战斗改造 =====
            CfgDodgeCancel = Config.Bind("Combat.Dodge", "CancelAttack", true,
                "闪避取消攻击。Patch InputDodge.m_canBeAnimationBlocked -> false。" +
                "★ 这是 InputDodge 上【唯一】存在的动画阻断开关，专为\"能不能在动作中途闪避\"设计。");

            // ===== 踩踏前踏吸附 =====
            CfgStompLunge = Config.Bind("Combat.Stomp", "LungeEnabled", true,
                "踩踏时向前吸附到刚好能踩到的位置。" +
                "按下踩踏的瞬间：找最近的敌人，把角色挪到它面前 stopDistance 处，然后放行踩踏。");
            CfgStompLungeRange = Config.Bind("Combat.Stomp", "LungeRange", 1.5f,
                "吸附搜索半径（米）。敌人在这个距离内才会触发前踏。超过就不动，避免隔空吸。");
            CfgStompStopDistance = Config.Bind("Combat.Stomp", "StopDistance", 0.10f,
                "前踏后与敌人保持的水平距离上限（米）。实际值会再按敌人碰撞体宽度自适应缩小，" +
                "所以这里填的是上限而不是固定值。0.35 太远会踩空，0.10~0.20 比较稳。");
            CfgStompLungeDuration = Config.Bind("Combat.Stomp", "LungeDuration", 0.15f,
                "前踏位移的持续时间（秒）。0 = 瞬移（会显得诡异）；0.12~0.20 是冲刺手感。" +
                "踩踏动画的前摇通常比这长，所以位移会在伤害帧之前完成。");

            // ===== 瞄准辅助线 =====
            CfgAimLineEnabled = Config.Bind("AimLine", "Enabled", true,
                "从枪口沿瞄准方向画一条线，延伸到指定距离就停。灰色。默认关。");
            CfgAimLineLength = Config.Bind("AimLine", "Length", 2.0f,
                "线长（米）。游戏自带 LaserBeam 也有 m_maxDistance，这个是我们的独立实现。");
            CfgAimLineWidth = Config.Bind("AimLine", "Width", 0.007f,
                "线宽（米）。0.007 左右是「细线」的手感。");
            CfgAimLineGray = Config.Bind("AimLine", "Gray", 0.75f,
                "灰度 0=黑 1=白。游戏自带的瞄准线是红色，这里是灰色。");
            CfgAimLineShowDelay = Config.Bind("AimLine", "ShowDelay", 0.15f,
                "开始瞄准后最多等多久就画线（秒）。实际是「等枪口连续静止 0.08s」先满足就先画，" +
                "这个值只是兜底上限 —— 调太大抬枪会明显延迟，0.15 基本感觉不到。");

            // SPT_DUMMY_VALIDATION：用 DummyDll 做编译校验时才定义
            // （DummyDll 的 MonoBehaviour 不派生 Il2CppObjectBase，泛型约束过不了）
            // ★ patch 必须放在最前面：后面任何一步（语言解析 / 组件挂载）出异常都不该拖垮功能改造。
            // 踩过的坑：一次 Config 绑定被误删，Load() 在语言那行抛 NRE，
            // 结果 Apply() 整个没跑 —— 面板还在、patch 全灭，表现为"功能静默失效"，极难排查。
            Diagnose();
            CombatPatches.Apply();

            L10n.Lang = ResolveLang(CfgLanguage?.Value);
            Log.LogInfo("[SPC] loaded v1.1.0 (be.789 / IL2CPP) lang=" + L10n.Lang);

#if !SPT_DUMMY_VALIDATION
            AddComponent<CombatBehaviour>();
            Log.LogInfo("[SPC] HUD attached");
#else
            Log.LogWarning("[SPC] DUMMY build - HUD not attached (compile-check only)");
#endif
        }

        private static string ResolveLang(string raw)
        {
            string s = (raw ?? "").Trim().ToLowerInvariant();
            if (s == "zh" || s == "cn" || s == "chinese") return "zh";
            if (s == "en" || s == "us" || s == "english") return "en";
            try
            {
                var sl = UnityEngine.Application.systemLanguage;
                if (sl == UnityEngine.SystemLanguage.Chinese ||
                    sl == UnityEngine.SystemLanguage.ChineseSimplified ||
                    sl == UnityEngine.SystemLanguage.ChineseTraditional) return "zh";
            }
            catch { return "zh"; }   // 读不到系统语言就回中文，别让界面突然变英文
            return "en";
        }

        // ================= 原生 IL2CPP 字段操作 =================

        private static readonly string[] CandidateAssemblies = { "CarpenterScripts", "Assembly-CSharp" };
        private static readonly string[] CandidateNamespaces = { "", "Assembly_CSharp", "CarpenterScripts" };
        private static readonly Dictionary<string, IntPtr> _fieldCache = new Dictionary<string, IntPtr>();

        /// <summary>按 类型名 + 字段名 找到 IL2CPP 静态字段句柄（带缓存）</summary>
        public static IntPtr FindStaticField(string typeName, string fieldName)
        {
            string key = typeName + "." + fieldName;
            if (_fieldCache.TryGetValue(key, out var cached)) return cached;

            try
            {
                IntPtr domain = IL2CPP.il2cpp_domain_get();
                if (domain == IntPtr.Zero) { Inst?.Log.LogError("[SPC] il2cpp_domain_get returned null"); return IntPtr.Zero; }

                foreach (var asmName in CandidateAssemblies)
                {
                    IntPtr asm;
                    IntPtr namePtr = IntPtr.Zero;
                    try
                    {
                        // 签名：il2cpp_domain_assembly_open(IntPtr domain, IntPtr name)
                        namePtr = Marshal.StringToHGlobalAnsi(asmName);
                        asm = IL2CPP.il2cpp_domain_assembly_open(domain, namePtr);
                    }
                    catch { asm = IntPtr.Zero; }
                    finally { if (namePtr != IntPtr.Zero) Marshal.FreeHGlobal(namePtr); }
                    if (asm == IntPtr.Zero) continue;

                    IntPtr image = IL2CPP.il2cpp_assembly_get_image(asm);
                    if (image == IntPtr.Zero) continue;

                    foreach (var ns in CandidateNamespaces)
                    {
                        IntPtr klass;
                        try { klass = IL2CPP.il2cpp_class_from_name(image, ns, typeName); }
                        catch { klass = IntPtr.Zero; }
                        if (klass == IntPtr.Zero) continue;

                        IntPtr field;
                        try { field = IL2CPP.il2cpp_class_get_field_from_name(klass, fieldName); }
                        catch { field = IntPtr.Zero; }

                        if (field != IntPtr.Zero)
                        {
                            Inst?.Log.LogInfo($"[SPC] resolved {typeName}.{fieldName} in {asmName} (ns='{ns}')");
                            _fieldCache[key] = field;
                            return field;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Inst?.Log.LogError($"[SPC] FindStaticField({typeName}.{fieldName}) threw: {e.Message}");
            }

            Inst?.Log.LogWarning($"[SPC] unresolved: {typeName}.{fieldName}");
            _fieldCache[key] = IntPtr.Zero;
            return IntPtr.Zero;
        }

        /// <summary>写 IL2CPP 静态 bool 字段</summary>
        public static unsafe bool SetStaticBool(string typeName, string fieldName, bool value)
        {
            IntPtr field = FindStaticField(typeName, fieldName);
            if (field == IntPtr.Zero) return false;

            try
            {
                // 签名：il2cpp_field_static_set_value(IntPtr field, void* value)
                // IL2CPP 原生侧 bool 占 1 字节
                byte raw = value ? (byte)1 : (byte)0;
                IL2CPP.il2cpp_field_static_set_value(field, &raw);
                return true;
            }
            catch (Exception e)
            {
                Inst?.Log.LogError($"[SPC] set {typeName}.{fieldName}={value} failed: {e.Message}");
                return false;
            }
        }

        /// <summary>启动时跑一次，把能解析/不能解析的字段全打进日志</summary>
        public static void Diagnose()
        {
            var targets = new[]
            {
                Tuple.Create("GameDebugCommands", "CHEAT_INVINCIBLE"),
                Tuple.Create("GameDebugCommands", "CHEAT_INFINITE_AMMO"),
                Tuple.Create("GameDebugCommands", "CHEAT_INFINITE_STAMINA"),
                Tuple.Create("GameDebugCommands", "ALWAYS_WEAKPOINT_HIT"),
                Tuple.Create("GameDebugCommands", "CHEAT_MASTER_KEY"),
                Tuple.Create("GameDebugDrawer", "DRAW_DAMAGE_COLLIDERS"),
                Tuple.Create("AISenses", "s_aiOblivious"),
            };
            int ok = 0;
            foreach (var t in targets)
                if (FindStaticField(t.Item1, t.Item2) != IntPtr.Zero) ok++;
            Inst?.Log.LogInfo($"[SPC] diagnose: {ok}/{targets.Length} fields resolved");
        }
    }

    // ================= 战斗改造 patch =================
    //
    // IL2CPP 下只有 Prefix / Postfix，没有 Transpiler ⇒ 每条改造只能是"改入口/改出口"。
    // 类型一律运行期解析（AccessTools.TypeByName）：命名空间猜错也不会编译失败，日志里能看到卡在哪。
    public static class CombatPatches
    {
        private static Harmony _harmony;

        public static void Apply()
        {
            var log = CombatPlugin.Inst?.Log;
            try
            {
                _harmony = new Harmony(CombatPlugin.GUID);
                PatchDodgeCancel(log);
                PatchStompLunge(log);
                PatchAimLock(log);
                PatchInputSuppress(log);
            }
            catch (Exception e) { log?.LogError("[SPC] CombatPatches.Apply failed: " + e); }
        }

        // ---------- 闪避取消攻击 ----------
        // 落点 InputDodge.m_canBeAnimationBlocked：每个闪避动作实例自己的"会不会被动画挡掉"开关。
        // 不用 IsDodgeAllowed() —— 那是玩家级总闸门（还管菜单/QTE），改它会带出菜单里也能闪避的副作用。
        private static void PatchDodgeCancel(BepInEx.Logging.ManualLogSource log)
        {
            var t = AccessTools.TypeByName("InputDodge");
            if (t == null) { log?.LogError("[SPC] type InputDodge not found"); return; }

            var prefix = new HarmonyMethod(typeof(CombatPatches)
                .GetMethod(nameof(DodgeUnblockPrefix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            if (prefix.method == null) { log?.LogError("[SPC] DodgeUnblockPrefix method missing"); return; }

            int ok = 0;
            foreach (var name in new[] { "OnEnter", "OnUpdate" })
            {
                var m = FindMethod(t, name);
                if (m == null) { log?.LogWarning($"[SPC] InputDodge.{name} not found"); continue; }
                try { _harmony.Patch(m, prefix: prefix); ok++; }
                catch (Exception e) { log?.LogError($"[SPC] patch InputDodge.{name}: {e.Message}"); }
            }
            log?.LogInfo($"[SPC] dodge-cancel: patched {ok}/2 entry points on InputDodge");
        }

        // 记下每个实例的原值，关闭时还原：开关必须双向写，只写 false 会导致关不掉。
        private static readonly Dictionary<int, bool> _dodgeOriginal = new Dictionary<int, bool>();
        private static readonly Dictionary<int, object> _dodgeInstances = new Dictionary<int, object>();
        private static bool _dodgeLogged;
        private static bool? _dodgeWasOn;      // 上一次看到的开关状态
        private static bool _dodgeOffLogged;   // 关闭后的还原日志只打一次

        private static void DodgeUnblockPrefix(object __instance)
        {
            if (__instance == null) return;

            bool want = CombatPlugin.CfgDodgeCancel != null && CombatPlugin.CfgDodgeCancel.Value;
            int id = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(__instance);
            var log = CombatPlugin.Inst?.Log;

            if (_dodgeWasOn == null || _dodgeWasOn.Value != want)
            {
                bool turnedOff = _dodgeWasOn == true && !want;
                _dodgeWasOn = want;
                _dodgeOffLogged = false;
                log?.LogInfo($"[SPC] dodge-cancel switch -> {(want ? "ON" : "OFF")}  (instance id={id})");

                // 关闭瞬间批量还原所有见过的实例：有些实例之后可能再也不更新了。
                if (turnedOff)
                {
                    int n = 0, ok = 0;
                    foreach (var kv in _dodgeInstances)
                    {
                        n++;
                        bool orig = _dodgeOriginal.TryGetValue(kv.Key, out var o) ? o : true;
                        if (SetMemberBool(kv.Value, "m_canBeAnimationBlocked", orig)) ok++;
                    }
                    log?.LogInfo($"[SPC] dodge-cancel OFF: bulk restore {ok}/{n} instance(s)");
                }
            }

            if (want)
            {
                // 游戏默认 true = 动画能挡住闪避
                if (!_dodgeOriginal.ContainsKey(id))
                    _dodgeOriginal[id] = GetMemberBool(__instance, "m_canBeAnimationBlocked", true);
                _dodgeInstances[id] = __instance;

                if (SetMemberBool(__instance, "m_canBeAnimationBlocked", false) && !_dodgeLogged)
                {
                    _dodgeLogged = true;
                    log?.LogInfo($"[SPC] dodge-cancel ACTIVE: m_canBeAnimationBlocked -> false (orig={_dodgeOriginal[id]})");
                }
            }
            else
            {
                // 关掉时无条件还原，没记录过就按游戏默认 true 写。
                bool orig = _dodgeOriginal.TryGetValue(id, out var o) ? o : true;
                bool ok = SetMemberBool(__instance, "m_canBeAnimationBlocked", orig);

                if (!_dodgeOffLogged)
                {
                    _dodgeOffLogged = true;
                    log?.LogInfo($"[SPC] dodge-cancel OFF: restore id={id} -> {orig} (ok={ok})");
                }
            }
        }

        // ---------- 踩踏前踏吸附 ----------
        // 落点 CheckForStompAttack.OnAttack()：玩家按下攻击键、进入踩踏判定的那一刻。
        // Prefix 找最近的敌人并把角色挪到它面前 StopDistance 处，再强制放行这一次踩踏；Postfix 复位。
        private static void PatchStompLunge(BepInEx.Logging.ManualLogSource log)
        {
            var t = AccessTools.TypeByName("CheckForStompAttack");
            if (t == null) { log?.LogError("[SPC] type CheckForStompAttack not found"); return; }

            var m = FindMethod(t, "OnAttack");
            if (m == null) { log?.LogError("[SPC] CheckForStompAttack.OnAttack not found"); return; }

            try
            {
                _harmony.Patch(m,
                    prefix: new HarmonyMethod(typeof(CombatPatches)
                        .GetMethod(nameof(StompLungePrefix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)),
                    postfix: new HarmonyMethod(typeof(CombatPatches)
                        .GetMethod(nameof(StompLungePostfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)));
                log?.LogInfo("[SPC] stomp-lunge: CheckForStompAttack.OnAttack patched");
            }
            catch (Exception e) { log?.LogError("[SPC] patch CheckForStompAttack.OnAttack: " + e.Message); }
        }

        private static void StompLungePrefix(object __instance)
        {
            if (CombatPlugin.CfgStompLunge == null || !CombatPlugin.CfgStompLunge.Value) return;
            var log = CombatPlugin.Inst?.Log;
            if (__instance == null) return;

            try
            {
                var input = GetMember(__instance, "m_input") as UnityEngine.Object;
                var inputComp = input != null ? input.TryCast<UnityEngine.Component>() : null;
                if (inputComp == null) { log?.LogWarning("[SPC] stomp-lunge: m_input is null"); return; }
                var playerTf = inputComp.transform;
                int playerRootId = playerTf.root.GetInstanceID();
                var playerPos = playerTf.position;

                // 找最近的、不属于玩家自己的 CharacterHealth
                var hpType = AccessTools.TypeByName("CharacterHealth");
                if (hpType == null) { log?.LogWarning("[SPC] stomp-lunge: CharacterHealth type missing"); return; }

                float range = CombatPlugin.CfgStompLungeRange.Value;
                UnityEngine.Transform best = null;
                float bestD = float.MaxValue;
                string bestName = "?";

                var all = UnityEngine.Object.FindObjectsOfType(Il2CppType.From(hpType));

                foreach (var h in all)
                {
                    if (h == null) continue;
                    var hc = h.TryCast<UnityEngine.Component>();
                    if (hc == null) continue;
                    var tf = hc.transform;
                    if (tf.root.GetInstanceID() == playerRootId) continue;   // 跳过玩家自己

                    var d = Vector3.Distance(new Vector3(tf.position.x, 0f, 0f), new Vector3(playerPos.x, 0f, 0f));
                    if (d < bestD) { bestD = d; best = tf; bestName = hc.gameObject.name; }
                }

                if (best == null || bestD > range)
                {
                    log?.LogInfo($"[SPC] stomp-lunge: no target in {range:0.0}m (nearest={bestName} d={bestD:0.00}) -> 原版行为");
                    return;
                }

                var delta = best.position - playerPos;
                delta.y = 0f; delta.z = 0f;
                if (delta.magnitude < 0.001f) return;
                var dir = delta.normalized;

                // 停留距离按敌人碰撞体宽度自适应收紧（固定值会踩空）：
                // stop = min(配置值, 敌人半宽 * 0.6)，下限 0.05。
                float stop = CombatPlugin.CfgStompStopDistance.Value;
                try
                {
                    var ecolRaw = best.GetComponent(Il2CppType.From(typeof(UnityEngine.Collider2D)));
                    var ecol = ecolRaw != null ? ecolRaw.TryCast<UnityEngine.Collider2D>() : null;
                    if (ecol != null)
                    {
                        float halfW = ecol.bounds.extents.x;
                        float adaptive = halfW * 0.6f;
                        if (adaptive < 0.05f) adaptive = 0.05f;
                        log?.LogInfo($"[SPC] stomp-lunge: target collider halfWidth={halfW:0.000} -> adaptive stop={adaptive:0.000} (cfg={stop:0.000})");
                        if (adaptive < stop) stop = adaptive;
                    }
                    else log?.LogInfo("[SPC] stomp-lunge: target has no Collider2D, use cfg stop as-is");
                }
                catch (Exception e) { log?.LogWarning("[SPC] stomp-lunge: adaptive stop failed: " + e.Message); }

                var newPos = best.position - dir * stop;
                newPos.y = playerPos.y;      // 只改水平，垂直沿用玩家自己的（避免掉进地板/浮空）

                float dur = CombatPlugin.CfgStompLungeDuration?.Value ?? 0.14f;
                log?.LogInfo($"[SPC] stomp-lunge START pos={playerPos} target=\"{bestName}\" d={bestD:0.00} -> {newPos}  dur={dur:0.000}s");
                // 匀速推过去，不用瞬移（瞬移观感很怪）
                CombatBehaviour.StartLunge(playerTf, playerPos, newPos, dur);
                log?.LogInfo("[SPC] stomp-lunge: coroutine started (位移进行中)");

                // 强制放行这一次踩踏（否则 CanStomp 还是上一帧在旧位置算的 false）
                SetMemberBool(__instance, "m_forceAllowStomp", true);
                var sac = GetMember(__instance, "m_stompActionCheck");
                if (sac != null) SetMemberBool(sac, "ForceAllowStomp", true);
            }
            catch (Exception e) { log?.LogWarning("[SPC] stomp-lunge prefix failed: " + e.Message); }
        }

        private static void StompLungePostfix(object __instance)
        {
            if (__instance == null) return;
            try
            {
                SetMemberBool(__instance, "m_forceAllowStomp", false);
                var sac = GetMember(__instance, "m_stompActionCheck");
                if (sac != null) SetMemberBool(sac, "ForceAllowStomp", false);
            }
            catch { }
        }

        /// <summary>读成员的 bool 值：属性优先，其次字段，都没有就用 fallback</summary>
        private static bool GetMemberBool(object obj, string name, bool fallback)
        {
            const System.Reflection.BindingFlags any =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            try
            {
                var t = obj.GetType();
                var p = t.GetProperty(name, any);
                if (p != null) return (bool)p.GetValue(obj);
                var f = t.GetField(name, any);
                if (f != null) return (bool)f.GetValue(obj);
            }
            catch { }
            return fallback;
        }

        /// <summary>
        /// 按【已知类型】读成员。IL2CPP 代理对象的 GetType() 返回基类（Component/Object），
        /// 对它反射游戏类成员会抛 TargetException 并被 catch 吞掉，表现为"字段读不到"。
        /// 必须先用 AccessTools.TypeByName 拿真实类型再反射。
        /// </summary>
        public static object GetMemberTyped(Type t, object obj, string name)
        {
            if (t == null || obj == null) return null;
            const System.Reflection.BindingFlags any =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            try
            {
                var p = t.GetProperty(name, any);
                if (p != null) return p.GetValue(obj);
                var f = t.GetField(name, any);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 按【真实类型】重新包装代理。IL2CPP 代理对象的 GetType() 返回基类（Component/Object），
        /// 所以反射游戏类成员前必须先用 Pointer + 真实类型重建一个类型正确的代理。
        /// </summary>
        public static object Rewrap(Type t, object o)
        {
            if (t == null || o == null) return o;
            try
            {
                var b = o as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;
                if (b == null || b.Pointer == IntPtr.Zero) return o;
                return Activator.CreateInstance(t, b.Pointer);
            }
            catch { return o; }
        }

        /// <summary>按【已知类型】写成员（字段或可写属性）</summary>
        public static bool SetMemberTyped(Type t, object obj, string name, object value)
        {
            if (t == null || obj == null) return false;
            const System.Reflection.BindingFlags any =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            try
            {
                var p = t.GetProperty(name, any);
                if (p != null && p.CanWrite) { p.SetValue(obj, value); return true; }
                var f = t.GetField(name, any);
                if (f != null) { f.SetValue(obj, value); return true; }
            }
            catch { }
            return false;
        }

        /// <summary>读成员：Il2CppInterop 把字段生成为属性，两种都试</summary>
        public static object GetMember(object obj, string name)
        {
            const System.Reflection.BindingFlags any =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = obj.GetType();
            try
            {
                var p = t.GetProperty(name, any);
                if (p != null) return p.GetValue(obj);
                var f = t.GetField(name, any);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

        // ---------- 手柄：左摇杆不影响瞄准 ----------
        //
        // 采样实测（BepInEx 日志，字段名来自 dump.cs）：
        //   右摇杆有输入 → m_aimDirection ≈ m_gamepadRawAimInput
        //   右摇杆静止   → m_aimDirection ≈ m_movementInput（左摇杆 / 移动方向）
        //   两者都有     → 右摇杆赢
        // 也就是"边跑边瞄准，枪口跟着跑动方向甩"是游戏有意的兜底，不是 bug。
        //
        // 修法：patch CharacterInputPlayer.get_AimDirectionInput()（下游 CharacterAiming 读的就是它）。
        // 不改 m_aimDirection 字段 —— 合成发生在游戏自己的 Update 里，
        // 我们 MonoBehaviour 的 Update/LateUpdate 相对它的先后顺序不稳定，写了可能下一帧就被覆盖。
        // patch getter 是唯一确定的拦截点：无论谁读、什么时候读，拿到的都是我们给的值。
        private static Type _aimLockType;
        private static Vector2 _lockDir;      // 锁定方向（单位向量）
        private static bool _lockValid;       // _lockDir 是否可用
        private static bool _wasAiming;       // 上一次采样是否在瞄准（用于捕捉"举枪瞬间"）
        private static bool _lockLogged;      // 生效只日志一次，避免刷屏
        private static bool _typeMismatchLogged;
        private static Vector2 _lastMouse;    // 上一帧 m_mouseAimPosition（判断"玩家在主动转向"）
        private static bool _hasLastMouse;
        private static float _nextLockLog;    // 干预日志节流

        private static void PatchAimLock(BepInEx.Logging.ManualLogSource log)
        {
            var t = AccessTools.TypeByName("CharacterInputPlayer");
            if (t == null) { log?.LogError("[SPC] aim-lock: type CharacterInputPlayer not found"); return; }

            var m = FindMethod(t, "get_AimDirectionInput");
            if (m == null) { log?.LogError("[SPC] aim-lock: get_AimDirectionInput not found"); return; }

            var postfix = new HarmonyMethod(typeof(CombatPatches)
                .GetMethod(nameof(AimDirInputPostfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            if (postfix.method == null) { log?.LogError("[SPC] aim-lock: AimDirInputPostfix missing"); return; }

            try
            {
                _harmony.Patch(m, postfix: postfix);
                _aimLockType = t;
                log?.LogInfo("[SPC] aim-lock patched: CharacterInputPlayer.get_AimDirectionInput");
            }
            catch (Exception e) { log?.LogError("[SPC] aim-lock patch failed: " + e.Message); }
        }

        /// <summary>
        /// 覆盖瞄准输入：右摇杆静止 + 左摇杆在动 ⇒ 返回锁定方向，而不是移动方向。
        /// 键鼠模式天然不受影响 —— 键鼠的瞄准由鼠标位置算，永远不会出现"瞄准方向 == 移动方向"这条回退路径。
        /// </summary>
        private static void AimDirInputPostfix(object __instance, ref Vector2 __result)
        {
            try
            {
                var cfg = CombatPlugin.CfgAimLockLeftStick;
                if (cfg == null || !cfg.Value) return;

                var inst = Rewrap(_aimLockType, __instance);
                if (inst == null) return;

                Vector2 locked;
                if (!TryComputeLock(inst, __result, out locked)) return;

                if (!_lockLogged)
                {
                    _lockLogged = true;
                    CombatPlugin.Inst?.Log.LogInfo($"[SPC] aim-lock 生效(getter): 锁定 {_lockDir}，左摇杆不再改变瞄准角度");
                }
                __result = locked;
            }
            catch { }
        }

        /// <summary>
        /// 判断这一帧要不要把瞄准方向换成锁定方向。
        ///
        /// 关键教训：不能只看 m_gamepadRawAimInput。实测发现同一台机器上瞄准可能走两条路——
        ///   ① 手柄直连：右摇杆值出现在 m_gamepadRawAimInput，m_mouseAimPosition 恒 0
        ///   ② 鼠标/模拟鼠标：右摇杆值被消费掉，m_gamepadRawAimInput 恒 0，真正的方向在 m_mouseAimPosition
        /// 只认 ① 会导致 ② 下"基准永不更新 + 左摇杆一动就锁死"，表现就是左摇杆抢控制权。
        ///
        /// 所以判据改成"现象检测"：只要这一帧瞄准方向相对移动方向不是被绑架的状态，就放行并更新基准。
        /// </summary>
        private static bool TryComputeLock(object inst, Vector2 current, out Vector2 locked)
        {
            locked = current;
            try
            {
                var aimV = GetMemberTyped(_aimLockType, inst, "IsAiming");
                if (!(aimV is bool) || !(bool)aimV) { _wasAiming = false; _lockValid = false; return false; }

                var rawV = GetMemberTyped(_aimLockType, inst, "m_gamepadRawAimInput");
                var mvV = GetMemberTyped(_aimLockType, inst, "m_movementInput");
                var mouseV = GetMemberTyped(_aimLockType, inst, "m_mouseAimPosition");
                if (!(mvV is Vector2) && !_typeMismatchLogged)
                {
                    _typeMismatchLogged = true;
                    CombatPlugin.Inst?.Log.LogWarning($"[SPC] aim-lock: 摇杆值类型不符 ({mvV?.GetType().Name})，已停用");
                    return false;
                }
                Vector2 raw = rawV is Vector2 r ? r : Vector2.zero;
                Vector2 mv = mvV is Vector2 v ? v : Vector2.zero;
                Vector2 mouse = mouseV is Vector2 mz ? mz : Vector2.zero;

                // 瞄准源是否在发生变化（鼠标位移 / 摇杆直连都算"玩家在主动转向"）
                bool mouseMoved = _hasLastMouse && (mouse - _lastMouse).sqrMagnitude > 1.0f;
                _lastMouse = mouse;
                _hasLastMouse = true;

                float dz = CombatPlugin.CfgAimLockDeadzone?.Value ?? 0.15f;
                float dz2 = dz * dz;

                // ① 玩家在主动转向（右摇杆有值，或鼠标位置在变）⇒ 放行，并把方向记成新基准
                if (raw.sqrMagnitude > dz2 || mouseMoved)
                {
                    if (current.sqrMagnitude > 0.0001f)
                    {
                        _lockDir = current.normalized;
                        _lockValid = true;
                    }
                    _wasAiming = true;
                    return false;
                }

                // ② 举枪的那一帧：以游戏此刻给的方向为基准（举枪时朝哪就锁哪）
                if (!_wasAiming)
                {
                    _wasAiming = true;
                    _lockDir = current.sqrMagnitude > 0.0001f ? current.normalized : Vector2.zero;
                    _lockValid = _lockDir.sqrMagnitude > 0.0001f;
                    return false;
                }

                if (!_lockValid) return false;              // 没有基准可锁 ⇒ 放行原行为
                if (mv.sqrMagnitude < dz2) return false;    // 左摇杆也没动 ⇒ 没什么可纠正的

                // ③ 左摇杆在动：只有当瞄准方向确实被它拽过去（两者夹角很小）才认为是"回退"
                //    夹角大说明玩家本来就想瞄那个方向，放行（否则会误锁）
                if (current.sqrMagnitude > 0.0001f)
                {
                    float dot = Vector2.Dot(current.normalized, mv.normalized);
                    if (dot < 0.85f) return false;          // 夹角 > ~32°，不是回退
                }

                locked = _lockDir;
                return true;
            }
            catch { return false; }
        }

        // ---------- 瞄准时压制移动输入（时序正确的三条路）----------
        //
        // ⛔ 教训：在 MonoBehaviour.LateUpdate 里写 m_movementInput 是没用的 ——
        //    游戏每帧在自己的 Update 开头从 InputSystem 重新刷新这个字段，
        //    我们写的值只存在于"本帧所有消费都结束之后"，从来没被读到过（实测：日志里 move 依然非 0）。
        //
        // ✅ 正解是在【写入点】上拦截：InputSystem 的 action 回调早于 MonoBehaviour.Update 触发，
        //    在 OnMove 写完之后立刻清零，游戏随后消费到的就是 0。
        //    同时给属性 getter 也挂一个（万一游戏读的是属性而不是字段）。
        private static void PatchInputSuppress(BepInEx.Logging.ManualLogSource log)
        {
            var t = AccessTools.TypeByName("CharacterInputPlayer");
            if (t == null) { log?.LogError("[SPC] move-suppress: type not found"); return; }

            var onMove = FindMethod(t, "OnMove");
            if (onMove != null)
            {
                var pf = new HarmonyMethod(typeof(CombatPatches)
                    .GetMethod(nameof(OnMovePostfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                if (pf.method != null)
                {
                    try { _harmony.Patch(onMove, postfix: pf); log?.LogInfo("[SPC] move-suppress patched: CharacterInputPlayer.OnMove"); }
                    catch (Exception e) { log?.LogError("[SPC] move-suppress OnMove failed: " + e.Message); }
                }
            }
            else log?.LogWarning("[SPC] move-suppress: OnMove not found");

            var getMv = FindMethod(t, "get_MovementInput");
            if (getMv != null)
            {
                var pf2 = new HarmonyMethod(typeof(CombatPatches)
                    .GetMethod(nameof(MovementInputPostfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                if (pf2.method != null)
                {
                    try { _harmony.Patch(getMv, postfix: pf2); log?.LogInfo("[SPC] move-suppress patched: get_MovementInput"); }
                    catch (Exception e) { log?.LogError("[SPC] move-suppress getter failed: " + e.Message); }
                }
            }
        }

        /// <summary>OnMove 写完立刻清零：这是输入回调，早于所有 MonoBehaviour.Update</summary>
        private static void OnMovePostfix(object __instance)
        {
            try
            {
                var cfg = CombatPlugin.CfgAimLockLeftStick;
                if (cfg == null || !cfg.Value) return;

                var inst = Rewrap(_aimLockType, __instance);
                if (inst == null) return;

                // ★ 无论是否在瞄准，都要更新"最后一次真实输入"缓存。
                //   之前只在瞄准时更新 ⇒ 缓存会停留在上一次瞄准期间的值，
                //   退出瞄准时把陈旧方向写回去 ⇒ 出现"随机往反方向跑"（实测踩过）。
                var curV = GetMemberTyped(_aimLockType, inst, "m_movementInput");
                if (curV is Vector2 cv) _lastRawMove = cv;

                var aimV = GetMemberTyped(_aimLockType, inst, "IsAiming");
                if (!(aimV is bool) || !(bool)aimV) return;

                SetMemberTyped(_aimLockType, inst, "m_movementInput", Vector2.zero);
            }
            catch { }
        }

        /// <summary>防守：游戏若读属性而非字段，这里直接返回零向量</summary>
        private static void MovementInputPostfix(object __instance, ref Vector2 __result)
        {
            try
            {
                var cfg = CombatPlugin.CfgAimLockLeftStick;
                if (cfg == null || !cfg.Value) return;

                var inst = Rewrap(_aimLockType, __instance);
                if (inst == null) return;
                var aimV = GetMemberTyped(_aimLockType, inst, "IsAiming");
                if (!(aimV is bool) || !(bool)aimV) return;

                __result = Vector2.zero;
            }
            catch { }
        }

        // ---------- 瞄准时禁用移动输入 ----------
        //
        // 实测现象（用户描述，两条输入路径都成立）：
        //   手柄：左摇杆与右摇杆【不同侧】时，瞄准角被挤到最高/最低位
        //   键鼠：鼠标不动时 A/D 让瞄准线【水平镜像】（与地面夹角不变）；鼠标与移动反向时人物原地转向
        // 共同点：瞄准方向被角色的左右翻转做了【镜像】。
        //
        // CharacterDirection 是 2D 精灵，朝向只有 Left/Right、靠 m_flipTranforms 翻转实现，
        // 而朝向决策读的是 m_movementInput 的 x 符号。游戏在瞄准时本来就不允许移动，
        // 所以 move 在这段时间只干两件坏事：
        //   ① 决定角色左右翻转 ⇒ 瞄准方向被水平镜像（镜像后角度超出手臂可达范围 → 卡最高/最低位）
        //   ② 右摇杆静止时被当作瞄准方向的回退值 ⇒ 直接污染瞄准
        //
        // ⇒ 直接把 move 置零，一次掐掉两个来源。
        // ⛔ 不要写成 aim.normalized：模长 1.0 表示【全速移动】，会让依赖模长的逻辑以为玩家在冲刺
        //    （上一版就是这么写错的，表现为"影响依然很烂"）。
        // ⛔ 不要硬写 CharacterDirection.CurrentDirection：游戏每帧翻过去、我们每帧翻回来，
        //    两边打架 ⇒ "人物一直原地转向"（也踩过）。
        private static Vector2 _lastRawMove;   // 压制期间最后一次真实移动输入（退出瞄准时归还）
        private static bool _moveSuppressed;   // 上一帧是否处于"压制"状态

        private static void SuppressMovementWhileAiming(object inst, bool aiming)
        {
            try
            {
                var cfg = CombatPlugin.CfgAimLockLeftStick;
                if (cfg == null || !cfg.Value) { RestoreRawMove(inst); return; }

                if (aiming)
                {
                    _moveSuppressed = true;
                    SetMemberTyped(_aimLockType, inst, "m_movementInput", Vector2.zero);
                    return;
                }

                RestoreRawMove(inst);
            }
            catch { }
        }

        /// <summary>
        /// 退出压制（松开瞄准 / 面板里关掉开关）：把缓存下来的真实移动输入还回去。
        /// 否则 OnMove 不会再触发（按键状态没变），角色会一直不动直到重新按键 —— 实测踩过。
        /// </summary>
        private static void RestoreRawMove(object inst)
        {
            if (!_moveSuppressed) return;
            _moveSuppressed = false;
            try
            {
                if (_lastRawMove.sqrMagnitude < 0.0001f) return;

                // ★ 只在这格还是 0 的时候才还回去。
                //   如果游戏刚刚已经写入了新的非零输入（玩家退出瞄准后又按了键），
                //   说明它已经接管，我们再覆盖就是拿旧值把新值打掉 —— 又是反向跑的来源。
                var curV = GetMemberTyped(_aimLockType, inst, "m_movementInput");
                if (curV is Vector2 cv && cv.sqrMagnitude > 0.0001f) return;

                SetMemberTyped(_aimLockType, inst, "m_movementInput", _lastRawMove);
            }
            catch { }
        }

        // 兜底：IL2CPP 里 get_AimDirectionInput 这种一行 getter 很可能被编译器内联，
        // 一旦内联，Harmony 的 detour 就打不进去（方法压根没被调用）。
        // 所以另外每帧直接写 m_aimDirection —— 下游读的是字段，写字段不依赖调用链。
        // 写在本帧 LateUpdate（此时游戏自己的 Update 都跑完了），下一帧生效，滞后 16ms，观感无感。
        private static object _lockPlayer;
        private static int _lockPlayerId;
        private static float _lockNextRefresh;

        public static void AimLockTickField()
        {
            try
            {
                var cfg = CombatPlugin.CfgAimLockLeftStick;
                if (cfg == null || !cfg.Value) return;
                if (_aimLockType == null) _aimLockType = AccessTools.TypeByName("CharacterInputPlayer");
                if (_aimLockType == null) return;

                if (_lockPlayer == null || Time.unscaledTime >= _lockNextRefresh)
                {
                    _lockNextRefresh = Time.unscaledTime + 3f;
                    var o = UnityEngine.Object.FindFirstObjectByType(Il2CppType.From(_aimLockType));
                    if (o == null) return;
                    int id = o.GetInstanceID();
                    if (_lockPlayer == null || id != _lockPlayerId)
                    {
                        _lockPlayer = Rewrap(_aimLockType, o);
                        _lockPlayerId = id;
                        _wasAiming = false;
                        _lockValid = false;
                        _hasLastMouse = false;
                    }
                }
                if (_lockPlayer == null) return;

                var aimV = GetMemberTyped(_aimLockType, _lockPlayer, "IsAiming");
                bool aimingNow = aimV is bool ba && ba;

                var curV = GetMemberTyped(_aimLockType, _lockPlayer, "m_aimDirection");
                Vector2 cur = curV is Vector2 c ? c : Vector2.zero;

                Vector2 locked;
                bool over = TryComputeLock(_lockPlayer, cur, out locked);
                if (over && SetMemberTyped(_aimLockType, _lockPlayer, "m_aimDirection", locked))
                {
                    if (!_lockLogged)
                    {
                        _lockLogged = true;
                        CombatPlugin.Inst?.Log.LogInfo($"[SPC] aim-lock 生效(字段兜底): 锁定 ({_lockDir.x:0.00},{_lockDir.y:0.00})");
                    }
                    // 干预过程也要留痕：否则"没生效"和"生效了但被游戏改回去"在日志里长得一样
                    if (Time.unscaledTime >= _nextLockLog)
                    {
                        _nextLockLog = Time.unscaledTime + 2f;
                        CombatPlugin.Inst?.Log.LogInfo($"[SPC] aim-lock 干预: game=({cur.x:0.00},{cur.y:0.00}) → locked=({_lockDir.x:0.00},{_lockDir.y:0.00})");
                    }
                }

                // 朝向跟随：用【修正后】的方向算，否则左摇杆能通过 move 间接把角色翻过去
                // 瞄准时禁用移动输入：朝向不再被 A/D、左摇杆抢走，瞄准也不会被回退污染
                SuppressMovementWhileAiming(_lockPlayer, aimingNow);
            }
            catch { }
        }

        // ---------- 辅助瞄准（自己实现）----------
        // 落点 CharacterAiming.UpdateAim 的 Postfix：它内部算完 m_previousAimVector / m_aimInput 之后、

        // ---------- 工具 ----------

        /// <summary>按名字找方法：先走 AccessTools，再退回全类型扫描（应对重载/继承）</summary>
        public static System.Reflection.MethodInfo FindMethodPublic(Type t, string name) => FindMethod(t, name);

        /// <summary>按名字找方法：先走 AccessTools，再退回全类型扫描（应对重载/继承）</summary>
        private static System.Reflection.MethodInfo FindMethod(Type t, string name)
        {
            var m = AccessTools.Method(t, name);
            if (m != null) return m;

            const System.Reflection.BindingFlags f =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
            try
            {
                foreach (var cand in t.GetMethods(f))
                    if (cand.Name == name) return cand;
            }
            catch { }
            return null;
        }

        /// <summary>写成员的 bool 值：Il2CppInterop 把字段生成为属性，两种都试</summary>
        private static bool SetMemberBool(object obj, string name, bool value)
        {
            const System.Reflection.BindingFlags any =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = obj.GetType();
            try
            {
                var p = t.GetProperty(name, any);
                if (p != null && p.CanWrite) { p.SetValue(obj, value); return true; }

                var f = t.GetField(name, any);
                if (f != null) { f.SetValue(obj, value); return true; }
            }
            catch (Exception e) { CombatPlugin.Inst?.Log.LogWarning($"[SPC] set {name} failed: {e.Message}"); }
            return false;
        }
    }

    // ================= 瞄准辅助线 =================
    //
    // 不复用游戏自带的 LaserBeam：它的 m_lineRenderer 是 [SerializeField]，
    // 运行时 AddComponent 出来是 null，只有拿到 M1911 那个 prefab 实例才有 —— 拿不到。
    // 数据：方向 = CharacterAiming.PreviousAimVector，起点 = ProjectileWeapon.m_muzzle（枪口）。
    public static class AimLine
    {
        private static UnityEngine.LineRenderer _lr;
        private static UnityEngine.Component _player;
        private static object _aimComp;           // ★ 必须是"真实类型的代理"，不能 TryCast<Component>
        private static Type _playerType;          // 真实类型（obj.GetType() 不可信）
        private static Type _aimType;
        private static bool _warned;
        private static bool _shown;              // 上一帧是否可见（用于状态切换日志）
        private static string _lastReason = "";
        private static bool _dirWasValid;        // 方向来源的过渡状态
        private static bool _aimingWarned;
        private static Type _weaponType;         // ProjectileWeapon 的真实类型
        private static bool _weaponWasNull = true; // 武器有无的过渡状态
        private static bool _hadMuzzle;          // 枪口有无的过渡状态
        private static bool _muzzleWarned;
        private static float _nextRefresh;      // 每 2s 重查玩家，防止跨关卡缓存过期
        private static int _playerId;
        private static object _playerRef;       // CharacterInputPlayer 的真实类型代理（反射用）
        private static float _nextDiag;
        private static float _aimSince = -1f;    // 本次瞄准开始时间（<0 = 没在瞄）
        private static float _stableSince = -1f; // 枪口开始静止的时间
        private static Vector3 _lastOrigin;
        private static bool _aimStateLogged;
        private static bool _aimStateLast;
        private static bool _diagDone;
        private static string _originFrom = "?";

        public static void Tick()
        {
            bool on = CombatPlugin.CfgAimLineEnabled != null && CombatPlugin.CfgAimLineEnabled.Value;
            if (!on) { Hide("config off"); return; }

            if (!ResolvePlayer()) { Hide("player not resolved"); return; }

            // 只在瞄准时显示：不瞄准时枪口姿态不更新，线会僵在原地挡视线。
            bool aiming = IsAiming();
            if (!aiming) { _aimSince = -1f; _stableSince = -1f; Hide("not aiming"); Diag(); return; }

            if (!EnsureRenderer()) { Hide("renderer create failed"); return; }

            Vector3 origin;
            Vector3 dir;
            if (!TryGetOriginAndDir(out origin, out dir)) { Hide("origin/dir unavailable"); return; }

            // 等枪"抬稳"再显示：抬手动画期间枪口每帧在动，直接画会看到线从低位滑上来。
            // 连续静止 settleTime 就画，最多等 maxWait。
            {
                float maxWait = CombatPlugin.CfgAimLineShowDelay?.Value ?? 0.15f;
                const float settleTime = 0.08f;
                const float moveEps = 0.015f;

                if (_aimSince < 0f)
                {
                    _aimSince = Time.unscaledTime;
                    _stableSince = -1f;
                    _lastOrigin = origin;
                }

                bool moving = Vector3.Distance(origin, _lastOrigin) > moveEps;
                _lastOrigin = origin;

                if (moving) _stableSince = -1f;
                else if (_stableSince < 0f) _stableSince = Time.unscaledTime;

                float elapsed = Time.unscaledTime - _aimSince;
                bool settled = _stableSince >= 0f && (Time.unscaledTime - _stableSince) >= settleTime;
                bool timeout = elapsed >= maxWait;

                if (!settled && !timeout) { Hide("aim raise in progress"); return; }
            }

            // 每帧刷新宽度/颜色，滑块改动立刻生效（否则只有重建时才读配置）
            float w = CombatPlugin.CfgAimLineWidth?.Value ?? 0.007f;
            float g = CombatPlugin.CfgAimLineGray?.Value ?? 0.75f;
            try
            {
                _lr.startWidth = w;
                _lr.endWidth = w;
                var c = new UnityEngine.Color(g, g, g, 1f);
                _lr.startColor = c;
                _lr.endColor = c;
            }
            catch { }

            float len = CombatPlugin.CfgAimLineLength?.Value ?? 2.0f;

            // 顺序很重要：先把两个端点写好，最后才 enabled = true。
            // 反过来的话，启用瞬间渲染器里还留着上一次的点，会闪一帧旧位置。
            var end = new Vector3(origin.x + dir.x * len, origin.y + dir.y * len, origin.z);
            _lr.positionCount = 2;
            _lr.SetPosition(0, origin);
            _lr.SetPosition(1, end);

            Show(origin, dir);
        }

        private static void Show(Vector3 origin, Vector3 dir)
        {
            if (_shown) return;          // 只在"由隐藏变可见"时打一条，避免每帧刷屏
            _shown = true;
            CombatPlugin.Inst?.Log.LogInfo(
                $"[SPC] AimLine VISIBLE: 起点={_originFrom} origin=({origin.x:0.00},{origin.y:0.00}) " +
                $"dir=({dir.x:0.00},{dir.y:0.00})");
        }

        private static void Hide(string why)
        {
            try
            {
                if (_lr != null)
                {
                    // 用点数清零来隐藏，不切 enabled：刚启用的那一帧可能用旧点位渲染，会闪一下旧线。
                    if (_lr.positionCount != 0) _lr.positionCount = 0;
                }
            }
            catch { }
            if (!_shown && _lastReason == why) return;
            _shown = false;
            if (_lastReason != why)
            {
                _lastReason = why;
                CombatPlugin.Inst?.Log.LogInfo($"[SPC] AimLine HIDDEN: {why}");
            }
        }

        /// <summary>
        /// 把 IL2CPP 包装对象按真实类型重新包一次。
        /// GetComponent / TryCast 返回的是基类包装，拿它反射游戏类成员会 TargetException。
        /// </summary>
        private static object Rewrap(Type t, object obj)
        {
            try
            {
                if (t == null || obj == null) return null;
                var baseObj = obj as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;
                if (baseObj == null) return null;
                IntPtr ptr = baseObj.Pointer;
                if (ptr == IntPtr.Zero) return null;
                return Activator.CreateInstance(t, ptr);
            }
            catch { return null; }
        }

        private static bool ResolvePlayer()
        {
            // 每 2 秒重查一次：跨关卡/读档后旧代理还非 null，但成员全读成默认值
            // （表现就是瞄准线突然全没了），所以实例 ID 变了必须丢掉所有缓存。
            bool need = _player == null || _playerType == null || Time.unscaledTime >= _nextRefresh;
            if (!need) return true;
            _nextRefresh = Time.unscaledTime + 2f;

            try
            {
                if (_playerType == null) _playerType = AccessTools.TypeByName("CharacterInputPlayer");
                if (_playerType == null) return false;

                var o = UnityEngine.Object.FindFirstObjectByType(Il2CppType.From(_playerType));
                if (o == null) return _player != null;    // 场景过渡中：暂时沿用旧引用

                int id = o.GetInstanceID();
                if (_player == null || id != _playerId)
                {
                    _player = o.TryCast<UnityEngine.Component>();
                    _playerRef = Rewrap(_playerType, o);
                    _playerId = id;
                    _aimComp = null;          // 玩家换了 → 瞄准组件缓存必须作废
                    _hadMuzzle = false;
                    _weaponWasNull = true;
                    _dirWasValid = false;
                    _diagDone = false;
                }
                return _player != null;
            }
            catch { return _player != null; }
        }

        /// <summary>CharacterAiming.AimBlocked（private bool m_isAimBlocked，带 OnAimBlockedChanged 事件）</summary>
        private static bool IsAimBlocked()
        {
            try
            {
                if (!ResolveAimComp()) return false;
                var v = CombatPatches.GetMemberTyped(_aimType, _aimComp, "AimBlocked");
                if (v is bool b) return b;
                var f = CombatPatches.GetMemberTyped(_aimType, _aimComp, "m_isAimBlocked");
                if (f is bool b2) return b2;
            }
            catch { }
            return false;
        }

        private static bool ResolveAimComp()
        {
            if (_aimComp != null) return true;
            try
            {
                if (_aimType == null) _aimType = AccessTools.TypeByName("CharacterAiming");
                if (_aimType == null || _player == null) return false;

                object raw = _player.gameObject.GetComponent(Il2CppType.From(_aimType));
                if (raw == null) raw = _player.gameObject.GetComponentInChildren(Il2CppType.From(_aimType), true);
                if (raw == null) return false;

                _aimComp = Rewrap(_aimType, raw);
                if (_aimComp == null && !_warned)
                {
                    _warned = true;
                    CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: CharacterAiming 重包装失败（Pointer 为空）");
                }
                return _aimComp != null;
            }
            catch (Exception e)
            {
                if (!_warned) { _warned = true; CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: ResolveAimComp failed: " + e.Message); }
                return false;
            }
        }

        private static bool IsAiming()
        {
            const System.Reflection.BindingFlags any =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            bool hasAim = false, aimActive = false;
            try
            {
                if (ResolveAimComp())
                {
                    var act = CombatPatches.GetMemberTyped(_aimType, _aimComp, "AimingActive");
                    if (act is bool b) { aimActive = b; hasAim = true; }
                }
            }
            catch { }

            bool hasInput = false, inputAiming = false;
            try
            {
                // 必须用 _playerRef（真实类型代理）：_player 是 Component 包装，反射会 TargetException。
                if (_playerRef != null && _playerType != null)
                {
                    var p = _playerType.GetProperty("IsAiming", any);
                    if (p != null && p.GetValue(_playerRef) is bool v) { inputAiming = v; hasInput = true; }
                }
            }
            catch { }

                // AimingActive 是权威，表示游戏此刻真的在瞄准。
                // IsAiming 只表示瞄准键按着 —— 翻滚时键没松但已退出瞄准姿态，用 OR 合并会让翻滚中挂着一条线。
            if (hasAim)
            {
                bool blocked = IsAimBlocked();
                bool result = aimActive && !blocked;

                if (!_aimStateLogged || _aimStateLast != result)
                {
                    _aimStateLogged = true;
                    _aimStateLast = result;
                    if (!result && blocked) CombatPlugin.Inst?.Log.LogInfo("[SPC] AimLine: AimBlocked=true（翻滚/受击等动作）→ 不显示");
                }
                return result;
            }

            // 读不到 AimingActive 才退到输入层
            if (hasInput) return inputAiming;

            if (!_aimingWarned) { _aimingWarned = true; CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: AimingActive / IsAiming 都读不到，视为一直瞄准"); }
            return true;
        }

        private static bool EnsureRenderer()
        {
            if (_lr != null) return true;
            try
            {
                var go = new UnityEngine.GameObject("SPT_AimLine");
                UnityEngine.Object.DontDestroyOnLoad(go);

                _lr = go.AddComponent<UnityEngine.LineRenderer>();
                if (_lr == null) return false;

                var sh = UnityEngine.Shader.Find("Sprites/Default");
                if (sh != null) _lr.material = new UnityEngine.Material(sh);
                else CombatPlugin.Inst?.Log.LogError("[SPC] AimLine: Shader.Find(\"Sprites/Default\") 返回 null，线可能不可见");

                // LineRenderer 有些属性不设会用它自己的默认材质/宽度，这里显式兜一遍
                try
                {
                    if (_lr.material == null)
                        CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: material 为 null（可能仍然可见，但颜色/透明度不可控）");
                }
                catch { }

                float w = CombatPlugin.CfgAimLineWidth?.Value ?? 0.007f;
                float g = CombatPlugin.CfgAimLineGray?.Value ?? 0.75f;
                var c = new UnityEngine.Color(g, g, g, 1f);

                _lr.useWorldSpace = true;
                _lr.positionCount = 2;
                _lr.startWidth = w;
                _lr.endWidth = w;
                _lr.startColor = c;
                _lr.endColor = c;
                _lr.sortingOrder = 30000;   // 盖在场景精灵上面
                _lr.enabled = true;          // 常开，用 positionCount 控制显隐
                _lr.positionCount = 0;

                CombatPlugin.Inst?.Log.LogInfo($"[SPC] AimLine renderer created (width={w}, gray={g})");
                return true;
            }
            catch (Exception e)
            {
                if (!_warned)
                {
                    _warned = true;
                    CombatPlugin.Inst?.Log.LogError("[SPC] AimLine EnsureRenderer failed: " + e);
                }
                return false;
            }
        }

        private static bool TryGetOriginAndDir(out Vector3 origin, out Vector3 dir)
        {
            origin = Vector3.zero;
            dir = Vector3.zero;

            if (!ResolveAimComp()) return false;

            // ================= 方向 =================
            // PreviousAimVector 是游戏的"上一帧瞄准向量"（public Vector2）——最直接的数据源。
            var av = CombatPatches.GetMemberTyped(_aimType, _aimComp, "PreviousAimVector");
            bool validDir = false;
            if (av is Vector2 v2)
            {
                float m = Mathf.Sqrt(v2.x * v2.x + v2.y * v2.y);
                if (m > 0.0001f) { dir = new Vector3(v2.x / m, v2.y / m, 0f); validDir = true; }
            }
            if (validDir != _dirWasValid)
            {
                _dirWasValid = validDir;
                if (validDir) CombatPlugin.Inst?.Log.LogInfo($"[SPC] AimLine: 方向来源=PreviousAimVector dir=({dir.x:0.00},{dir.y:0.00})");
                else CombatPlugin.Inst?.Log.LogWarning($"[SPC] AimLine: PreviousAimVector 无效 (实际={(av == null ? "null" : av.GetType().Name)}) → 用兜底方向");
            }

            // ================= 起点 =================
            // 起点 = 枪口：游戏自己的红线就是挂在武器 prefab 上、靠自身 transform 定位的。
            var muzzle = FindMuzzle();
            if (muzzle != null)
            {
                origin = muzzle.position;
                if (!_hadMuzzle)
                {
                    _hadMuzzle = true;
                    _originFrom = "枪口 m_muzzle";
                    CombatPlugin.Inst?.Log.LogInfo($"[SPC] AimLine: 起点=枪口 m_muzzle pos=({origin.x:0.00},{origin.y:0.00})");
                }
            }
            else
            {
                if (_hadMuzzle)
                {
                    _hadMuzzle = false;
                    CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: 枪口丢失（换空手/近战了？）→ 用兜底起点");
                }

                var aimTf = AsTransform(CombatPatches.GetMemberTyped(_aimType, _aimComp, "AimTransform"));
                if (aimTf == null) aimTf = AsTransform(CombatPatches.GetMemberTyped(_aimType, _aimComp, "ArmsTransform"));

                if (aimTf != null)
                {
                    origin = aimTf.position;
                    _originFrom = "瞄准枢轴 AimTransform";
                }
                else
                {
                    var ptf = _player.transform;
                    origin = new Vector3(ptf.position.x, ptf.position.y + 0.35f, ptf.position.z);
                    _originFrom = "玩家位置+0.35(兜底)";
                }
            }

            // 方向仍为空 → 枪口朝向 → 角色朝向
            if (!validDir)
            {
                if (muzzle != null)
                {
                    try
                    {
                        var r = muzzle.right;
                        float m2 = Mathf.Sqrt(r.x * r.x + r.y * r.y);
                        if (m2 > 0.0001f) dir = new Vector3(r.x / m2, r.y / m2, 0f);
                    }
                    catch { }
                }
                if (dir.sqrMagnitude < 0.0001f)
                {
                    float sx = 1f;
                    try { sx = _player.transform.localScale.x; } catch { }
                    dir = new Vector3(sx < 0f ? -1f : 1f, 0f, 0f);
                }
            }

            return true;
        }

        /// <summary>CharacterAiming.Weapon -> ProjectileWeapon.m_muzzle</summary>
        private static UnityEngine.Transform FindMuzzle()
        {
            try
            {
                var weapon = CombatPatches.GetMemberTyped(_aimType, _aimComp, "Weapon");
                if (weapon == null)
                {
                    if (!_weaponWasNull)
                    {
                        _weaponWasNull = true;
                        CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: CharacterAiming.Weapon 变 null（手持近战/空手）");
                    }
                    return null;
                }
                if (_weaponWasNull)
                {
                    _weaponWasNull = false;
                    CombatPlugin.Inst?.Log.LogInfo($"[SPC] AimLine: CharacterAiming.Weapon = {weapon.GetType().Name}");
                }

                if (_weaponType == null) _weaponType = AccessTools.TypeByName("ProjectileWeapon");
                if (_weaponType == null) return null;

                var raw = CombatPatches.GetMemberTyped(_weaponType, weapon, "m_muzzle");
                var tf = AsTransform(raw);
                if (tf == null)
                {
                    if (!_muzzleWarned)
                    {
                        _muzzleWarned = true;
                        CombatPlugin.Inst?.Log.LogWarning($"[SPC] AimLine: ProjectileWeapon.m_muzzle 读不到 (实际={(raw == null ? "null" : raw.GetType().Name)})");
                    }
                }
                return tf;
            }
            catch (Exception e)
            {
                if (!_muzzleWarned) { _muzzleWarned = true; CombatPlugin.Inst?.Log.LogWarning("[SPC] AimLine: FindMuzzle failed: " + e.Message); }
                return null;
            }
        }

        /// <summary>
        /// 一次性诊断：把玩家/瞄准组件的真实身份打出来。
        /// 只在"瞄准线该显示却没显示"时调用，且只打一次（换玩家会重置）。
        /// </summary>
        private static void Diag()
        {
            if (_diagDone) return;
            if (Time.unscaledTime < _nextDiag) return;
            _diagDone = true;
            _nextDiag = Time.unscaledTime + 30f;

            var log = CombatPlugin.Inst?.Log;
            try
            {
                string pname = "?", ppath = "?";
                try
                {
                    if (_player != null)
                    {
                        pname = _player.gameObject.name;
                        ppath = _player.gameObject.transform.root.name;
                    }
                }
                catch { }

                log?.LogInfo($"[SPC] ==== AimLine DIAG ====");
                log?.LogInfo($"[SPC] player=\"{pname}\" root=\"{ppath}\" id={_playerId}");
                log?.LogInfo($"[SPC] aimComp={(_aimComp == null ? "null" : "ok")}");

                if (_aimComp != null && _aimType != null)
                {
                    var w = CombatPatches.GetMemberTyped(_aimType, _aimComp, "Weapon");
                    var act = CombatPatches.GetMemberTyped(_aimType, _aimComp, "AimingActive");
                    var av = CombatPatches.GetMemberTyped(_aimType, _aimComp, "PreviousAimVector");
                    var at = CombatPatches.GetMemberTyped(_aimType, _aimComp, "AimTransform");
                    log?.LogInfo($"[SPC]   Weapon={(w == null ? "null" : w.GetType().Name)}  AimingActive={act}  PrevAimVec={av}  AimTransform={(at == null ? "null" : "ok")}");

                    // 真实类名（代理层 GetType() 不可信）
                    try
                    {
                        var baseObj = _aimComp as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;
                        if (baseObj != null && baseObj.Pointer != IntPtr.Zero)
                        {
                            IntPtr k = IL2CPP.il2cpp_object_get_class(baseObj.Pointer);
                            if (k != IntPtr.Zero)
                                log?.LogInfo($"[SPC]   realClass={System.Runtime.InteropServices.Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(k))}");
                        }
                    }
                    catch (Exception e) { log?.LogInfo("[SPC]   realClass read failed: " + e.Message); }
                }

                // 场景里一共有几个 CharacterAiming？分别在谁身上？
                try
                {
                    var all = UnityEngine.Object.FindObjectsOfType(Il2CppType.From(_aimType));
                    log?.LogInfo($"[SPC]   CharacterAiming instances in scene = {all.Length}");
                    int n = 0;
                    foreach (var c in all)
                    {
                        if (c == null) continue;
                        if (n++ >= 6) break;
                        try
                        {
                            var comp = c.TryCast<UnityEngine.Component>();
                            log?.LogInfo($"[SPC]     [{n}] on \"{comp.gameObject.name}\" root=\"{comp.gameObject.transform.root.name}\"");
                        }
                        catch { }
                    }
                }
                catch (Exception e) { log?.LogInfo("[SPC]   FindObjectsOfType failed: " + e.Message); }

                // 输入层自己的 IsAiming 是什么
                try
                {
                    if (_playerType != null && _player != null)
                    {
                        var p = _playerType.GetProperty("IsAiming",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        log?.LogInfo($"[SPC]   CharacterInputPlayer.IsAiming={(p == null ? "no prop" : p.GetValue(_player)?.ToString())}");
                    }
                }
                catch (Exception e) { log?.LogInfo("[SPC]   IsAiming read failed: " + e.Message); }

                log?.LogInfo($"[SPC] ==== DIAG end ====");
            }
            catch (Exception e) { log?.LogWarning("[SPC] AimLine Diag failed: " + e.Message); }
        }

        private static UnityEngine.Transform AsTransform(object o)
        {
            if (o == null) return null;
            try
            {
                var uo = o as UnityEngine.Object;
                if (uo == null) return null;
                return uo.TryCast<UnityEngine.Transform>();
            }
            catch { return null; }
        }
    }

    // ================= 手柄输入诊断 =================
    // 目的：查清"左摇杆为什么会改变瞄准角度"。
    // dump 只有类结构没有方法体（IL2CPP 没有 IL），读代码判断不了，只能运行时观测。
    // 采样点（CharacterInputPlayer，字段名来自 dump.cs）：
    //   m_movementInput     左摇杆（移动）
    //   m_gamepadRawAimInput 右摇杆（瞄准）
    //   m_aimDirection      合成后的瞄准方向
    //   m_mouseAimPosition  鼠标位置（键鼠模式才动）
    //   AimDirectionInput   属性：下游 CharacterAiming 实际消费的瞄准输入
    // 对照 CharacterAiming 的 m_aimInput / PreviousAimVector，看改动发生在哪一层。
    public static class GamepadDiag
    {
        private static Type _inType;                 // CharacterInputPlayer
        private static Type _aimType;                // CharacterAiming
        private static object _input;                // 重包装后的输入组件代理
        private static object _aim;                  // 重包装后的瞄准组件代理
        private static UnityEngine.Component _inputComp;
        private static int _id;
        private static float _next;
        private static float _nextRefresh;
        private static bool _typeWarned;
        private static string _last;
        private static float _lastPrint;
        private static Type _dirType;      // CharacterDirection（只读采样朝向）
        private static object _dirComp;

        public static void Tick(float interval)
        {
            var log = CombatPlugin.Inst?.Log;
            if (log == null) return;
            if (CombatPlugin.CfgGpDiag != null && !CombatPlugin.CfgGpDiag.Value) return;

            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + interval;

            if (!Resolve(log)) return;

            try
            {
                var mv = CombatPatches.GetMemberTyped(_inType, _input, "m_movementInput");
                var gp = CombatPatches.GetMemberTyped(_inType, _input, "m_gamepadRawAimInput");
                var ad = CombatPatches.GetMemberTyped(_inType, _input, "m_aimDirection");
                var mp = CombatPatches.GetMemberTyped(_inType, _input, "m_mouseAimPosition");
                var adi = CombatPatches.GetMemberTyped(_inType, _input, "AimDirectionInput");
                var isAim = CombatPatches.GetMemberTyped(_inType, _input, "IsAiming");
                var turn = CombatPatches.GetMemberTyped(_inType, _input, "m_turnDisabled");

                var aimIn = _aim == null ? null : CombatPatches.GetMemberTyped(_aimType, _aim, "m_aimInput");
                var prev = _aim == null ? null : CombatPatches.GetMemberTyped(_aimType, _aim, "PreviousAimVector");

                // 只读采样角色朝向：判断"瞄准时把 move 置零"之后，朝向还会不会跟着瞄准转
                string face = "?";
                try
                {
                    if (_dirType == null) _dirType = AccessTools.TypeByName("CharacterDirection");
                    if (_dirType != null && _inputComp != null)
                    {
                        if (_dirComp == null)
                        {
                            var rd = _inputComp.gameObject.GetComponent(Il2CppType.From(_dirType));
                            if (rd != null) _dirComp = CombatPatches.Rewrap(_dirType, rd);
                        }
                        var fv = CombatPatches.GetMemberTyped(_dirType, _dirComp, "CurrentDirection");
                        if (fv != null)
                        {
                            int fi = Convert.ToInt32(fv);
                            face = fi == 1 ? "R" : fi == 2 ? "L" : "0";
                        }
                    }
                }
                catch { }

                string line = "[SPC][GP] move=" + Vs(mv) + " rawAim=" + Vs(gp) + " aimDir=" + Vs(ad)
                            + " mouse=" + Vs(mp) + " | AimDirInput=" + Vs(adi) + " IsAiming=" + isAim
                            + " turnDis=" + turn + " face=" + face
                            + " | aim.m_aimInput=" + Vs(aimIn) + " PrevAimVec=" + Vs(prev);

                // 值没变化就别刷屏；但每 5 秒至少打一次，证明还活着
                if (line == _last && Time.unscaledTime - _lastPrint < 5f) return;
                _last = line;
                _lastPrint = Time.unscaledTime;
                log.LogInfo(line);
            }
            catch (Exception e) { log.LogWarning("[SPC][GP] diag failed: " + e.Message); }
        }

        private static bool Resolve(BepInEx.Logging.ManualLogSource log)
        {
            try
            {
                if (_inType == null) _inType = AccessTools.TypeByName("CharacterInputPlayer");
                if (_aimType == null) _aimType = AccessTools.TypeByName("CharacterAiming");
                if (_inType == null)
                {
                    if (!_typeWarned)
                    {
                        _typeWarned = true;
                        log?.LogWarning("[SPC][GP] CharacterInputPlayer 类型没找到");
                    }
                    return false;
                }

                if (_input != null && Time.unscaledTime < _nextRefresh) return true;
                _nextRefresh = Time.unscaledTime + 3f;

                var o = UnityEngine.Object.FindFirstObjectByType(Il2CppType.From(_inType));
                if (o == null) return false;

                int id = o.GetInstanceID();
                if (_input == null || id != _id)
                {
                    _inputComp = o.TryCast<UnityEngine.Component>();
                    _input = Rewrap(_inType, o);
                    _aim = null;
                    _id = id;
                    _last = null;
                    log?.LogInfo("[SPC][GP] 绑定玩家输入组件 id=" + id
                        + " on=" + (_inputComp == null ? "?" : _inputComp.gameObject.name));
                }

                if (_aim == null && _aimType != null && _inputComp != null)
                {
                    object raw = _inputComp.gameObject.GetComponent(Il2CppType.From(_aimType));
                    if (raw == null) raw = _inputComp.gameObject.GetComponentInChildren(Il2CppType.From(_aimType), true);
                    if (raw != null) _aim = Rewrap(_aimType, raw);
                }
                return _input != null;
            }
            catch (Exception e)
            {
                log?.LogWarning("[SPC][GP] resolve failed: " + e.Message);
                return false;
            }
        }

        /// <summary>按真实类型重新包装代理：IL2CPP 代理的 GetType() 返回基类，直接反射读不到游戏类字段</summary>
        private static object Rewrap(Type t, object o) => CombatPatches.Rewrap(t, o);

        private static string Vs(object o)
        {
            if (o == null) return "null";
            try
            {
                if (o is UnityEngine.Vector2 v2) return "(" + v2.x.ToString("0.00") + "," + v2.y.ToString("0.00") + ")";
                if (o is UnityEngine.Vector3 v3) return "(" + v3.x.ToString("0.00") + "," + v3.y.ToString("0.00") + "," + v3.z.ToString("0.00") + ")";
            }
            catch { }
            return o.ToString();
        }
    }

    // ================= 背包扩容 =================
    //
    // 数据层（dump 实测）：
    //   PlayerMainInventory : Inventory（ScriptableObject，不是场景组件）
    //     m_baseInventorySize  (Vector2Int)          基础尺寸（宽×高）
    //     m_inventoryUpgrades  (List<ItemInstance>)  已购买的扩容道具
    //     InventorySize        (override get)        实际尺寸——运行时算出来的，不一定直接等于 base
    //     IsSlotLocked(pos)    (override)            某个格子是否锁着
    //   InventoryUpgradeItemDefinition.m_addedSlots = 每个升级道具加多少格
    //
    // UI 层：InventoryPanel 有 m_inventoryItemCellPrefab + m_expandingBackground + m_unlockFlashEffectTemplate
    //   ⇒ 格子是动态生成的（还专门有"解锁新格子"的闪光动效），所以容量变了有机会跟着长 —— 待实测。
    //
    // 获取路径：玩家 GameObject 上的 CharacterInventory.Inventory
    public static class BagTweak
    {
        private static Type _charInvType;   // CharacterInventory
        private static Type _invType;       // PlayerMainInventory
        private static object _inv;         // PlayerMainInventory 代理
        private static float _nextResolve;
        private static string _info = "背包：未解析";
        private static bool _warned;

        public static string Info()
        {
            Resolve();
            return _info;
        }

        public static void Resolve()
        {
            if (Time.unscaledTime < _nextResolve) return;
            _nextResolve = Time.unscaledTime + 2f;
            try
            {
                if (_charInvType == null) _charInvType = AccessTools.TypeByName("CharacterInventory");
                if (_invType == null) _invType = AccessTools.TypeByName("PlayerMainInventory");
                if (_charInvType == null || _invType == null)
                {
                    if (!_warned) { _warned = true; CombatPlugin.Inst?.Log.LogWarning("[SPC] bag: 类型没找到"); }
                    _info = "背包：类型未找到";
                    return;
                }

                var o = UnityEngine.Object.FindFirstObjectByType(Il2CppType.From(_charInvType));
                if (o == null) { _info = "背包：玩家未就绪"; return; }

                var proxy = CombatPatches.Rewrap(_charInvType, o);
                var invObj = CombatPatches.GetMemberTyped(_charInvType, proxy, "Inventory");
                if (invObj == null) { _info = "背包：Inventory 读不到"; return; }

                _inv = CombatPatches.Rewrap(_invType, invObj);

                var size = CombatPatches.GetMemberTyped(_invType, _inv, "InventorySize");
                var bs = CombatPatches.GetMemberTyped(_invType, _inv, "m_baseInventorySize");
                _info = "尺寸 " + V2I(size) + "  基础 " + V2I(bs);
            }
            catch (Exception e) { _info = "背包：解析异常 " + e.Message; }
        }

        /// <summary>把基础尺寸加 rows 行，并打出 InventorySize 的前后变化（用于判断 UI/逻辑是否跟着变）</summary>
        public static void AddRows(int rows)
        {
            try
            {
                Resolve();
                if (_inv == null) { Log("扩容失败：背包未解析"); return; }

                var bsV = CombatPatches.GetMemberTyped(_invType, _inv, "m_baseInventorySize");
                if (!(bsV is Vector2Int bs))
                {
                    Log("扩容失败：m_baseInventorySize 类型不符 " + (bsV == null ? "null" : bsV.GetType().Name));
                    return;
                }

                var before = CombatPatches.GetMemberTyped(_invType, _inv, "InventorySize");
                var nb = new Vector2Int(bs.x, bs.y + rows);
                bool ok = CombatPatches.SetMemberTyped(_invType, _inv, "m_baseInventorySize", nb);
                _nextResolve = 0f;   // 立刻重算 Info
                var after = CombatPatches.GetMemberTyped(_invType, _inv, "InventorySize");

                Log($"扩容尝试: base {V2I(bs)} → {V2I(nb)}（写入={ok}）| InventorySize {V2I(before)} → {V2I(after)}");
            }
            catch (Exception e) { Log("扩容异常: " + e.Message); }
        }

        // ---------- 配置驱动的自动应用 ----------
        // 用【增量】而不是绝对基准：InventorySize = base + 已购升级道具，
        // 所以绝对基准会把玩家后来正常买到的扩容道具覆盖掉。
        private static int _appliedRows = int.MinValue;   // 上次应用到的行数（MinValue = 还没应用过）
        private static string _lastMsg = "";

        public static void Tick()
        {
            var cfg = CombatPlugin.CfgBagExtraRows;
            if (cfg == null) return;

            Resolve();
            if (_inv == null) return;

            int want = Mathf.Clamp(cfg.Value, 0, 10);
            if (_appliedRows == int.MinValue) _appliedRows = 0;   // 首次视为"已应用 0 行"
            int delta = want - _appliedRows;
            if (delta == 0) return;                                // 配置没变 ⇒ 不碰，避免覆盖游戏内正常扩容

            try
            {
                var bsV = CombatPatches.GetMemberTyped(_invType, _inv, "m_baseInventorySize");
                if (!(bsV is Vector2Int bs)) return;

                // ---- 缩容保护 ----
                // 缩容会让"超出新容量"的物品留在非法位置。游戏 UI 在移动/旋转这类物品时会卡死：
                // 目标位置越界 ⇒ 移动状态机无法退出 ⇒ 之后所有按键（含 ESC）全部无响应，只能大退。
                // 挡在操作之前，而不是事后警告玩家。
                if (delta < 0)
                {
                    var sizeV = CombatPatches.GetMemberTyped(_invType, _inv, "InventorySize");
                    int targetRows = (sizeV is Vector2Int sz ? sz.y : 0) + delta;
                    int bad = CountItemsBeyondRow(targetRows);
                    if (bad != 0)
                    {
                        // bad < 0：物品列表读不到 ⇒ 同样拒绝（宁可不让缩，也不制造卡死状态）
                        string msg = bad < 0
                            ? "无法读取物品列表，已取消缩容"
                            : $"额外行还有 {bad} 件物品，先清空再缩容";
                        if (_lastMsg != msg)   // 只在变化时打日志，否则每次 Tick 都会重试并刷屏
                        {
                            _lastMsg = msg;
                            Log("[缩容被拒绝] " + msg);
                        }
                        // 把配置值回滚到"实际生效的行数"：否则面板会显示一个没落地的数字，
                        // 玩家看到"额外 1 行"但实际还是 3 行，会以为功能坏了。
                        if (cfg.Value != _appliedRows)
                        {
                            try { cfg.Value = _appliedRows; } catch { }
                        }
                        return;   // 注意：不动 _appliedRows，等玩家清空后再点
                    }
                }

                var target = new Vector2Int(bs.x, Mathf.Max(1, bs.y + delta));
                if (CombatPatches.SetMemberTyped(_invType, _inv, "m_baseInventorySize", target))
                {
                    _appliedRows = want;
                    _lastMsg = "";
                    _nextResolve = 0f;
                    Log($"应用配置 {want} 行: base {V2I(bs)} → {V2I(target)}");
                }
            }
            catch (Exception e) { Log("自动应用异常: " + e.Message); }
        }

        /// <summary>
        /// 统计"占用行超出 targetRows"的物品数量。
        /// 返回 -1 = 读不到（调用方应保守处理）。
        /// 物品占位：ItemInstance.m_itemGridPosition + 尺寸（旋转时宽高互换），多格物品按整个占位算。
        /// </summary>
        private static int CountItemsBeyondRow(int targetRows)
        {
            try
            {
                if (targetRows < 1) return -1;

                object items = CombatPatches.GetMemberTyped(_invType, _inv, "m_items");
                if (items == null) return -1;

                var cV = CombatPatches.GetMember(items, "Count");
                if (cV == null) return -1;
                int count = Convert.ToInt32(cV);

                var listType = items.GetType();
                var idx = listType.GetProperty("Item",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (idx == null) return -1;

                var itemType = AccessTools.TypeByName("ItemInstance");
                var defType = AccessTools.TypeByName("ItemDefinition");
                if (itemType == null || defType == null) return -1;

                int bad = 0;
                for (int i = 0; i < count; i++)
                {
                    object raw;
                    try { raw = idx.GetValue(items, new object[] { i }); }
                    catch { return -1; }
                    if (raw == null) continue;

                    var item = CombatPatches.Rewrap(itemType, raw);
                    if (item == null) continue;

                    var posV = CombatPatches.GetMemberTyped(itemType, item, "m_itemGridPosition");
                    if (!(posV is Vector2Int pos)) continue;

                    var rotV = CombatPatches.GetMemberTyped(itemType, item, "m_itemRotated");
                    bool rotated = rotV is bool rb && rb;

                    var defV = CombatPatches.GetMemberTyped(itemType, item, "m_itemDefinition");
                    if (defV == null) continue;
                    var def = CombatPatches.Rewrap(defType, defV);
                    var sizeV = CombatPatches.GetMemberTyped(defType, def, "m_itemSize");
                    if (!(sizeV is Vector2Int size)) continue;

                    int h = rotated ? size.x : size.y;      // 旋转时宽高互换
                    if (h < 1) h = 1;
                    if (pos.y + h > targetRows) bad++;
                }
                return bad;
            }
            catch { return -1; }
        }

        /// <summary>面板按钮：直接改配置值，Tick 会负责落地</summary>
        public static void ChangeRows(int delta)
        {
            var cfg = CombatPlugin.CfgBagExtraRows;
            if (cfg == null) return;
            int v = Mathf.Clamp(cfg.Value + delta, 0, 10);
            if (v != cfg.Value) cfg.Value = v;
        }

        public static void Dump()
        {
            try
            {
                Resolve();
                if (_inv == null) { Log("dump: 背包未解析"); return; }

                var size = CombatPatches.GetMemberTyped(_invType, _inv, "InventorySize");
                var bs = CombatPatches.GetMemberTyped(_invType, _inv, "m_baseInventorySize");
                var ups = CombatPatches.GetMemberTyped(_invType, _inv, "m_inventoryUpgrades");
                var cnt = ups == null ? null : CombatPatches.GetMember(ups, "Count");
                var lastRow = CombatPatches.GetMemberTyped(_invType, _inv, "GetAvailableLastRowSlots");

                Log($"dump: InventorySize={V2I(size)} base={V2I(bs)} upgrades={cnt ?? "?"} lastRowSlots={lastRow ?? "?"}");
            }
            catch (Exception e) { Log("dump 异常: " + e.Message); }
        }

        private static void Log(string msg) => CombatPlugin.Inst?.Log.LogInfo("[SPC][BAG] " + msg);

        public static string LastMsg => _lastMsg;

        private static string V2I(object o)
        {
            if (o is Vector2Int v) return v.x + "×" + v.y;
            return o == null ? "null" : o.ToString();
        }
    }

    // ================= 无敌帧浅蓝滤镜 =================
    //
    // 状态源：DamageBlock.m_damageBlockActive —— 闪避动画的帧事件
    //   调 AnimationEventsHelper.EnableInvincibilityFrames() → DamageBlock.ActivateDamageBlock()
    // 做法：无敌时把角色身上所有 SpriteRenderer 的 color 乘一个浅蓝 tint，结束时逐项还原。
    //   2D 精灵最省事的方式 —— color 本来就是 SpriteRenderer 的 tint，不用换材质、不用 shader。
    // ⚠️ 只能知道"在不在无敌"，游戏没暴露剩余时长 ⇒ 做不了倒计时，只能做整段显示。
    public static class IFrameTint
    {
        private static Type _blockType;
        private static object _block;
        private static int _playerGo;
        private static float _nextResolve;
        private static bool _tinted;
        private static bool _logged;

        private static readonly System.Collections.Generic.List<UnityEngine.SpriteRenderer> _srs
            = new System.Collections.Generic.List<UnityEngine.SpriteRenderer>();
        private static readonly System.Collections.Generic.List<Color> _orig
            = new System.Collections.Generic.List<Color>();

        // 亮青蓝：R 压到 0.35 让青味出来，G/B 保持高位 ⇒ 高饱和、不发灰，在暗色调场景里最扎眼。
        // 注意：这是【乘法】tint，值越接近 1 越接近原色（越淡越看不见），
        // 所以"显眼"要靠压低 R 拉高对比，而不是把三个通道都往 1 抬。
        private static readonly Color Tint = new Color(0.35f, 0.92f, 1f, 1f);

        public static void Tick()
        {
            var cfg = CombatPlugin.CfgIFrameTint;
            if (cfg == null || !cfg.Value) { Restore(); return; }

            Resolve();
            if (_block == null) { Restore(); return; }

            bool active = false;
            try
            {
                var v = CombatPatches.GetMemberTyped(_blockType, _block, "m_damageBlockActive");
                active = v is bool b && b;
            }
            catch { Restore(); return; }

            if (active) Apply(); else Restore();
        }

        private static void Resolve()
        {
            if (Time.unscaledTime < _nextResolve) return;
            _nextResolve = Time.unscaledTime + 1f;

            try
            {
                if (_blockType == null) _blockType = AccessTools.TypeByName("DamageBlock");
                if (_blockType == null) return;

                var inputType = AccessTools.TypeByName("CharacterInputPlayer");
                if (inputType == null) return;

                var po = UnityEngine.Object.FindFirstObjectByType(Il2CppType.From(inputType));
                if (po == null) { _block = null; return; }

                var comp = po.TryCast<UnityEngine.Component>();
                if (comp == null) return;

                int goId = comp.gameObject.GetInstanceID();
                if (_playerGo == goId && _block != null) return;

                object raw = comp.gameObject.GetComponent(Il2CppType.From(_blockType));
                if (raw == null) raw = comp.gameObject.GetComponentInChildren(Il2CppType.From(_blockType), true);
                if (raw == null) { _block = null; return; }

                _block = CombatPatches.Rewrap(_blockType, raw);
                _playerGo = goId;
                CacheRenderers(comp);
            }
            catch { }
        }

        /// <summary>缓存玩家身上所有 SpriteRenderer 及其原始颜色（身体/手臂/头发是分开的）</summary>
        private static void CacheRenderers(UnityEngine.Component playerComp)
        {
            Restore();                 // 先还原旧的，避免换角色时把蓝 tint 留在上一具身体上
            _srs.Clear();
            _orig.Clear();
            try
            {
                var arr = playerComp.gameObject.GetComponentsInChildren(
                    Il2CppType.From(typeof(UnityEngine.SpriteRenderer)), true);
                if (arr == null) return;

                foreach (var c in arr)
                {
                    var sr = c.TryCast<UnityEngine.SpriteRenderer>();
                    if (sr == null) continue;
                    _srs.Add(sr);
                    _orig.Add(sr.color);
                }
            }
            catch { }
        }

        private static void Apply()
        {
            if (_tinted) return;
            _tinted = true;
            if (!_logged)
            {
                _logged = true;
                CombatPlugin.Inst?.Log.LogInfo($"[SPC] i-frame tint 生效: tinted {_srs.Count} 个 SpriteRenderer");
            }
            for (int i = 0; i < _srs.Count; i++)
            {
                try
                {
                    var o = _orig[i];
                    _srs[i].color = new Color(o.r * Tint.r, o.g * Tint.g, o.b * Tint.b, o.a);
                }
                catch { }
            }
        }

        private static void Restore()
        {
            if (!_tinted) return;
            _tinted = false;
            for (int i = 0; i < _srs.Count; i++)
            {
                try { _srs[i].color = _orig[i]; } catch { }
            }
        }
    }

    // ================= 运行时：热键 + 面板 =================
    // 面板字符串表。日志不翻：那是排障用的，翻了反而不好搜。
    public static class L10n
    {
        public static string Lang = "zh";

        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>
        {
            { "title",           new[]{ "战斗性能优化",          "Combat tweaks" } },
            { "hide",            new[]{ "F7 显隐",               "F7 toggle" } },
            { "tab.combat",      new[]{ "战斗",                  "Combat" } },
            { "tab.aim",         new[]{ "瞄准",                  "Aim" } },
            { "tab.cheat",       new[]{ "作弊",                  "Cheats" } },

            { "dodge",           new[]{ "闪避取消攻击",          "Dodge cancels attacks" } },
            { "stomp",           new[]{ "前踏吸附",              "Stomp snap" } },
            { "stomp.range",     new[]{ "吸附半径 {0:0.0} m",    "Snap radius {0:0.0} m" } },
            { "stomp.stop",      new[]{ "停留距离 {0:0.00} m（按敌人宽度自适应）", "Stop distance {0:0.00} m (auto by enemy width)" } },
            { "stomp.dur",       new[]{ "位移时长 {0:0.000} s（0=瞬移）", "Lunge time {0:0.000} s (0 = teleport)" } },
            { "drawboxes",       new[]{ "绘制判定框",            "Draw hitboxes" } },

            { "aim.enable",      new[]{ "显示瞄准线",            "Show aim line" } },
            { "aim.len",         new[]{ "线长 {0:0.00} m",       "Length {0:0.00} m" } },
            { "aim.width",       new[]{ "线宽 {0:0.000} m",      "Width {0:0.000} m" } },
            { "aim.gray",        new[]{ "灰度 {0:0.00}（0黑 1白）", "Gray {0:0.00} (0 black, 1 white)" } },
            { "aim.delay",       new[]{ "抬起延迟 {0:0.00} s",   "Raise delay {0:0.00} s" } },
            { "aim.movelock",    new[]{ "瞄准时忽略移动输入（左摇杆/WASD）", "Ignore movement input while aiming (stick/WASD)" } },
            { "ifr.tint",        new[]{ "显示闪避无敌帧",         "Show dodge i-frames" } },
            { "tab.bag",         new[]{ "背包",                    "Bag" } },
            { "bag.rows",        new[]{ "额外行数 {0}",            "Extra rows {0}" } },
            { "bag.sub",         new[]{ "− 1 行",                  "− 1 row" } },
            { "bag.add",         new[]{ "+ 1 行",                  "+ 1 row" } },
            { "bag.dump",        new[]{ "打印详情到日志",          "Dump to log" } },
            { "god",             new[]{ "无敌",                  "Invincible" } },
            { "ammo",            new[]{ "无限弹药",              "Infinite ammo" } },
            { "weak",            new[]{ "全程弱点命中",          "Always weakpoint" } },
            { "stamina",         new[]{ "无限体力",              "Infinite stamina" } },
            { "blind",           new[]{ "AI 失明",               "AI oblivious" } },
            { "slow.on",         new[]{ "慢动作 {0:0.00}x  (F6 关)", "Slow-mo {0:0.00}x  (F6 off)" } },
            { "slow.off",        new[]{ "慢动作 off  (F6 开)",   "Slow-mo off  (F6 on)" } },
        };

        public static string T(string key)
        {
            string[] v;
            if (!Map.TryGetValue(key, out v)) return key;
            return Lang == "en" ? v[1] : v[0];
        }

        public static string F(string key, params object[] args)
        {
            try { return string.Format(T(key), args); }
            catch { return T(key); }
        }
    }

    public class CombatBehaviour : MonoBehaviour
    {
        private bool _show = true;
        private bool _slow;
        private int _tab;   // 0=战斗 1=瞄准 2=作弊
        private float _nextApply;
        private bool _legacyInputOk = true;

        // 只在值变化时写原生内存；从没写过 + 目标 false 就不写，避免关掉游戏自己解锁的作弊。
        private readonly Dictionary<string, bool> _applied = new Dictionary<string, bool>();
        private float _appliedTimeScale = float.NaN;

        // 前踏匀速位移状态（不用协程：IL2CPP 的 StartCoroutine 只吃 Il2Cpp 的 IEnumerator）
        private static UnityEngine.Transform _lungeTf;
        private static Vector3 _lungeFrom;
        private static Vector3 _lungeTo;
        private static float _lungeT;
        private static float _lungeDur;

        private void Start()
        {
            CombatPlugin.Inst?.Log.LogInfo("[SPC] HUD attached");
        }

        /// <summary>
        /// 必须放 LateUpdate：枪口是游戏自己在 Update 里摆好的，
        /// Update 里读到的是上一帧姿态 ⇒ 刚开始瞄准会看到线在旧位置闪一下。
        /// </summary>
        private void LateUpdate()
        {
            AimLine.Tick();
            CombatPatches.AimLockTickField();   // 手柄瞄准锁定（getter patch 的兜底）
        }

        private void Update()
        {
            TickLunge();                              // 前踏位移每帧推进
            GamepadDiag.Tick(0.2f);                   // 手柄输入采样（Debug.GamepadInputLog 控制）
            BagTweak.Tick();                          // 背包扩容（按配置增量应用）
            IFrameTint.Tick();                        // 无敌帧浅蓝滤镜

            if (Time.unscaledTime >= _nextApply)
            {
                _nextApply = Time.unscaledTime + 0.25f;
                ApplyAll();
            }

            try
            {
                if (_legacyInputOk && UnityEngine.Input.GetKeyDown(KeyCode.F6)) ToggleSlow();
            }
            catch
            {
                _legacyInputOk = false;
                CombatPlugin.Inst?.Log.LogWarning("[SPC] legacy Input unavailable, using IMGUI events only");
            }
        }

        private static void TickLunge()
        {
            var tf = _lungeTf;
            if (tf == null) return;

            _lungeT += Time.deltaTime;
            float a = _lungeDur <= 0.0001f ? 1f : _lungeT / _lungeDur;
            if (a > 1f) a = 1f;

            try { tf.position = Vector3.Lerp(_lungeFrom, _lungeTo, a); }
            catch { _lungeTf = null; return; }

            if (a >= 1f)
            {
                _lungeTf = null;
                try { CombatPlugin.Inst?.Log.LogInfo($"[SPC] stomp-lunge done: pos={tf.position} ({_lungeDur:0.000}s, 匀速)"); } catch { }
            }
        }

        /// <summary>duration 秒内匀速从 from 推到 to。线性不用 easing：缓动末段快，像被吸过去。</summary>
        public static void StartLunge(UnityEngine.Transform tf, Vector3 from, Vector3 to, float duration)
        {
            if (tf == null) return;
            if (duration <= 0.001f)
            {
                try { tf.position = to; } catch { }
                CombatPlugin.Inst?.Log.LogInfo($"[SPC] stomp-lunge done: pos={to} (0.000s, 瞬移)");
                return;
            }
            _lungeTf = tf;
            _lungeFrom = from;
            _lungeTo = to;
            _lungeT = 0f;
            _lungeDur = duration;
            CombatPlugin.Inst?.Log.LogInfo("[SPC] stomp-lunge: coroutine started (位移进行中)");
        }

        private void ApplyAll()
        {
            if (CombatPlugin.Inst == null) return;

            SetOnce("GameDebugCommands", "CHEAT_INVINCIBLE", CombatPlugin.CfgInvincible.Value);
            SetOnce("GameDebugCommands", "CHEAT_INFINITE_AMMO", CombatPlugin.CfgAmmo.Value);
            SetOnce("GameDebugCommands", "CHEAT_INFINITE_STAMINA", CombatPlugin.CfgStamina.Value);
            SetOnce("GameDebugCommands", "ALWAYS_WEAKPOINT_HIT", CombatPlugin.CfgWeakpoint.Value);
            SetOnce("GameDebugDrawer", "DRAW_DAMAGE_COLLIDERS", CombatPlugin.CfgDrawBoxes.Value);

            SetOnce("AISenses", "s_aiOblivious", CombatPlugin.CfgAiBlind.Value);

            float want = _slow ? (CombatPlugin.CfgSlowMo?.Value ?? 0.35f) : 1f;
            if (!_appliedTimeScale.Equals(want))
            {
                Time.timeScale = want;
                _appliedTimeScale = want;
            }
        }

        private void SetOnce(string typeName, string fieldName, bool value)
        {
            string key = typeName + "." + fieldName;

            bool written = _applied.TryGetValue(key, out var prev);
            if (written && prev == value) return;   // 值没变，一次原生调用都不发

            // 从没写过 + 目标 false ⇒ 不去碰游戏状态（否则会关掉游戏自己解锁的作弊）
            if (!written && !value) { _applied[key] = false; return; }

            if (CombatPlugin.SetStaticBool(typeName, fieldName, value)) _applied[key] = value;
        }

        private void ToggleSlow() { _slow = !_slow; }

        private void Toggle(BepInEx.Configuration.ConfigEntry<bool> cfg)
        {
            if (cfg != null) cfg.Value = !cfg.Value;
        }

        private void HandleKeys()
        {
            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.F1: Toggle(CombatPlugin.CfgInvincible); e.Use(); break;
                case KeyCode.F2: Toggle(CombatPlugin.CfgAmmo); e.Use(); break;
                case KeyCode.F3: Toggle(CombatPlugin.CfgWeakpoint); e.Use(); break;
                case KeyCode.F4: Toggle(CombatPlugin.CfgStamina); e.Use(); break;
                case KeyCode.F5: Toggle(CombatPlugin.CfgAiBlind); e.Use(); break;
                case KeyCode.F6: ToggleSlow(); e.Use(); break;
                case KeyCode.F7: _show = !_show; e.Use(); break;
            }
        }

        private void Row(BepInEx.Configuration.ConfigEntry<bool> cfg, KeyCode key, string label)
        {
            if (cfg == null) return;
            string prefix = key == KeyCode.None ? "      " : key.ToString() + "  ";
            bool v = GUILayout.Toggle(cfg.Value, prefix + label);
            if (v != cfg.Value) cfg.Value = v;
        }

        private static readonly string[] TabKeys = { "tab.combat", "tab.aim", "tab.bag", "tab.cheat" };
        private bool _guiErrorLogged;

        private void OnGUI()
        {
            HandleKeys();

            if (!_show) return;

            float w = L10n.Lang == "en" ? 400f : 350f;
            var r = new Rect(12, 12, w, 600);
            GUI.Box(r, L10n.T("title") + "   (" + L10n.T("hide") + ")");
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 26, r.width - 20, r.height - 34));

            // 不能用 GUILayout.Toolbar：它内部走 GUIContent.Temp(Il2CppStringArray)，该方法被 IL2CPP 裁剪掉，
            // 运行时抛 NotSupportedException，且异常夹在 BeginArea/EndArea 之间会让整个面板消失。
            // 只用游戏自己也在用的 IMGUI API，外加 try/finally 兜底。
            try
            {
                DrawTabBar();
                GUILayout.Space(6);

                switch (_tab)
                {
                    case 0: TabCombat(); break;
                    case 1: TabAim(); break;
                    case 2: TabBag(); break;
                    default: TabCheats(); break;
                }
            }
            catch (Exception e)
            {
                if (!_guiErrorLogged)
                {
                    _guiErrorLogged = true;
                    CombatPlugin.Inst?.Log.LogError("[SPC] OnGUI failed: " + e);
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void DrawTabBar()
        {
            // 选中态用方括号标记，不用 GUI.backgroundColor（未验证的属性，被裁剪掉会再黑一次面板）。
            GUILayout.BeginHorizontal();
            for (int i = 0; i < TabKeys.Length; i++)
            {
                string name = L10n.T(TabKeys[i]);
                string text = (i == _tab) ? "[" + name + "]" : name;
                if (GUILayout.Button(text))
                {
                    _tab = i;
                    _guiErrorLogged = false;
                }
            }
            if (GUILayout.Button(L10n.Lang == "zh" ? "EN" : "中文"))
            {
                L10n.Lang = L10n.Lang == "zh" ? "en" : "zh";
                if (CombatPlugin.CfgLanguage != null) CombatPlugin.CfgLanguage.Value = L10n.Lang;
            }
            GUILayout.EndHorizontal();
        }

        // ---------------- 战斗 ----------------
        private void TabCombat()
        {
            Row(CombatPlugin.CfgDodgeCancel, KeyCode.None, L10n.T("dodge"));
            Row(CombatPlugin.CfgIFrameTint, KeyCode.None, L10n.T("ifr.tint"));

            GUILayout.Space(8);
            Row(CombatPlugin.CfgStompLunge, KeyCode.None, L10n.T("stomp"));

            bool prev = GUI.enabled;
            GUI.enabled = CombatPlugin.CfgStompLunge.Value;

            GUILayout.Label(L10n.F("stomp.range", CombatPlugin.CfgStompLungeRange.Value));
            CombatPlugin.CfgStompLungeRange.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgStompLungeRange.Value, 0.5f, 4f);

            GUILayout.Label(L10n.F("stomp.stop", CombatPlugin.CfgStompStopDistance.Value));
            CombatPlugin.CfgStompStopDistance.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgStompStopDistance.Value, 0.02f, 0.6f);

            GUILayout.Label(L10n.F("stomp.dur", CombatPlugin.CfgStompLungeDuration.Value));
            CombatPlugin.CfgStompLungeDuration.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgStompLungeDuration.Value, 0f, 0.4f);
            GUI.enabled = prev;

            GUILayout.Space(8);
            Row(CombatPlugin.CfgDrawBoxes, KeyCode.None, L10n.T("drawboxes"));
        }

        // ---------------- 瞄准 ----------------
        private void TabAim()
        {
            Row(CombatPlugin.CfgAimLineEnabled, KeyCode.None, L10n.T("aim.enable"));

            GUILayout.Space(8);
            GUILayout.Label(L10n.F("aim.len", CombatPlugin.CfgAimLineLength.Value));
            CombatPlugin.CfgAimLineLength.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgAimLineLength.Value, 0.5f, 10f);

            GUILayout.Label(L10n.F("aim.width", CombatPlugin.CfgAimLineWidth.Value));
            CombatPlugin.CfgAimLineWidth.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgAimLineWidth.Value, 0.002f, 0.05f);

            GUILayout.Label(L10n.F("aim.gray", CombatPlugin.CfgAimLineGray.Value));
            CombatPlugin.CfgAimLineGray.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgAimLineGray.Value, 0f, 1f);

            GUILayout.Label(L10n.F("aim.delay", CombatPlugin.CfgAimLineShowDelay.Value));
            CombatPlugin.CfgAimLineShowDelay.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgAimLineShowDelay.Value, 0f, 0.5f);

            GUILayout.Space(10);
            Row(CombatPlugin.CfgAimLockLeftStick, KeyCode.None, L10n.T("aim.movelock"));
        }

        private void TabBag()
        {
            GUILayout.Label(BagTweak.Info());

            GUILayout.Space(8);
            GUILayout.Label(L10n.F("bag.rows", CombatPlugin.CfgBagExtraRows.Value));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L10n.T("bag.sub"))) BagTweak.ChangeRows(-1);
            if (GUILayout.Button(L10n.T("bag.add"))) BagTweak.ChangeRows(1);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button(L10n.T("bag.dump"))) BagTweak.Dump();

            // 缩容被拒绝时把原因显示出来（否则按钮点了没反应，玩家会以为坏了）
            if (!string.IsNullOrEmpty(BagTweak.LastMsg))
            {
                GUILayout.Space(6);
                GUILayout.Label("⚠ " + BagTweak.LastMsg);
            }
        }

        // ---------------- 作弊 ----------------
        private void TabCheats()
        {
            Row(CombatPlugin.CfgInvincible, KeyCode.F1, L10n.T("god"));
            Row(CombatPlugin.CfgAmmo, KeyCode.F2, L10n.T("ammo"));
            Row(CombatPlugin.CfgWeakpoint, KeyCode.F3, L10n.T("weak"));
            Row(CombatPlugin.CfgStamina, KeyCode.F4, L10n.T("stamina"));
            Row(CombatPlugin.CfgAiBlind, KeyCode.F5, L10n.T("blind"));

            GUILayout.Space(8);
            GUILayout.Label(_slow ? L10n.F("slow.on", CombatPlugin.CfgSlowMo.Value) : L10n.T("slow.off"));
            CombatPlugin.CfgSlowMo.Value = GUILayout.HorizontalSlider(CombatPlugin.CfgSlowMo.Value, 0.05f, 1f);
        }

    }
}
