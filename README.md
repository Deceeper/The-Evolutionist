# The Evolutionist / 进化者

A custom playable slugcat mod for Rain World: Downpour.

《雨世界：倾盆大雨》的自定义可玩蛞蝓猫模组。

## 简介

进化者能够通过完整食用生物尸体获得永久进化。一次雨循环内获得的进度只有在成功雨眠后才会保存；死亡会使本循环尚未保存的进度回滚。

除了普通属性成长外，进化者还可以解锁以下特殊能力：

- 圣徒的舌头
- 工匠的爆炸跳与爆炸格挡
- 矛大师的活体针矛生成
- 猎手的背部存矛

模组还包含进化者专属的剧情路线、对白和特殊结局。

## Features

The Evolutionist permanently evolves by fully consuming creature corpses. Progress earned during the current cycle is saved after successful hibernation and rolled back on death.

Unlockable abilities include:

- Saint's tongue
- Artificer's explosive jump and parry
- Spearmaster-style needle spear generation
- Hunter's back spear storage

The mod also includes custom story events, dialogue, and a unique ending.

## 依赖

- Rain World
- Downpour / More Slugcats Expansion
- SlugBase

## 安装

普通玩家请使用 Steam 创意工坊版本或 GitHub Releases 中提供的完整模组压缩包。

本仓库主要保存源代码。直接下载源代码不会包含已经编译的 `Evolutionist.dll`。

## 编译

项目目标框架为 `netstandard2.1`。

项目默认使用开发者本机的《雨世界》和 SlugBase 路径。如果安装位置不同，请通过 MSBuild 属性指定：

```powershell
dotnet build .\Evolutionist.csproj -c Debug `
  -p:RainWorldDir="你的雨世界安装目录" `
  -p:SlugBaseDir="SlugBase插件目录"
  ```

编译后的插件会生成到：

```text
mod\plugins\Evolutionist.dll
```

## 项目结构

```text
src/                 C# 源代码
mod/                 SlugBase 配置、世界生成文件和美术资源
Evolutionist.csproj  项目文件
Evolutionist.slnx    Visual Studio 解决方案
```

## 作者

Deceeper