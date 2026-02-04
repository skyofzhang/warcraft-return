# WarcraftReturn - Unity 工程说明

**依据**: 《程序基础知识库 v1.3》《程序开发计划_可执行版》

**执行前必读**: 每次按计划执行任务前，请先阅读《程序基础知识库》（四阶段自检、脚本依赖顺序、事件表、配置验证、测试闭环等）。

## 一键完成（推荐）

1. 用 **Unity 2022.3.47f1** 打开本工程（在 Unity Hub 中「添加」并选择 `WarcraftReturn` 文件夹；若无法识别为项目，请先新建 3D 项目 WarcraftReturn，再将本仓库 `WarcraftReturn\Assets` 下全部内容复制到该项目的 `Assets` 下）。
2. **自动运行第二步**（二选一）：
   - **推荐**：双击项目根目录下的 **`运行一键配置.bat`**（或在 PowerShell 中执行 **`.\运行一键配置.ps1`**），脚本会以 Unity 批处理模式执行一键配置，无需打开 Unity 界面。
   - 或在 Unity 菜单栏点击：**WarcraftReturn → 一键配置工程与场景**。
3. 脚本将自动完成：
   - **项目设置**：Company Name、Product Name、竖屏 Portrait、默认分辨率 1080×1920
   - **Layers**：Player、Monster、Projectile、Ground
   - **Tag**：Monster（Player 已内置）
   - **Managers**：创建空物体并挂载 GameManager、EventManager、ConfigManager
   - **MainMenu 场景**：仅含 Managers，保存为 `Assets/Scenes/MainMenu.unity`
   - **Gameplay 场景**：Managers + 地面 Plane + 玩家占位符（Capsule + 脚本 + Tag Player）+ 怪物占位符（Capsule + 脚本 + Tag Monster），保存为 `Assets/Scenes/Gameplay.unity`
   - **Build Settings**：自动加入 MainMenu、Gameplay 场景
   - **Managers** 还挂载：LootManager、EquipmentManager、UIManager、AudioManager；**Gameplay 场景** 额外挂载 **SanityCheck** 组件

完成后可直接运行 **MainMenu** 或 **Gameplay** 场景进行测试。

### Phase 5：数值与测试

- **数值护栏**：玩家初始 HP/攻击/防御/移速符合 GDD 3.5（PlayerStats 默认值在护栏内）。
- **SanityCheck**：Gameplay 场景启动时自检玩家、刷怪点、Canvas、数值护栏；未通过时在 Console 输出警告。
- **自动化测试**：GDD 第 11 章 12 条测试用例对应 `Assets/Tests/PlayMode/WarcraftReturnPlayModeTests.cs`。需先执行一键配置（或手动将 MainMenu、Gameplay 加入 Build Settings），再在 Unity 中打开 **Window → General → Test Runner**，切换到 **PlayMode**，点击 **Run All**。

---

## 手动方式（可选）

### 1.1 创建 Unity 工程

1. 打开 **Unity Hub**，选择 **Unity 2022.3.47f1**。
2. 新建 **3D (Core)** 项目，名称 `WarcraftReturn`，或将本仓库 `WarcraftReturn` 文件夹作为项目根目录打开。

### 1.2 目录结构（已创建）

- `Art/`、`Prefabs/`、`Scripts/`、`Scenes/`、`Resources/Config/`、`Audio/`、`Tests/PlayMode/` 等已就绪。

### 1.3 配置项目设置（一键脚本已包含）

若未使用一键脚本，可手动在 Edit → Project Settings 中配置 Player（竖屏、分辨率）、Quality、Layers（Player、Monster、Projectile、Ground）。
