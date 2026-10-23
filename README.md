# Silver Pines Combat

[English](dist/README.en.md) | 中文

给《银松镇 / Silver Pines》（Steam AppID 2333000）做的战斗手感改造 mod。Unity 6 + IL2CPP，走 BepInEx 6 Bleeding Edge + Harmony，**全部改动在运行时内存里完成，不碰任何游戏文件**。

已实现的功能：

- **闪避取消攻击** — 原版只有起手瞬间和判定结束后能取消，patch 后整段攻击都能取消
- **踩踏前踏吸附** — 按下踩踏先向前踏一小段，贴到刚好踩得到的距离，不再原地踩空
- **瞄准指示线** — 从枪口 `ProjectileWeapon.m_muzzle` 画一条灰色细线，定长，只在真正瞄准时出现
- **背包扩容**（v1.1.0）— 面板调额外行数（0–10），格子跟着长出来，已买的扩容道具照常叠加；缩容前会检查额外行有没有物品，有就拒绝（越界物品会让背包菜单卡死，只能大退）
- **瞄准时移动不再带偏方向**（v1.1.0）— 手柄左摇杆、键鼠 A/D 都不再改变瞄准角度，右摇杆/鼠标仍能正常转身
- **显示闪避无敌帧**（v1.1.0，默认关）— 无敌期间角色整体变亮青色，能直观看出无敌窗口有多长

外加 F1–F5 作弊开关、F6 慢动作、判定框绘制（默认关）。面板支持中英双语。

---

## 下载

去 [Releases](../../releases) 页面拿打包好的：

| 文件 | 体积 | 内容 |
|---|---|---|
| `SilverPinesCombat-1.1.0-full.zip` | **34.4 MB** | 插件 + BepInEx 6 be.789 全套，什么都不装，解压到游戏根目录就能用 |
| `SilverPinesCombat-1.1.0.zip` | **57 KB** | 只有插件 DLL，适合已经装过 BepInEx 6 IL2CPP（be.789 或更新）的人 |

⚠️ 解压时别让资源管理器多套一层目录（默认会解到 `Silver Pines\SilverPinesCombat-1.1.0-full\`）。
`winhttp.dll` 必须在 `Silver Pines\` 这一层，否则游戏能启动但 mod 完全不加载、且没有任何报错。

> `-full` 包里**不含** `BepInEx\unity-libs\`——那是首次启动时 BepInEx 自己生成的缓存（约 11 MB），
> 打进压缩包会让 Nexus 的自动扫描判定为「嵌套压缩包」而隔离整个文件。

安装和使用说明见 [`dist/README.md`](dist/README.md)（中文）/ [`dist/README.en.md`](dist/README.en.md)（English）。

---

## 目录结构

```
plugin/           插件源码
  Plugin.cs         全部代码（4 个类：入口 / patch / 瞄准线 / 每帧驱动）
  SilverPinesCombat.csproj
  deploy.ps1        构建后拷进游戏目录并用 MD5 校验
dist/             发布产物与用户文档
  README.md / README.en.md   用户手册（安装 / 配置 / 已知限制）
  release-notes-v1.1.0.md    Release 正文（当前版本）
  release-notes-v1.0.0.md    Release 正文（历史）
  nexus-page.txt             Nexus 页面文案（含上传注意事项）
  nexus-description-1.1.0.txt  N 网 Description 正文（BBCode，直接粘贴）
  nexus-fields-1.1.0.txt       N 网配套字段（短描述 / Files 描述 / 标签）
tools/
  check-plugin.ps1 启动后自检脚本
recon.ps1          零代码侦察脚本
逆向教程_从零到注入.md   完整逆向过程，从买游戏到注入跑通
```

> 没进仓库的三样：`tools/dump/`（43 MB 的 `dump.cs`）、Il2CppDumper 本体、以及开发期的侦察笔记。
> 体积太大或含本机信息。需要的话按教程里的步骤自己生成。

---

## 自己编译

前置：游戏目录里已经有能跑的 BepInEx（首次启动会生成 `BepInEx\interop\`）。

```powershell
cd plugin
dotnet build -c Release
```

构建会自动引用 `BepInEx\interop\*.dll`，并把产物拷到 `BepInEx\plugins\SilverPinesCombat\`。想换游戏目录：

```powershell
dotnet build -c Release -p:GameDir="D:\...\Silver Pines"
```

**版本要求**：BepInEx 6.0.0-bleeding-edge（be.789 或更新），IL2CPP win-x64。老版本会报 `Unsupported metadata version found! We support 23-29, got 39`——游戏用的是 metadata v39。

---

## 这个 mod 是怎么做出来的

一句话：IL2CPP 没有 IL，改不了方法体，只能靠 Harmony 的 Prefix / Postfix 在方法进出时动手，加上原生 `il2cpp_field_static_set_value` 写静态开关。类型信息是官方 Il2CppDumper 不支持 metadata v39，从上游未合并的 PR 分支自己编译才拿到的。

完整过程（含踩过的坑）写在 [`逆向教程_从零到注入.md`](逆向教程_从零到注入.md)。

---

## 许可

本项目代码 MIT，见 [LICENSE](LICENSE)。

仓库里不含任何游戏原文件。`-full` 发布包里捆绑的 BepInEx 是 LGPL-2.1，见 `dist/THIRD-PARTY-NOTICES.txt`。
