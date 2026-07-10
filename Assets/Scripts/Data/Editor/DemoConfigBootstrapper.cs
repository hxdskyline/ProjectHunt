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
                "\u5251",
                WeaponType.Sword,
                AttackBehaviorType.SlashForward,
                AttackRangeType.MeleeShort,
                AttackTempo.Medium,
                false,
                false);

            var fist = CreateWeapon(
                "WD_Weapon_Fist",
                "weapon_fist",
                "\u62f3",
                WeaponType.Fist,
                AttackBehaviorType.PunchForward,
                AttackRangeType.MeleeVeryShort,
                AttackTempo.Fast,
                false,
                false);

            var bow = CreateWeapon(
                "WD_Weapon_Bow",
                "weapon_bow",
                "\u5f13",
                WeaponType.Bow,
                AttackBehaviorType.ShootLine,
                AttackRangeType.RangedLine,
                AttackTempo.Slow,
                false,
                false);

            var meteorHammer = CreateWeapon(
                "WD_Weapon_MeteorHammer",
                "weapon_meteor_hammer",
                "\u6d41\u661f\u9524",
                WeaponType.MeteorHammer,
                AttackBehaviorType.SpinArea,
                AttackRangeType.SpinArea,
                AttackTempo.Medium,
                true,
                true);

            var swordsman = CreateCharacter(
                "CD_Player_Swordsman",
                "player_swordsman",
                "\u5251\u58eb",
                RoleType.Swordsman,
                "human_swordsman",
                WeaponType.Sword,
                "attack",
                "walk",
                AttackTempo.Medium,
                AttackRangeType.MeleeShort,
                SpawnSlot.Front);

            var assassin = CreateCharacter(
                "CD_Player_Assassin",
                "player_brawler",
                "\u523a\u5ba2",
                RoleType.Assassin,
                "assassin",
                WeaponType.Sword,
                "attack",
                "walk",
                AttackTempo.Fast,
                AttackRangeType.RangedLine,
                SpawnSlot.Mid,
                2.6f,
                0f);

            var archer = CreateCharacter(
                "CD_Player_Archer",
                "player_archer",
                "\u5f13\u7bad\u624b",
                RoleType.Archer,
                "longbowman",
                WeaponType.Bow,
                "attack",
                "walk",
                AttackTempo.Slow,
                AttackRangeType.RangedLine,
                SpawnSlot.Back,
                2.4f,
                0f);

            var boss = CreateBoss(
                "BD_MeteorHammerBoss",
                "boss_meteor_hammer",
                "\u6d41\u661f\u9524 Boss",
                "goblin_boss_wife",
                WeaponType.MeteorHammer,
                "slam_strong",
                "walk",
                "death",
                AttackRangeType.SpinArea,
                AttackTempo.Medium,
                89,
                WeaponType.MeteorHammer,
                3.8f,
                0f);

            CreateFormation(
                "FD_DefaultBattleFormation",
                "demo_default_battle",
                swordsman,
                assassin,
                archer,
                boss);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Project Hunt",
                "Demo \u914d\u7f6e\u8d44\u6e90\u5df2\u751f\u6210\u6216\u66f4\u65b0\u5b8c\u6210\u3002",
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
            SpawnSlot spawnSlot,
            float visualScale = 3f,
            float yOffset = 0f)
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
            asset.visualScale = visualScale;
            asset.yOffset = yOffset;
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
            WeaponType dropWeaponType,
            float visualScale = 3.8f,
            float yOffset = 0f)
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
            asset.visualScale = visualScale;
            asset.yOffset = yOffset;
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
