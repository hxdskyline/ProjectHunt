namespace ProjectHunt.Data
{
    public static class MeteorHammerRules
    {
        public const float SwordsmanBonusDamage = 4f;
        public const float SwordsmanBlockChance = 0.3f;
        public const float AssassinStunChance = 0.35f;
        public const float AssassinStunDuration = 0.9f;
        public const float ArcherImpactRadius = 1.35f;
        public const int ArcherAreaDamageBonus = 3;

        public static string GetEffectDescription(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "\u53d8\u4e3a\u8303\u56f4\u4f24\u5bb3";
                case RoleType.Assassin:
                    return "\u653b\u51fb\u6709\u6982\u7387\u51fb\u6655";
                default:
                    return "\u63d0\u9ad8\u4f24\u5bb3\uff0c\u5e76\u6709\u6982\u7387\u683c\u6321";
            }
        }

        public static string GetAssignButtonText(string displayName)
        {
            return $"\u4ea4\u7ed9{displayName}";
        }

        public static bool IsMeteorHammerArcher(CharacterConfig config)
        {
            return config != null && config.roleType == RoleType.Archer;
        }

        public static bool IsMeteorHammerAssassin(CharacterConfig config)
        {
            return config != null && config.roleType == RoleType.Assassin;
        }

        public static bool IsMeteorHammerSwordsman(CharacterConfig config)
        {
            return config != null && config.roleType == RoleType.Swordsman;
        }
    }

    public static class HolyCupRules
    {
        public static string GetEffectDescription(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "\u53d8\u4e3a\u6295\u77f3\u8f66\uff0c\u6295\u51fa\u91cd\u578b\u9152\u676f";
                case RoleType.Assassin:
                    return "\u6454\u7834\u5723\u676f\uff0c\u5316\u4e3a\u788e\u676f\u5315\u9996";
                default:
                    return "\u9152\u676f\u5957\u5934\uff0c\u5076\u5c14\u949f\u9e23\u964d\u4f4e\u654c\u4eba\u4f24\u5bb3";
            }
        }

        public static string GetAssignButtonText(string displayName)
        {
            return $"\u4ea4\u7ed9{displayName}";
        }

        public static string GetVariantDisplayName(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "\u6295\u77f3\u8f66";
                case RoleType.Assassin:
                    return "\u676f\u523a\u5ba2";
                default:
                    return "\u676f\u5251\u58eb";
            }
        }
    }

    public static class GiantKeyRules
    {
        public static string GetEffectDescription(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "\u62db\u7269\u7ebf\u5c04\u51fa\u65cb\u8f6c\u94a5\u5319\uff0c\u50cf\u94bb\u5934\u4e00\u6837";
                case RoleType.Assassin:
                    return "\u628a\u94a5\u5319\u5f53\u56de\u65cb\u9556\u6254";
                case RoleType.Mage:
                    return "\u4e3e\u8d77\u94a5\u5319\uff0c\u4f20\u9001\u95e8\u5f00\u542f\uff0c\u53ec\u5524\u5c04\u7ebf";
                default:
                    return "\u53d1\u6761\u5251\u58eb";
            }
        }

        public static string GetAssignButtonText(string displayName)
        {
            return $"\u4ea4\u7ed9{displayName}";
        }

        public static string GetVariantDisplayName(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "\u94a5\u5f13\u624b";
                case RoleType.Assassin:
                    return "\u94a5\u523a\u5ba2";
                case RoleType.Mage:
                    return "\u94a5\u6cd5\u5e08";
                default:
                    return "\u94a5\u5251\u58eb";
            }
        }
    }
}
