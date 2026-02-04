# 仓库指南 | Repository Guide

> **读者**: 人类(项目管理) + AI(代码开发与质检)
> **维护者**: Claude (通过n8n工作流自动维护)
> **最后更新**: 2026-02-04

---

## 1. 仓库矩阵总览

本项目以 **AI量产游戏** 为目标，采用多仓库架构。所有AI在接入时先读本文件定位目标仓库。

```json
{
  "repo_matrix": [
    {
      "name": "warcraft-return",
      "url": "https://github.com/skyofzhang/warcraft-return",
      "type": "game_project",
      "description": "Unity游戏工程 - 当前项目《我叫MT之魔兽归来》",
      "engine": "Unity 2022.3.47f1",
      "who_writes": ["Cursor"],
      "who_reads": ["Claude(质检)", "n8n(监听)", "人类(体验)"],
      "branch_strategy": {
        "main": "稳定版本，通过所有测试",
        "dev": "开发分支，Cursor日常提交",
        "feature/*": "功能分支，大功能开发"
      }
    },
    {
      "name": "ai-resources",
      "url": "https://github.com/skyofzhang/ai-resources",
      "type": "knowledge_base",
      "description": "知识库文档 - 策划案、开发规范、知识库",
      "who_writes": ["Claude"],
      "who_reads": ["Cursor", "人类", "其他AI"],
      "sync_with": "Notion 小游戏知识库V3.0"
    },
    {
      "name": "ai-game-templates",
      "url": "https://github.com/skyofzhang/ai-game-templates",
      "type": "template_library",
      "description": "模板库 - 可复用的策划模板、代码模块、资源规范",
      "who_writes": ["Claude(知识沉淀)"],
      "who_reads": ["所有AI(开发新游戏时复用)"],
      "purpose": "开发一次，复用多次"
    }
  ]
}
```

---

## 2. 本仓库(warcraft-return)目录结构

```
warcraft-return/
├── REPO_GUIDE.md              ← 你正在读的文件(仓库指南)
├── .gitignore                 ← Git忽略规则
├── .task/                     ← [AI任务目录] n8n写入，Cursor读取
│   ├── current_task.json      ← 当前待执行任务
│   └── completed/             ← 已完成任务归档
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              ← 核心系统(GameManager等管理器)
│   │   ├── Data/              ← 数据定义(枚举、接口、配置类)
│   │   ├── Combat/            ← 战斗系统(伤害计算、特效)
│   │   ├── Gameplay/          ← 游戏玩法(角色控制、怪物AI)
│   │   ├── Systems/           ← 游戏系统(刷怪、掉落、装备)
│   │   ├── Environment/       ← 环境(地图构建、滚动)
│   │   └── UI/                ← UI系统(UIManager+25个面板)
│   ├── Editor/                ← 编辑器工具(10个)
│   ├── Tests/PlayMode/        ← 测试用例(13个PlayMode测试)
│   ├── Resources/Config/      ← JSON配置文件(6个)
│   ├── Art/                   ← 美术资源
│   └── Audio/                 ← 音频资源
├── Packages/                  ← Unity包管理
├── ProjectSettings/           ← Unity项目设置
└── Tools/                     ← 辅助工具脚本
```

### AI快速定位指南

```json
{
  "ai_navigation": {
    "要改游戏逻辑": "Assets/Scripts/Gameplay/",
    "要改战斗系统": "Assets/Scripts/Combat/",
    "要改UI界面": "Assets/Scripts/UI/",
    "要改核心管理器": "Assets/Scripts/Core/",
    "要改数值配置": "Assets/Resources/Config/",
    "要改数据结构": "Assets/Scripts/Data/",
    "要改怪物生成": "Assets/Scripts/Systems/MonsterSpawner.cs",
    "要改装备系统": "Assets/Scripts/Systems/EquipmentManager.cs",
    "要跑测试": "Assets/Tests/PlayMode/",
    "要读任务": ".task/current_task.json",
    "要提交完成": "git commit -m '[NODE_COMPLETE] 功能名称'"
  }
}
```

---

## 3. AI任务协议

### 3.1 Cursor读取任务

```json
{
  "task_protocol": {
    "read_task": ".task/current_task.json",
    "task_format": {
      "id": "TASK-001",
      "phase": "Phase6",
      "module": "Combat",
      "description": "实现技能冷却系统",
      "reference_doc": "Notion L4/05 技能系统策划案",
      "acceptance_criteria": ["CD冷却倒计时", "UI显示冷却遮罩", "技能CD期间不可释放"],
      "created_by": "n8n/Claude",
      "status": "pending"
    },
    "on_complete": {
      "step1": "更新.task/current_task.json的status为completed",
      "step2": "git add && git commit -m '[NODE_COMPLETE] 技能冷却系统'",
      "step3": "git push origin dev",
      "step4": "等待Claude审核反馈(通过n8n自动触发)"
    }
  }
}
```

### 3.2 提交规范

```json
{
  "commit_convention": {
    "format": "[TAG] 简要描述",
    "tags": {
      "[NODE_COMPLETE]": "节点任务完成，触发Claude审核",
      "[FEATURE]": "新功能",
      "[FIX]": "修复BUG",
      "[REFACTOR]": "重构",
      "[CONFIG]": "配置变更",
      "[TEST]": "测试相关",
      "[WIP]": "进行中，不触发审核"
    },
    "examples": [
      "[NODE_COMPLETE] 技能冷却系统完成",
      "[FIX] 修复怪物死亡后不消失",
      "[FEATURE] 新增装备强化功能",
      "[WIP] 关卡编辑器进行中"
    ]
  }
}
```

---

## 4. 依赖关系

```json
{
  "dependencies": {
    "knowledge_base": {
      "repo": "ai-resources",
      "usage": "Cursor开发前先读取知识库获取规范",
      "key_files": [
        "Docs/知识库与策划案/知识库/游戏开发的程序知识库（Unity）v2.1.1.md",
        "Docs/cursor_program_basic_knowledge_base_opt.md"
      ]
    },
    "template_library": {
      "repo": "ai-game-templates",
      "usage": "新功能开发时先检查是否有可复用模板",
      "check_before": "开始写新代码前"
    },
    "notion": {
      "project_kb": "小游戏知识库V3.0",
      "key_pages": {
        "L3_知识库": "2faba9f9-9f58-8149-9780-de23ee8970c8",
        "L4_执行层": "2faba9f9-9f58-81db-9405-d78a41aa07ae",
        "L5_开发计划": "2faba9f9-9f58-81fa-a18a-c20c453bc7d0",
        "L6_开发进度": "2faba9f9-9f58-816c-9bf9-d70909dd1644",
        "L7_BUG追踪": "2faba9f9-9f58-810f-a26a-c6c8008350bc"
      }
    }
  }
}
```

---

## 5. 质量门禁

```json
{
  "quality_gates": {
    "before_push": {
      "must": ["代码编译通过", "无明显runtime错误"],
      "should": ["PlayMode测试通过", "SanityCheck无错误"]
    },
    "before_merge_to_main": {
      "must": [
        "所有PlayMode测试通过(13个)",
        "SanityCheck无错误",
        "Claude审核通过",
        "FPS >= 30"
      ]
    }
  }
}
```

---

## 6. 维护说明

- 本文件由 **Claude** 通过n8n工作流自动维护
- 每次目录结构变更时自动更新
- AI读取本文件获取仓库导航信息
- 人类读取本文件了解项目架构
- 版本号跟随工作流版本: 当前 V1.0
