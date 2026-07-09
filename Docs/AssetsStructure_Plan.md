# Target Hunt Demo V0.1 Assets 目录规划

## 1. 目标

这份文档只定义当前 Demo 在 `Assets` 下的推荐目录结构。

原则：

- 先为当前 Demo 服务
- 不做过度预留
- 让资源、预制体、场景、数据分开

---

## 2. 推荐目录结构

```text
Assets
├─ Art
│  ├─ Sprites
│  │  ├─ Units
│  │  ├─ Weapons
│  │  ├─ Drops
│  │  └─ UI
│  └─ Backgrounds
├─ Data
│  ├─ Characters
│  ├─ Bosses
│  ├─ Weapons
│  └─ RuntimeTemplates
├─ Prefabs
│  ├─ Units
│  ├─ Bosses
│  ├─ Drops
│  ├─ UI
│  └─ Battle
├─ Scenes
├─ Scripts
│  ├─ Battle
│  ├─ Build
│  ├─ UI
│  ├─ Data
│  └─ Flow
└─ bundle
   └─ units
```

---

## 3. 当前目录职责

### 3.1 `Assets/bundle/units`

职责：

- 保留当前直接引用的原始像素单位资源包

当前保留：

- `human_swordsman`
- `assassin`
- `longbowman`
- `goblin_boss_wife`
- `human_militia`
- `human_crossbowman`
- `goblin_boss`

说明：

- 这里仍然是原始资源区
- 不建议把实际业务预制体直接堆在这里

### 3.2 `Assets/Art/Sprites/Units`

职责：

- 放整理后的单位图集、切片结果或后续转存资源

首版可以先不急着挪原图，但后续接正式预制体时建议统一归档到这里

### 3.3 `Assets/Art/Sprites/Weapons`

职责：

- 放剑、拳、弓、流星锤相关的武器显示资源

### 3.4 `Assets/Art/Sprites/Drops`

职责：

- 放掉落物显示资源
- 当前重点是流星锤掉落图

### 3.5 `Assets/Art/Sprites/UI`

职责：

- 放角色头像
- 放 Build 界面武器图标
- 放按钮和 UI 图

### 3.6 `Assets/Backgrounds`

职责：

- 放横版战斗背景
- 放主界面和结果界面背景

### 3.7 `Assets/Data`

职责：

- 放角色、Boss、武器、流程配置资源

建议子目录：

- `Assets/Data/Characters`
- `Assets/Data/Bosses`
- `Assets/Data/Weapons`
- `Assets/Data/RuntimeTemplates`

### 3.8 `Assets/Prefabs`

职责：

- 放正式业务预制体

建议子目录：

- `Assets/Prefabs/Units`
- `Assets/Prefabs/Bosses`
- `Assets/Prefabs/Drops`
- `Assets/Prefabs/UI`
- `Assets/Prefabs/Battle`

### 3.9 `Assets/Scenes`

职责：

- 放场景文件

建议最终包含：

- `MainMenu.unity`
- `BattleScene.unity`
- `BuildScene.unity`
- `ResultScene.unity`

### 3.10 `Assets/Scripts`

职责：

- 放脚本

建议子目录：

- `Assets/Scripts/Battle`
- `Assets/Scripts/Build`
- `Assets/Scripts/UI`
- `Assets/Scripts/Data`
- `Assets/Scripts/Flow`

---

## 4. 当前最值得先建的目录

如果现在开始动工程，建议最先补这几个目录：

- `Assets/Art`
- `Assets/Data`
- `Assets/Prefabs`
- `Assets/Scripts`

这样就能把后续产物和当前 `bundle` 原始资源分开。

---

## 5. 当前文件摆放建议

### 5.1 原始单位资源

继续保留在：

- `Assets/bundle/units`

### 5.2 角色配置

建议放在：

- `Assets/Data/Characters`

### 5.3 Boss 配置

建议放在：

- `Assets/Data/Bosses`

### 5.4 武器配置

建议放在：

- `Assets/Data/Weapons`

### 5.5 玩家角色预制体

建议放在：

- `Assets/Prefabs/Units`

### 5.6 Boss 预制体

建议放在：

- `Assets/Prefabs/Bosses`

### 5.7 流星锤掉落物预制体

建议放在：

- `Assets/Prefabs/Drops`

### 5.8 Build、HUD、结果界面 UI

建议放在：

- `Assets/Prefabs/UI`

---

## 6. 当前不建议做的事

- 不要把业务脚本写进 `bundle` 目录
- 不要把场景专用临时对象长期留在 `SampleScene`
- 不要把角色配置、Boss 配置、UI 配置混在同一个目录里
- 不要把原始资源目录和正式预制体目录混用
