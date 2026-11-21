namespace MedievalRPG.Data
{
    public enum CharacterClassType
    {
        Knight = 0,
        Mage = 1,
        Archer = 2
    }

    public enum DamageType
    {
        Physical = 0,
        Magical = 1,
        TrueDamage = 2
    }

    public enum SkillTargetType
    {
        SingleEnemy = 0,
        AllEnemies = 1,
        SingleAlly = 2,
        AllAllies = 3,
        Self = 4
    }

    public enum StatusEffectType
    {
        None = 0,
        Stun = 1,
        Burn = 2,
        Taunt = 3,
        DefenseUp = 4,
        AttackUp = 5
    }

    public enum EnemyIntentType
    {
        None = 0,
        SingleAttack = 1,
        AoEAttack = 2,
        Buff = 3,
        DebuffAttack = 4
    }

    public enum ConsumableItemType
    {
        HealthPotion = 0,
        ManaElixir = 1,
        PhoenixFeather = 2
    }
}
