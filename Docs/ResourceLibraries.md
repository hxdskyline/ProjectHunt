# Resource Libraries

## 1. 当前正式使用中的资源目录

项目当前直接使用的单位像素资源目录：
- [Assets/StreamingAssets/bundle/units](/E:/aiwork/ProjectHunt/Assets/StreamingAssets/bundle/units)

这里保留的是当前 Demo 主线实际会继续用到，或者大概率继续扩展的单位资源。

## 2. 项目内备选资源库

项目内备选资源库位置：
- [raw](/E:/aiwork/ProjectHunt/raw)

用途：
- 存放从正式 `Assets` 目录中移出的备选资源
- 保留可回退、可再筛选的历史资源

当前约定：
- 已排除但不想彻底删除的资源，统一放在 `raw` 下
- 优先从 `raw` 中回找，再去外部原始资源库中二次检索

## 3. 外部原始资源库

更原始的外部资源库位置：
- [E:\SteamLibrary\steamapps\common\The King is Watching\_extracted_images](</E:/SteamLibrary/steamapps/common/The King is Watching/_extracted_images>)

两个重要索引文件：
- [animation_groups.json](</E:/SteamLibrary/steamapps/common/The King is Watching/_extracted_images/animation_groups.json>)
- [animation_categories.json](</E:/SteamLibrary/steamapps/common/The King is Watching/_extracted_images/animation_categories.json>)

用途：
- 查询 Boss / 小怪 / 投射物 / 特效的原始命名
- 回查动作组
- 继续挑选后续关卡候选资源

## 4. 当前主线已确认的资源

玩家基础单位：
- `human_swordsman`
- `assassin`
- `longbowman`

第一关相关：
- `goblin_boss_wife`
- `human_swordsman_hammer`
- `assassin_hammer`
- `longbowman_hammer`

第二关和第三关资源：
- 待正式选定并导入

历史候选资源：
- `fire_spider_boss`
- `human_swordsman_fire`
- `assassin_fire`
- `longbowman_fire`
- `slime_green_big`
- `slime_green_small`

## 5. 使用约定

- `Assets/StreamingAssets/bundle/units` 只保留当前 Demo 真正会用到的资源
- 临时排除的资源不要混回正式目录
- 新挑选 Boss 或新兵种时，先在索引库中确认动作组，再复制进项目
- 进入项目后的资源目录名，尽量使用稳定、可读、可复用的命名
