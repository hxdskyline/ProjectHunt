using UnityEngine;

namespace ProjectHunt.Data
{
    public enum RoleType
    {
        Swordsman = 0,
        Assassin = 1,
        Archer = 2,
    }

    public enum WeaponType
    {
        Sword = 0,
        Fist = 1,
        Bow = 2,
        MeteorHammer = 3,
    }

    public enum AttackTempo
    {
        Fast = 0,
        Medium = 1,
        Slow = 2,
    }

    public enum AttackRangeType
    {
        MeleeShort = 0,
        MeleeVeryShort = 1,
        RangedLine = 2,
        SpinArea = 3,
    }

    public enum SpawnSlot
    {
        Front = 0,
        Mid = 1,
        Back = 2,
        Boss = 3,
    }

    public enum GamePhase
    {
        MainMenu = 0,
        Battle01 = 1,
        Drop = 2,
        Build = 3,
        Battle02 = 4,
        Drop02 = 5,
        Build02 = 6,
        Result = 7,
    }

    public enum RewardType
    {
        None = 0,
        MeteorHammer = 1,
        FireGland = 2,
    }

    public enum AttackBehaviorType
    {
        SlashForward = 0,
        PunchForward = 1,
        ShootLine = 2,
        SpinArea = 3,
    }
}
