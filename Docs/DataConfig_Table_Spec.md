# Target Hunt Demo V0.1 数据配置表结构

## 1. 使用说明

这份文档定义首版 Demo 需要的最小数据表结构。

目标：

- 让角色、Boss、战斗流程、Build 结果有统一字段口径
- 先服务当前 Demo，不为长线系统预留过多复杂字段

---

## 2. 角色表

表名建议：

- `CharacterConfig`

每名角色一条记录。

### 2.1 字段

| 字段名 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `id` | string | 是 | 角色唯一 ID |
| `displayName` | string | 是 | 角色显示名称 |
| `roleType` | enum | 是 | `Swordsman` / `Brawler` / `Archer` |
| `resourceId` | string | 是 | 当前绑定的像素资源名 |
| `defaultWeaponType` | enum | 是 | `Sword` / `Fist` / `Bow` |
| `defaultAttackAction` | string | 是 | 默认攻击动作名 |
| `moveAction` | string | 是 | 默认移动动作名 |
| `attackTempo` | enum | 是 | `Fast` / `Medium` / `Slow` |
| `attackRangeType` | enum | 是 | `MeleeShort` / `MeleeVeryShort` / `RangedLine` |
| `spawnSlot` | enum | 是 | `Front` / `Mid` / `Back` |
| `isPlayable` | bool | 是 | 是否为玩家可用角色 |

### 2.2 当前建议配置

| id | displayName | roleType | resourceId | defaultWeaponType | defaultAttackAction | moveAction | attackTempo | attackRangeType | spawnSlot | isPlayable |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `player_swordsman` | 剑士 | `Swordsman` | `human_swordsman` | `Sword` | `attack` | `walk` | `Medium` | `MeleeShort` | `Front` | `true` |
| `player_brawler` | 拳师 | `Brawler` | `assassin` | `Fist` | `attack` | `walk` | `Fast` | `MeleeVeryShort` | `Mid` | `true` |
| `player_archer` | 弓箭手 | `Archer` | `longbowman` | `Bow` | `attack` | `walk` | `Slow` | `RangedLine` | `Back` | `true` |

---

## 3. Boss 表

表名建议：

- `BossConfig`

### 3.1 字段

| 字段名 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `id` | string | 是 | Boss 唯一 ID |
| `displayName` | string | 是 | Boss 显示名称 |
| `resourceId` | string | 是 | 当前绑定的像素资源名 |
| `weaponType` | enum | 是 | 当前武器类型 |
| `mainAttackAction` | string | 是 | 首版主攻击动作 |
| `moveAction` | string | 是 | 移动动作 |
| `deathAction` | string | 是 | 死亡动作 |
| `attackRangeType` | enum | 是 | `SpinArea` |
| `attackTempo` | enum | 是 | `Medium` 或 `Fast` |
| `maxHp` | int | 是 | 最大生命值 |
| `dropWeaponType` | enum | 是 | 死亡后掉落武器 |

### 3.2 当前建议配置

| id | displayName | resourceId | weaponType | mainAttackAction | moveAction | deathAction | attackRangeType | attackTempo | maxHp | dropWeaponType |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `boss_meteor_hammer` | 流星锤 Boss | `goblin_boss_wife` | `MeteorHammer` | `attack_round` | `walk` | `death` | `SpinArea` | `Medium` | `100` | `MeteorHammer` |

---

## 4. 武器表

表名建议：

- `WeaponConfig`

### 4.1 字段

| 字段名 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `id` | string | 是 | 武器唯一 ID |
| `displayName` | string | 是 | 武器显示名称 |
| `weaponType` | enum | 是 | 武器类型 |
| `attackBehaviorType` | enum | 是 | 攻击行为类型 |
| `rangeType` | enum | 是 | 攻击距离或范围类型 |
| `tempo` | enum | 是 | 攻击节奏 |
| `canEquipInBuild` | bool | 是 | 是否能在 Build 中分配 |
| `isBossDrop` | bool | 是 | 是否来自 Boss 掉落 |

### 4.2 当前建议配置

| id | displayName | weaponType | attackBehaviorType | rangeType | tempo | canEquipInBuild | isBossDrop |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `weapon_sword` | 剑 | `Sword` | `SlashForward` | `MeleeShort` | `Medium` | `false` | `false` |
| `weapon_fist` | 拳 | `Fist` | `PunchForward` | `MeleeVeryShort` | `Fast` | `false` | `false` |
| `weapon_bow` | 弓 | `Bow` | `ShootLine` | `RangedLine` | `Slow` | `false` | `false` |
| `weapon_meteor_hammer` | 流星锤 | `MeteorHammer` | `SpinArea` | `SpinArea` | `Medium` | `true` | `true` |

---

## 5. 战斗站位表

表名建议：

- `BattleFormationConfig`

### 5.1 字段

| 字段名 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `id` | string | 是 | 阵型 ID |
| `frontCharacterId` | string | 是 | 前排角色 |
| `midCharacterId` | string | 是 | 中排角色 |
| `backCharacterId` | string | 是 | 后排角色 |
| `bossId` | string | 是 | Boss ID |

### 5.2 当前建议配置

| id | frontCharacterId | midCharacterId | backCharacterId | bossId |
| --- | --- | --- | --- | --- |
| `demo_default_battle` | `player_swordsman` | `player_brawler` | `player_archer` | `boss_meteor_hammer` |

---

## 6. Build 结果表

表名建议：

- `BuildSelectionState`

这是运行时数据，不一定需要做成静态资源表，但字段口径需要固定。

### 6.1 字段

| 字段名 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `hasClaimedMeteorHammer` | bool | 是 | 是否已经拾取流星锤 |
| `selectedCharacterId` | string | 否 | 当前被选为流星锤使用者的角色 ID |
| `isSelectionConfirmed` | bool | 是 | 是否已经在 Build 中点击确认 |

### 6.2 当前规则

- 第一场开始前，`hasClaimedMeteorHammer = false`
- 玩家拾取流星锤后，`hasClaimedMeteorHammer = true`
- 玩家未在 Build 中完成拖拽前，`selectedCharacterId` 为空
- 玩家点击确认后，`isSelectionConfirmed = true`

---

## 7. Demo 流程状态表

表名建议：

- `DemoRunState`

这是运行时流程状态，不一定需要做成静态资源表，但字段口径需要固定。

### 7.1 字段

| 字段名 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `phase` | enum | 是 | `MainMenu` / `Battle01` / `Drop` / `Build` / `Battle02` / `Result` |
| `isBattle01Complete` | bool | 是 | 第一场是否完成 |
| `isDropSpawned` | bool | 是 | 是否已生成掉落物 |
| `isDropClaimed` | bool | 是 | 是否已拾取掉落物 |
| `isBattle02Started` | bool | 是 | 第二场是否开始 |
| `isBattle02Complete` | bool | 是 | 第二场是否完成 |

---

## 8. 首版推荐枚举

### 8.1 `WeaponType`

- `Sword`
- `Fist`
- `Bow`
- `MeteorHammer`

### 8.2 `RoleType`

- `Swordsman`
- `Brawler`
- `Archer`

### 8.3 `AttackTempo`

- `Fast`
- `Medium`
- `Slow`

### 8.4 `AttackRangeType`

- `MeleeShort`
- `MeleeVeryShort`
- `RangedLine`
- `SpinArea`

### 8.5 `PhaseType`

- `MainMenu`
- `Battle01`
- `Drop`
- `Build`
- `Battle02`
- `Result`

---

## 9. 当前最小落地要求

如果要尽快开始做 Demo，最少先准备以下 4 份配置：

1. `CharacterConfig`
2. `BossConfig`
3. `WeaponConfig`
4. `BuildSelectionState`

这样就足够驱动：

- 第一场战斗
- 掉落与拾取
- Build 选择
- 第二场战斗加载
