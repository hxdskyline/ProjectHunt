# Demo 配置资源生成器使用说明

## 1. 入口

进入 Unity 后，在顶部菜单使用：

- `Project Hunt/Bootstrap/Create Demo Config Assets`

## 2. 会生成的资源

执行后会自动创建或更新以下配置资源：

### 2.1 角色配置

- `Assets/Data/Characters/CD_Player_Swordsman.asset`
- `Assets/Data/Characters/CD_Player_Brawler.asset`
- `Assets/Data/Characters/CD_Player_Archer.asset`

### 2.2 Boss 配置

- `Assets/Data/Bosses/BD_MeteorHammerBoss.asset`

### 2.3 武器配置

- `Assets/Data/Weapons/WD_Weapon_Sword.asset`
- `Assets/Data/Weapons/WD_Weapon_Fist.asset`
- `Assets/Data/Weapons/WD_Weapon_Bow.asset`
- `Assets/Data/Weapons/WD_Weapon_MeteorHammer.asset`

### 2.4 默认阵型配置

- `Assets/Data/RuntimeTemplates/FD_DefaultBattleFormation.asset`

## 3. 当前写入内容

生成器会按当前主选资源写入默认值：

- 剑士：`human_swordsman`
- 拳师：`assassin`
- 弓箭手：`longbowman`
- Boss：`goblin_boss_wife`

## 4. 使用建议

- 第一次执行后，先检查 Inspector 中的字段是否符合当前需求
- 如果后面你改了主选资源名，可以重复执行一次生成器进行更新
- 如果你手动改过某个资源字段，再次执行时会被当前脚本里的默认值覆盖
