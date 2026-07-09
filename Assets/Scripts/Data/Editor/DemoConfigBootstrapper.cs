using System.IO;
using ProjectHunt.Data;
using UnityEditor;
using UnityEngine;

namespace ProjectHunt.EditorTools
{
    public static class DemoConfigBootstrapper
    {
        private const string CharactersDir = "Assets/Data/Characters";
        private const string BossesDir = "Assets/Data/Bosses";
        private const string WeaponsDir = "Assets/Data/Weapons";
        private const string RuntimeTemplatesDir = "Assets/Data/RuntimeTemplates";

        [MenuItem("Project Hunt/Bootstrap/Create Demo Config Assets")]
        public static void CreateDemoConfigAssets()
        {
            EnsureFolder(CharactersDir);
            EnsureFolder(BossesDir);
            EnsureFolder(WeaponsDir);
            EnsureFolder(RuntimeTemplatesDir);

            var sword = CreateWeapon(
                "WD_Weapon_Sword",
                "weapon_sword",
                "剑",
                WeaponType.Sword,
                AttackBehaviorType.SlashForward,
                AttackRangeType.MeleeShort,
                AttackTempo.Medium,
                false,
                false);

            var fist = CreateWeapon(
                "WD_Weapon_Fist",
                "weapon_fist",
                "拳",
                WeaponType.Fist,
                AttackBehaviorType.PunchForward,
                AttackRangeType.MeleeVeryShort,
                AttackTempo.Fast,
                false,
                false);

            var bow = CreateWeapon(
                "WD_Weapon_Bow",
                "weapon_bow",
                "弓",
                WeaponType.Bow,
                AttackBehaviorType.ShootLine,
                AttackRangeType.RangedLine,
                AttackTempo.Slow,
                false,
                false);

            var meteorHammer = CreateWeapon(
                "WD_Weapon_MeteorHammer",
                "weapon_meteor_hammer",
                "流星锤",
                WeaponType.MeteorHammer,
                AttackBehaviorType.SpinArea,
                AttackRangeType.SpinArea,
                AttackTempo.Medium,
                true,
                true);

            var swordsman = CreateCharacter(
                "CD_Player_Swordsman",
                "player_swordsman",
                "剑士",
                RoleType.Swordsman,
                "human_swordsman",
                WeaponType.Sword,
                "attack",
                "walk",
                AttackTempo.Medium,
                AttackRangeType.MeleeShort,
                SpawnSlot.Front);

            var brawler = CreateCharacter(
                "CD_Player_Brawler",
                "player_brawler",
                "拳师",
                RoleType.Brawler,
                "assassin",
                WeaponType.Fist,
                "attack",
                "walk",
                AttackTempo.Fast,
                AttackRangeType.MeleeVeryShort,
                SpawnSlot.Mid);

            var archer = CreateCharacter(
                "CD_Player_Archer",
                "player_archer",
                "弓箭手",
                RoleType.Archer,
                "longbowman",
                WeaponType.Bow,
                "attack",
                "walk",
                AttackTempo.Slow,
                AttackRangeType.RangedLine,
                SpawnSlot.Back);

            var boss = CreateBoss(
                "BD_MeteorHammerBoss",
                "boss_meteor_hammer",
                "流星锤 Boss",
                "goblin_boss_wife",
                WeaponType.MeteorHammer,
                "attack_round",
                "walk",
                "death",
                AttackRangeType.SpinArea,
                AttackTempo.Medium,
                100,
                WeaponType.MeteorHammer);

            CreateFormation(
                "FD_DefaultBattleFormation",
                "demo_default_battle",
                swordsman,
                brawler,
                archer,
                boss);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Project Hunt",
                "Demo 配置资源已生成或更新完成。",
                "OK");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static WeaponConfig CreateWeapon(
            string assetName,
            string id,
            string displayName,
            WeaponType weaponType,
            AttackBehaviorType behavior,
            AttackRangeType rangeType,
            AttackTempo tempo,
            bool canEquipInBuild,
            bool isBossDrop)
        {
            var assetPath = $"{WeaponsDir}/{assetName}.asset";
            var asset = LoadOrCreateAsset<WeaponConfig>(assetPath);
            asset.id = id;
            asset.displayName = displayName;
            asset.weaponType = weaponType;
            asset.attackBehaviorType = behavior;
            asset.rangeType = rangeType;
            asset.tempo = tempo;
            asset.canEquipInBuild = canEquipInBuild;
            asset.isBossDrop = isBossDrop;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CharacterConfig CreateCharacter(
            string assetName,
            string id,
            string displayName,
            RoleType roleType,
            string resourceId,
            WeaponType defaultWeaponType,
            string defaultAttackAction,
            string moveAction,
            AttackTempo attackTempo,
            AttackRangeType attackRangeType,
            SpawnSlot spawnSlot)
        {
            var assetPath = $"{CharactersDir}/{assetName}.asset";
            var asset = LoadOrCreateAsset<CharacterConfig>(assetPath);
            asset.id = id;
            asset.displayName = displayName;
            asset.roleType = roleType;
            asset.resourceId = resourceId;
            asset.defaultWeaponType = defaultWeaponType;
            asset.defaultAttackAction = defaultAttackAction;
            asset.moveAction = moveAction;
            asset.attackTempo = attackTempo;
            asset.attackRangeType = attackRangeType;
            asset.spawnSlot = spawnSlot;
            asset.isPlayable = true;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BossConfig CreateBoss(
            string assetName,
            string id,
            string displayName,
            string resourceId,
            WeaponType weaponType,
            string mainAttackAction,
            string moveAction,
            string deathAction,
            AttackRangeType attackRangeType,
            AttackTempo attackTempo,
            int maxHp,
            WeaponType dropWeaponType)
        {
            var assetPath = $"{BossesDir}/{assetName}.asset";
            var asset = LoadOrCreateAsset<BossConfig>(assetPath);
            asset.id = id;
            asset.displayName = displayName;
            asset.resourceId = resourceId;
            asset.weaponType = weaponType;
            asset.mainAttackAction = mainAttackAction;
            asset.moveAction = moveAction;
            asset.deathAction = deathAction;
            asset.attackRangeType = attackRangeType;
            asset.attackTempo = attackTempo;
            asset.maxHp = maxHp;
            asset.dropWeaponType = dropWeaponType;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static BattleFormationConfig CreateFormation(
            string assetName,
            string id,
            CharacterConfig front,
            CharacterConfig mid,
            CharacterConfig back,
            BossConfig boss)
        {
            var assetPath = $"{RuntimeTemplatesDir}/{assetName}.asset";
            var asset = LoadOrCreateAsset<BattleFormationConfig>(assetPath);
            asset.id = id;
            asset.frontCharacter = front;
            asset.midCharacter = mid;
            asset.backCharacter = back;
            asset.boss = boss;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                EnsureFolder(dir.Replace("\\", "/"));
            }

            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
