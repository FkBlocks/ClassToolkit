# ClassToolkit 项目文档

本目录存放 ClassToolkit 项目的设计与规划文档。

## 文档索引

| 文档 | 状态 | 说明 |
|---|---|---|
| [REQUIREMENTS.md](REQUIREMENTS.md) | ✅ 现行 | **需求总纲与路线图**：全部需求、架构决策、工期与版本切割 |
| [SETTINGS-DESIGN.md](SETTINGS-DESIGN.md) | ✅ 现行 | 设置模块设计文档：字典式配置架构与控件扩展速查手册 |
| [FLOATBALL_EXPANSION.md](FLOATBALL_EXPANSION.md) | 🗄️ 已归档 | 悬浮球扩展方案。悬浮球已被左右对称侧边栏取代；其中"音量控制集成（Core Audio）"一节仍与音量需求直接相关，留作参考 |

## 约定

- 需求变更与决策记录统一维护在 `REQUIREMENTS.md` 的 §12 决策记录中；
- 废弃方案文档保留在本目录并标注"已归档"，不直接删除；
- 仓库根目录的 `README.md` 为项目主页（GitHub 展示页），按惯例保留原位；
- `packages/**/README.md` 为 NuGet 依赖包自带文档，不属于项目文档。
