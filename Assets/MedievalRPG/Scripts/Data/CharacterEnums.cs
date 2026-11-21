namespace MedievalRPG.Data
{
    public enum CharacterClassType
    {
        Knight,
        Mage,
        Archer
    }

    public enum DamageType
    {
        Physical,
        Magical,
        TrueDamage
    }

    public enum SkillTargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self
    }
}
