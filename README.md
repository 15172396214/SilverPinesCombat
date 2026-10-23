# Silver Pines Combat

[English](dist/README.en.md) | 中文

给《银松镇 / Silver Pines》（Steam AppID 2333000）做的战斗手感改造 mod。Unity 6 + IL2CPP，走 BepInEx 6 Bleeding Edge + Harmony，**全部改动在运行时内存里完成，不碰任何游戏文件**。

三个已实现的功能：

- **闪避取消攻击** — 原版只有起手瞬间和判定结束后能取消，patch 后整段攻击都能取消
- **踩踏前踏吸附** — 按下踩踏先向前踏一小段，贴到刚好踩得到的距离，不再原地踩空
- **瞄准指示线** — 从枪口 `ProjectileWeapon.m_muzzle` 画一条灰色细线，定长，只在真正瞄准时出现

外加 F1–F5 作弊开关、F6 慢动作、判定框绘制（默认关）。面板支持中英双语。

---

## 下载

去 [Releases](../../releases) 页面拿打包好的：

| 文件 | 内容 |
|---|---|
| `SilverPinesCombat-1.0.0.zip` | 只有插件 DLL + 两份 README，适合已经装过 BepInEx 的人 |
| `SilverPinesCombat-1.0.0-full.zip` | 含 BepInEx 6 be.789 全套，什么都不装直接解压就能用 |

安装和使用说明见 [`dist/README.md`](dist/README.md)（中文）/ [`dist/README.en.md`](dist/README.en.md)（English）。

---

## 目录结构

```
plugin/           插件源码
  Plugin.cs         全部代码（4 个类：入口 / patch / 瞄准线 / 每帧驱动）
  SilverPinesCombat.csproj
  deploy.ps1        构建后拷进游戏目录并用 MD5 校验
dist/             发布产物与用户文档
tools/
  check-plugin.ps1 启动后自检脚本
  recon.ps1        零代码侦察脚本
侦察结果_*.md      游戏文件结构侦察记录
反编译结果_*.md    作弊开关 / 控制台相关类型整理
逆向教程_从零到注入.md   完整逆向过程，从买游戏到注入跑通
战斗改造方案.md     功能设计与取舍记录
```

> `tools/dump/`（43 MB 的 `dump.cs`）和 Il2CppDumper 没进仓库，体积太大。需要的话按教程里的步骤自己生成。

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
