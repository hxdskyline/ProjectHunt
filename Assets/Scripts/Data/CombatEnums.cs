using UnityEngine;

namespace ProjectHunt.Data
{
    public enum RoleType
    {
        Swordsman = 0,
        Assassin = 1,
        Archer = 2,
        Mage = 3,
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
        Battle03 = 5,
        Drop02 = 6,
        Build02 = 7,
        Battle04 = 8,
        Battle05 = 9,
        Drop03 = 10,
        Build03 = 11,
        Battle06 = 12,
        Result = 13,
        BlacksmithValidation = 14,
    }

    public enum RewardType
    {
        None = 0,
        MeteorHammer = 1,
        HolyCup = 2,
        GiantKey = 3,
    }

    public enum AttackBehaviorType
    {
        SlashForward = 0,
        PunchForward = 1,
        ShootLine = 2,
        SpinArea = 3,
    }
}
