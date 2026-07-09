# Target Hunt Demo V0.1 Unity 场景 / 预制体 / 数据配置清单

## 1. 目标

这份清单用于把当前功能文档落到 Unity 工程结构上，明确：

- 需要哪些场景
- 需要哪些预制体
- 需要哪些 UI
- 需要哪些数据配置
- 每项资源在 Demo 中承担什么作用

---

## 2. 场景清单

### 2.1 `MainMenu`

用途：

- 进入 Demo 的开始界面

场景内容：

- 背景图或简单像素背景
- 标题
- `Start Hunt` 按钮

需要完成的事：

- 点击开始后进入 `BattleScene`
- 初始化为第一场战斗

### 2.2 `BattleScene`

用途：

- 承载第一场战斗和第二场战斗

场景内容：

- 固定横版像素战场背景
- 玩家队伍生成点
- Boss 生成点
- Boss 血条 UI
- 掉落提示 UI

需要完成的事：

- 根据当前流程状态决定是第一场还是第二场
- 根据当前配置生成玩家角色
- 根据当前配置生成 Boss
- 第一场结束后进入掉落
- 第二场结束后进入结果界面

### 2.3 `BuildScene`

用途：

- 承载流星锤分配界面

场景内容：

- 标题 `Choose a Wielder`
- 3 个角色槽位
- 1 个流星锤拖拽对象
- 确认按钮

需要完成的事：

- 展示三名角色当前默认状态
- 允许玩家拖拽流星锤给任意角色
- 保存第二场战斗使用者

### 2.4 `ResultScene`

用途：

- 展示 Demo 结束

场景内容：

- 完成标题
- 重新开始按钮

需要完成的事：

- 点击重新开始后回到 `MainMenu`

---

## 3. 战斗场景层级建议

`BattleScene` 建议至少包含以下根节点：

- `BattleRoot`
- `Background`
- `PlayerTeamRoot`
- `BossRoot`
- `DropRoot`
- `BattleUI`
- `SpawnPoints`

### 3.1 `SpawnPoints`

建议子节点：

- `PlayerFrontPoint`
- `PlayerMidPoint`
- `PlayerBackPoint`
- `BossPoint`
- `DropPoint`

### 3.2 `BattleUI`

建议子节点：

- `BossHpBar`
- `DropHint`

---

## 4. 角色预制体清单

### 4.1 玩家角色主选预制体

需要制作：

- `PF_Player_Swordsman`
- `PF_Player_Brawler`
- `PF_Player_Archer`

当前资源映射：

- `PF_Player_Swordsman` 使用 `human_swordsman`
- `PF_Player_Brawler` 使用 `assassin`
- `PF_Player_Archer` 使用 `longbowman`

### 4.2 玩家角色预制体必须包含

- 像素渲染节点
- 动画播放组件
- 战斗表现组件
- 基础朝向控制
- 攻击起点或命中参考点
- 受击判定区域

### 4.3 玩家角色预制体首版必须接入的动作

#### `PF_Player_Swordsman`

- `walk`
- `attack`

#### `PF_Player_Brawler`

- `walk`
- `attack`

#### `PF_Player_Archer`

- `walk`
- `attack`

### 4.4 玩家角色预制体首版可暂不接入

- 独立死亡动作
- 独立受击动作
- 技能动作
- 待机特效

---

## 5. Boss 预制体清单

### 5.1 Boss 主选预制体

需要制作：

- `PF_Boss_MeteorHammer`

当前资源映射：

- `PF_Boss_MeteorHammer` 使用 `goblin_boss_wife`

### 5.2 Boss 预制体必须包含

- 像素渲染节点
- 动画播放组件
- 血量组件
- 攻击范围表现
- 受击判定区域
- 死亡表现触发

### 5.3 Boss 首版必须接入的动作

- `walk`
- `attack_round`
- `death`

### 5.4 Boss 首版暂不接入的动作

- `attack_1`
- `slam`
- `slam_strong`

---

## 6. 掉落物预制体清单

### 6.1 需要制作

- `PF_Drop_MeteorHammer`

### 6.2 掉落物预制体必须包含

- 流星锤像素图或掉落表现图
- 点击区域
- 掉落后停留表现

### 6.3 掉落物行为要求

- 只在第一场 Boss 死亡后生成
- 只能点击一次
- 点击后销毁或隐藏
- 点击后推进到 `BuildScene`

---

## 7. Build 界面预制体清单

### 7.1 需要制作

- `PF_BuildPanel`
- `PF_BuildCharacterSlot`
- `PF_BuildWeaponDragItem`

### 7.2 `PF_BuildPanel` 内容

- 标题文本
- 三个角色槽位容器
- 流星锤拖拽对象
- 确认按钮

### 7.3 `PF_BuildCharacterSlot` 内容

- 角色名称
- 角色头像或半身像
- 当前武器标识
- 选中高亮

### 7.4 `PF_BuildWeaponDragItem` 内容

- 流星锤图标
- 拖拽显示
- 回位逻辑

---

## 8. UI 预制体清单

### 8.1 MainMenu UI

- `UI_MainMenu`

包含：

- 标题
- 开始按钮

### 8.2 Battle UI

- `UI_BattleHUD`

包含：

- `BossHpBar`
- `DropHint`

### 8.3 Build UI

- `UI_Build`

包含：

- Build 标题
- 三个角色槽位
- 流星锤拖拽对象
- 确认按钮

### 8.4 Result UI

- `UI_Result`

包含：

- 完成标题
- 重新开始按钮

---

## 9. 动画数据清单

### 9.1 玩家角色动画集

#### 剑士

- `human_swordsman.walk`
- `human_swordsman.attack`

#### 拳师占位

- `assassin.walk`
- `assassin.attack`

#### 弓箭手

- `longbowman.walk`
- `longbowman.attack`

### 9.2 Boss 动画集

- `goblin_boss_wife.walk`
- `goblin_boss_wife.attack_round`
- `goblin_boss_wife.death`

### 9.3 动画接入规则

- 第一场和第二场共用同一套默认角色动画
- Boss 首版始终使用 `attack_round`
- 第二场若未实现流星锤换装动作，则先不进入第二场开发

---

## 10. 数据配置清单

### 10.1 角色配置

每名角色至少需要以下字段：

- 角色 ID
- 显示名称
- 当前资源名
- 默认武器类型
- 默认攻击类型
- 攻击节奏
- 攻击距离
- 生成站位

需要配置：

- `CD_Player_Swordsman`
- `CD_Player_Brawler`
- `CD_Player_Archer`

### 10.2 Boss 配置

Boss 至少需要以下字段：

- Boss ID
- 显示名称
- 当前资源名
- 当前武器类型
- 默认攻击动作
- 攻击范围类型
- 最大生命值

需要配置：

- `BD_MeteorHammerBoss`

### 10.3 Build 结果配置

Build 至少需要记录：

- 当前是否已获得流星锤
- 流星锤分配给哪一名角色
- 第二场是否已经读取该结果

### 10.4 Demo 流程配置

流程状态至少需要记录：

- 当前处于主菜单、第一场、掉落、Build、第二场、结果中的哪一阶段
- 当前是否是第一场
- 当前是否是第二场
- 当前是否需要生成掉落物

---

## 11. 武器清单

### 11.1 当前必须存在的武器定义

- `Weapon_Sword`
- `Weapon_Fist`
- `Weapon_Bow`
- `Weapon_MeteorHammer`

### 11.2 当前实际接入要求

首版即使暂不实现完整换装系统，也必须先明确：

- 剑士默认使用 `Weapon_Sword`
- 拳师默认使用 `Weapon_Fist`
- 弓箭手默认使用 `Weapon_Bow`
- Boss 默认使用 `Weapon_MeteorHammer`

---

## 12. 首版必须先做完的资源接入

按优先级排序：

1. `human_swordsman` 的 `walk`、`attack`
2. `assassin` 的 `walk`、`attack`
3. `longbowman` 的 `walk`、`attack`
4. `goblin_boss_wife` 的 `walk`、`attack_round`、`death`
5. `BossHpBar`
6. `PF_Drop_MeteorHammer`
7. `UI_Build`

---

## 13. 当前可暂缓制作的内容

- 玩家角色独立死亡动作
- Boss 额外攻击动作
- 复杂受击反馈
- 第二场完整流星锤换装动作
- 更细的 Build 预览动画

---

## 14. 当前推荐目录口径

建议在 `Assets` 下按以下方式组织：

- `Assets/Art/Sprites/Units`
- `Assets/Art/Sprites/Weapons`
- `Assets/Art/UI`
- `Assets/Prefabs/Units`
- `Assets/Prefabs/Bosses`
- `Assets/Prefabs/Drops`
- `Assets/Prefabs/UI`
- `Assets/Scenes`
- `Assets/Data`

---

## 15. 当前开工顺序

1. 建 `MainMenu`、`BattleScene`、`BuildScene`、`ResultScene`
2. 接入三名玩家主选角色的基础动作
3. 接入 Boss 主选资源与血条
4. 跑通第一场战斗
5. 接入掉落物
6. 跑通拾取到 Build
7. 接入 Build 界面
8. 再回头实现第二场战斗与换装验证
