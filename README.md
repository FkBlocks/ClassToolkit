<div align="center">
  <h1>ClassToolkit（课堂工具箱）</h1>
  <img src="https://img.shields.io/github/stars/FkBlocks/ClassToolkit?style=flat-square&logo=github&color=yellow" alt="GitHub stars">
  <img src="https://img.shields.io/github/forks/FkBlocks/ClassToolkit?style=flat-square&logo=github&color=blue" alt="GitHub forks">
  <img src="https://img.shields.io/badge/License-GPLv3-blue?style=flat-square&logo=gnu" alt="License">
  <img src="https://img.shields.io/github/contributors/FkBlocks/ClassToolkit" alt="GitHub contributors">
  <p />
  <h3>让教学与互动无缝衔接</h3>
</div>

---

## 简介
- ClassToolkit 是一个**完全开源免费、面向课堂**、适用于大屏触摸一体机的工具箱，旨在**让教学与互动无缝衔接**； 
- **同一作者，完全重构于仓库**: [class-toolkit](https://github.com/FkBlocks/class-toolkit) ，才用与原仓库完全不同的语言（Python -> C#），拥有**超高的性能**，**美观的界面**和**稳定的体验**；
- 无缝衔接，在使用过程中，**无需退出PPT，视频播放器**等**全屏**软件，即可互动，保证极佳的课堂效率

## 功能

### 对于教师
- 随机点名：可以快速抽取同学回答问题，进行课堂互动，支持姓名和学号两种模式
- 倒计时：限时活动，准备时间等
- 音量调节：在PPT中播放音乐/视频/听力时，可以在**不被打断、无需退出播放**的情况下调节系统的声音至合适的大小

### 对于普通学生
- 课间&课前，会发出提示即将上课，让同学们做好准备
- 课上，可以展示 当前课程、下节课程、当前时间、结束时间、天气情况 \
*我们强烈建议您搭配 **ClassIsland** 使用*

### 对于电教委员
- 可以在设置面板中按照班级喜好进行个性化设置
- 可以调整各项使用设置以使班级的老师上课更加得心应手
- 您也可以拉取我们仓库的代码，进行个性化定制开发出**专属于你们学校/班级的 ClassToolkit** \
*同时，我们非常欢迎您给我们**提出issue**；如果您有足够的实力，**我们也热烈欢迎您为 ClassToolkit 贡献代码，甚至进行二次开发*** \
* *注：请二次创作后务必遵循**GPL v3开源协议***

## 开始使用
**首先，你设备的系统必须满足为 Windows 10 Biuld 1809 及以上的版本** \
- 本软件仅能在 Windows 环境下运行

1. 首先，您需要在你的系统上安装[ .NET 10 桌面运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)
2. 下载 ClassToolkit，从[Github Release](https://github.com/FkBlocks/ClassToolkit/releases)
3. 下载完成后，解压缩得到的zip压缩包，双击`ClassToolkit.exe`运行，然后退出，完成默认配置文件的生成
4. 退出后，进入`解压缩后的文件夹/data/`，找到`names.txt`文件，双击打开，在记事本中，以**一行一个名字**的方式，填写班级所有同学的姓名，在随机点名中将会使用这份名单
5. 回到上一级目录，双击`ClassToolkit.exe`运行
6. 点击小球，点击设置，即可开始个性化的配置
- 最后祝您使用愉快，让课堂更加丰富多彩
