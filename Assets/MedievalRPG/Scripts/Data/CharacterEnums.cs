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
        PhoenixFeather = 2,
        WarElixir = 3,
        IronTonic = 4
    }

    public enum CampBuffType
    {
        VitalityBlessing = 0, // +35 Max HP toàn đội cả trận
        FuryBlessing = 1,     // +8 ATK & +8 MAG toàn đội cả trận
        AegisBlessing = 2     // +6 DEF, +6 MDEF & +2 SPD toàn đội cả trận
    }

    public enum ItemRarity
    {
        Common = 0,     // Thường
        Rare = 1,       // Hiếm
        Epic = 2,       // Sử Thi
        Legendary = 3   // Huyền Thoại
    }

    public enum EquipmentSlot
    {
        Weapon = 0,     // Vũ Khí
        Armor = 1,      // Giáp
        Accessory = 2,  // Phụ Kiện
        Helmet = 3      // Mũ
    }

    public enum QuizDifficultyFilter
    {
        AllB1B2 = 0,
        OnlyB1 = 1,
        OnlyB2 = 2
    }

    public enum CampaignStageId
    {
        ForestChapter1 = 0,
        CryptChapter2 = 1,
        CitadelChapter3 = 2,
        EndlessTower = 3
    }

    public enum EnvironmentThemeType
    {
        MistyForest = 0,
        AncientCrypt = 1,
        DarkCitadel = 2
    }

    public enum AchievementId
    {
        FirstChapterClear = 0,
        StreakFiveB2 = 1,
        FirstLegendary = 2,
        ForgePlusThree = 3,
        MistakeReviewer = 4,
        ChapterThreeClear = 5,
        EndlessWaveTen = 6
    }
}
