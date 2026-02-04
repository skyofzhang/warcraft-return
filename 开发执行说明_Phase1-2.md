# 开发执行说明（Phase 1～2 已完成）

**依据**: 《程序开发计划_可执行版》、程序基础知识库 v1.3

## 已完成内容

### Phase 1
- **1.1**：需在 Unity Hub 中手动创建工程（见 `README_Unity工程说明.md`）。
- **1.2**：已在 `WarcraftReturn/Assets/` 下创建目录：Art（Characters/Monsters/Scenes/UI/VFX）、Prefabs（Characters/Monsters/Props/UI）、Scripts（Core/Gameplay/Data）、Scenes、Resources/Config、Audio（Music/SFX）、Tests/PlayMode。
- **1.3**：需在 Unity 中手动配置 Player/Quality/Physics（见 README）。

### Phase 2
- **第一层**：`Scripts/Data/StatType.cs`、`IStatsProvider.cs`、`ConfigDataClasses.cs`（GDD 第 10 章 JSON 结构）。
- **第二层**：`Scripts/Core/PlayerStats.cs`、`MonsterStats.cs`（IStatsProvider）、`EventManager.cs`（策划知识库 10.2 事件名）、`ConfigManager.cs`（加载 + 简单验证）。
- **第三层**：`Scripts/Core/VirtualJoystick.cs`（占位）、`Scripts/Gameplay/PlayerController.cs`、`MonsterController.cs`。
- **第五层**：`Scripts/Core/GameState.cs`、`GameManager.cs`（状态机、ValidateArtAssets、场景切换仅通过 GameManager）。
- **配置**：`Resources/Config/` 下已放置 `LevelConfigs.json`、`MonsterConfigs.json`、`EquipmentConfigs.json`、`DropTableConfigs.json` 示例。

## 您需要做的（已可由脚本自动完成）

1. **用 Unity 打开工程**  
   - 用 **Unity 2022.3.47f1** 打开本仓库中的 `WarcraftReturn` 文件夹（作为项目根目录）。

2. **一键配置**  
   - 在 Unity 菜单栏点击：**WarcraftReturn → 一键配置工程与场景**。  
   - 将自动完成：项目设置（竖屏 1080×1920、Company/Product）、Layers（Player/Monster/Projectile/Ground）、Tag（Monster）、Managers 挂载、MainMenu 与 Gameplay 场景创建、Build Settings 添加。详见 `README_Unity工程说明.md`。

3. **测试**  
   - 打开 `Assets/Scenes/Gameplay.unity`，运行 Play，用键盘 WASD 移动玩家，靠近怪物可自动普攻；怪物会追逐并攻击玩家。

4. **下一步**  
   - 按计划继续 **Phase 3**（战斗系统、技能、成长、关卡、掉落、装备、相机）。

## 事件名与依赖

- 事件名已与《游戏设计的策划知识库 v1.6》10.2 一致（如 `GAME_STATE_CHANGED`、`HEALTH_CHANGED`、`MONSTER_KILLED`）。
- 脚本依赖顺序符合程序基础知识库 5.9。
