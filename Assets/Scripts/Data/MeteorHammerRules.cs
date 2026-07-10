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
}
