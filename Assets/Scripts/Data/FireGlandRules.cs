namespace ProjectHunt.Data
{
    public static class FireGlandRules
    {
        public static string GetEffectDescription(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "发射燃烧蛛网";
                case RoleType.Assassin:
                    return "火粉爆炸";
                default:
                    return "多一把火剑";
            }
        }

        public static string GetAssignButtonText(string displayName)
        {
            return $"交给{displayName}";
        }

        public static string GetVariantDisplayName(RoleType roleType)
        {
            switch (roleType)
            {
                case RoleType.Archer:
                    return "炎弓手";
                case RoleType.Assassin:
                    return "炎刺客";
                default:
                    return "炎剑士";
            }
        }
    }
}
